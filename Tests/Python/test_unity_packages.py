import importlib.util
import hashlib
import json
from pathlib import Path, PureWindowsPath
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("package_builder", ROOT / "Tools/build_unity_packages.py")
builder = importlib.util.module_from_spec(spec)
spec.loader.exec_module(builder)


class UnityPackageTests(unittest.TestCase):
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
