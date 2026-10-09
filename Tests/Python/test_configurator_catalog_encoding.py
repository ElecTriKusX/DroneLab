"""Regression for the Windows CP1252 generator failure seen in Actions."""
import importlib.util
import json
from pathlib import Path
import runpy
import shutil
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]

class CatalogEncodingTests(unittest.TestCase):
    def test_generator_is_identical_under_cp1252_defaults(self):
        original_read = Path.read_text
        original_write = Path.write_text
        def windows_read(path, encoding=None, **kwargs):
            return original_read(path, encoding=encoding or 'cp1252', **kwargs)
        def windows_write(path, data, encoding=None, **kwargs):
            return original_write(path, data, encoding=encoding or 'cp1252', **kwargs)
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / 'Tools').mkdir()
            shutil.copyfile(ROOT / 'Tools/generate_configurator_catalog.py', root / 'Tools/generate_configurator_catalog.py')
            shutil.copytree(ROOT / 'Assets/DronePhysics/Resources', root / 'Assets/DronePhysics/Resources')
            target = root / 'Assets/DroneUI/Resources/DroneLab'
            target.mkdir(parents=True)
            with patch.object(Path, 'read_text', windows_read), patch.object(Path, 'write_text', windows_write):
                runpy.run_path(str(root / 'Tools/generate_configurator_catalog.py'))
            for name in ('DroneParameters.json', 'DroneParameterDefaults.json'):
                generated = (target / name).read_bytes()
                self.assertEqual(generated, (ROOT / 'Assets/DroneUI/Resources/DroneLab' / name).read_bytes())
                self.assertNotIn(b'\r', generated)
                json.loads(generated.decode('utf-8'))
