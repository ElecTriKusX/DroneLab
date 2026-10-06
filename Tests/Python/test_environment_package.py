import hashlib
import importlib.util
import json
from pathlib import Path
import re
import shutil
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("environment_builder", ROOT / "Tools/build_environment_package.py")
builder = importlib.util.module_from_spec(spec)
spec.loader.exec_module(builder)


class EnvironmentPackageTests(unittest.TestCase):
    def test_export_is_reproducible_preserves_guids_and_excludes_vendor(self):
        files = builder.collect()[builder.NAME]
        self.assertEqual(files, builder.collect()[builder.NAME])
        self.assertEqual(files["Runtime/Enviro/EnviroWeatherController.cs.meta"],
                         builder.canonical_bytes("x.meta", (ROOT / "Assets/DroneEnvironment/Runtime/EnviroWeatherController.cs.meta").read_bytes()))
        self.assertNotIn("EnviroManager.cs", " ".join(files))
        self.assertNotIn("Enviro3.Runtime.asmdef", " ".join(files))
        self.assertIn("Enviro3.Runtime", json.loads(files["Runtime/Enviro/DroneLab.Environment.Enviro.asmdef"])["references"])
        self.assertEqual(json.loads(files["package.json"])["dependencies"]["com.dronelab.physics"], "0.3.4")
        hashes = json.loads(files["source-files.sha256.json"])
        self.assertEqual(set(hashes), set(files) - {"source-files.sha256.json"})
        for path, data in files.items():
            self.assertNotIn("\\", path)
            if path != "source-files.sha256.json":
                self.assertEqual(hashes[path], hashlib.sha256(data).hexdigest())
            if not path.endswith(".meta"):
                self.assertIn(path + ".meta", files)
        guids = [re.search(rb"guid: ([0-9a-f]{32})", data).group(1)
                 for path, data in files.items() if path.endswith(".meta")]
        self.assertEqual(len(guids), len(set(guids)))

    def test_unicode_crlf_bom_checkout_matches_linux_export(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "Проект с пробелами"
            shutil.copytree(ROOT / "Assets/DroneEnvironment", root / "Assets/DroneEnvironment")
            shutil.copyfile(ROOT / "LICENSE", root / "LICENSE")
            expected = builder.collect(root)
            for path in root.rglob("*"):
                if path.is_file() and (path.suffix in builder.TEXT_SUFFIXES or path.name == "LICENSE"):
                    path.write_bytes(b"\xef\xbb\xbf" + path.read_bytes().replace(b"\n", b"\r\n"))
            self.assertEqual(expected, builder.collect(root))

    def test_check_handles_crlf_and_rejects_changed_source_or_missing_meta(self):
        with tempfile.TemporaryDirectory() as directory:
            destination = Path(directory)
            packages = builder.build(destination)
            for path, data in packages[builder.NAME].items():
                (destination / builder.NAME / path).write_bytes(data.replace(b"\n", b"\r\n"))
            builder.build(destination, check=True)
            (destination / builder.NAME / "Runtime/Core/WeatherSnapshot.cs").write_text("changed", encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "Stale"):
                builder.build(destination, check=True)
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            shutil.copytree(ROOT / "Assets/DroneEnvironment", root / "Assets/DroneEnvironment")
            shutil.copyfile(ROOT / "LICENSE", root / "LICENSE")
            (root / "Assets/DroneEnvironment/Core/WeatherSnapshot.cs.meta").unlink()
            with self.assertRaisesRegex(ValueError, "Missing source metadata"):
                builder.collect(root)
