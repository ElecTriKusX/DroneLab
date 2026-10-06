import importlib.util
import hashlib
import json
import posixpath
import re
import shutil
from pathlib import Path, PureWindowsPath
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("package_builder", ROOT / "Tools/build_unity_packages.py")
builder = importlib.util.module_from_spec(spec)
spec.loader.exec_module(builder)


class UnityPackageTests(unittest.TestCase):
    def test_packaged_documentation_has_no_missing_local_links(self):
        for name, files in builder.collect().items():
            for path, data in files.items():
                if not path.endswith(".md"):
                    continue
                for target in re.findall(r"\]\(([^)]+)\)", data.decode("utf-8")):
                    if target.startswith(("https:", "http:", "#", "mailto:")):
                        continue
                    resolved = posixpath.normpath(posixpath.join(posixpath.dirname(path), target.split("#")[0]))
                    self.assertIn(resolved, files, name + "/" + path + " -> " + target)

    def test_crlf_bom_unicode_checkout_produces_same_package_bytes(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder) / "DroneLab проект с пробелами"
            root.mkdir()
            for relative in ("Assets/DronePhysics", "Docs/Physics"):
                shutil.copytree(ROOT / relative, root / relative)
            for name in ("LICENSE", "CHANGELOG.md"):
                shutil.copyfile(ROOT / name, root / name)
            expected = builder.collect(root)
            for file in root.rglob("*"):
                if file.is_file() and (file.suffix in builder.TEXT_SUFFIXES or file.name == "LICENSE"):
                    data = file.read_bytes().decode("utf-8-sig").replace("\r\n", "\n")
                    file.write_bytes(b"\xef\xbb\xbf" + data.replace("\n", "\r\n").encode("utf-8"))
            self.assertEqual(builder.collect(root), expected)

    def test_check_accepts_windows_checkout_but_detects_content_changes(self):
        with tempfile.TemporaryDirectory() as folder:
            destination = Path(folder) / "пакеты с пробелами"
            packages = builder.build(destination)
            for name, files in packages.items():
                for path, data in files.items():
                    if Path(path).suffix in builder.TEXT_SUFFIXES:
                        (destination / name / path).write_bytes(data.replace(b"\n", b"\r\n"))
            builder.build(destination, check=True)
            with (destination / "com.dronelab.physics/Runtime/Core/PhysicsMath.cs").open("ab") as stream:
                stream.write(b"// changed implementation\r\n")
            with self.assertRaisesRegex(ValueError, "Stale"):
                builder.build(destination, check=True)

    def test_missing_sources_fail_before_emitting_partial_packages(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaisesRegex(ValueError, "Missing source directory"):
                builder.collect(Path(folder))

    def test_windows_package_paths_preserve_documentation_and_folder_guids(self):
        expected = builder.collect()
        def windows_package_path(value):
            # Simulate Windows separators for logical package destinations,
            # while keeping real source IO on the host filesystem.
            if isinstance(value, str) and value.startswith(("Runtime", "Editor", "Tests", "Documentation~")):
                return PureWindowsPath(value)
            return Path(value)
        with patch.object(builder, "Path", side_effect=windows_package_path):
            actual = builder.collect()
        self.assertEqual(actual, expected)
        self.assertIn("Documentation~/Data/reference-model-manifest.json", actual["com.dronelab.physics"])
        self.assertTrue(all("\\" not in path for files in actual.values() for path in files))

    def test_all_importable_package_assets_have_stable_metadata_and_hashes(self):
        first = builder.collect()
        self.assertEqual(first, builder.collect())
        for name, files in first.items():
            hashes = json.loads(files["source-files.sha256.json"])
            self.assertEqual(set(hashes), set(files) - {"source-files.sha256.json"})
            for path, content in files.items():
                if path != "source-files.sha256.json":
                    self.assertEqual(hashes[path], hashlib.sha256(content).hexdigest(), name + "/" + path)
                if path.endswith(".meta") or path.startswith("Documentation~/"):
                    continue
                self.assertIn(path + ".meta", files, name + "/" + path)
            self.assertFalse(any(path.startswith("Documentation~/") and path.endswith(".meta") for path in files))

    def test_physics_has_no_demo_input_or_render_pipeline_dependency(self):
        files = builder.collect()["com.dronelab.physics"]
        manifest = json.loads(files["package.json"])
        self.assertNotIn("com.unity.inputsystem", manifest["dependencies"])
        self.assertFalse(any("DroneTestPilot.cs" in p or "DronePilotInput.cs" in p for p in files))
        for path, value in files.items():
            if path.startswith(("Runtime/", "Editor/")) and path.endswith((".cs", ".asmdef")):
                self.assertNotIn(b"Unity.InputSystem", value)
                self.assertNotIn(b"DroneTestPilot", value.replace(b"DroneTestPilot cannot control this layout", b""))
        self.assertIn("Runtime/Resources/DronePhysics/drone-profile.schema.json", files)
        self.assertIn("Editor/DroneInertiaImportMenu.cs", files)

    def test_demo_depends_on_physics_and_source_guids_are_preserved(self):
        packages = builder.collect()
        manifest = json.loads(packages["com.dronelab.demo"]["package.json"])
        self.assertEqual(manifest["dependencies"]["com.dronelab.physics"], builder.VERSION)
        for name, dest, src in [
            ("com.dronelab.demo", "Runtime/DroneTestPilot.cs.meta", "Demo/Runtime/DroneTestPilot.cs.meta"),
            ("com.dronelab.physics", "Runtime/Unity/DronePhysicsBody.cs.meta", "Unity/DronePhysicsBody.cs.meta"),
        ]:
            def guid(content):
                return next(line for line in content.splitlines() if line.startswith(b"guid: "))
            self.assertEqual(guid(packages[name][dest]), guid((ROOT / "Assets/DronePhysics" / src).read_bytes()))

    def test_generated_packages_are_reproducible_and_stale_copy_is_detected(self):
        with tempfile.TemporaryDirectory() as folder:
            destination = Path(folder)
            builder.build(destination)
            builder.build(destination, check=True)
            (destination / "com.dronelab.physics/Runtime/Core/PhysicsMath.cs").write_text("stale")
            with self.assertRaisesRegex(ValueError, "Stale"):
                builder.build(destination, check=True)

    def test_packages_do_not_duplicate_guids_or_compile_input_in_physics(self):
        seen = set()
        for files in builder.collect().values():
            for name, content in files.items():
                if name.endswith(".meta"):
                    for line in content.decode().splitlines():
                        if line.startswith("guid: "):
                            guid = line[6:]
                            self.assertNotIn(guid, seen, name)
                            seen.add(guid)
