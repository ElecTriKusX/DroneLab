"""Install generated DJI documents and compressed GLBs as built-in gallery content.
Usage: python Tools/install_builtin_drones.py DIRECTORY_FROM_BUILD_DJI_PROFILES
StreamingAssets preserve the model data without Unity importing 200 MB of duplicate meshes.
"""
import argparse
import hashlib
import json
from pathlib import Path
import uuid
import zipfile

ROOT = Path(__file__).resolve().parents[1]
KEYS = ('dji_mavic_3', 'dji_mini_3', 'dji_avata2', 'dji_m600_ul_ver')

def meta(path, importer='DefaultImporter', folder=False):
    target = Path(str(path) + '.meta')
    if target.exists():
        return
    guid = uuid.uuid5(uuid.NAMESPACE_URL, 'DroneLab/' + path.relative_to(ROOT).as_posix()).hex
    target.write_bytes((f'fileFormatVersion: 2\nguid: {guid}\n' + ('folderAsset: yes\n' if folder else '') +
                       importer + ':\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n').encode('utf-8'))

def install(source):
    archives = ROOT / 'Assets/StreamingAssets/DroneLab/DroneModels'
    archives.mkdir(parents=True, exist_ok=True)
    for folder in (archives.parents[1], archives.parent, archives):
        meta(folder, folder=True)
    catalog = {'profiles': [], 'models': {}}
    for key in KEYS:
        document = json.loads((source / key / 'document.json').read_text(encoding='utf-8'))
        document['id'] = 'builtin-' + key
        document['draft'] = False
        document['visual']['bundledModel'] = key
        model = source / key / document['visual']['modelFile']
        data = model.read_bytes()
        archive = archives / (key + '.zip')
        with zipfile.ZipFile(archive, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as pack:
            entry = zipfile.ZipInfo(model.name, date_time=(2026, 1, 1, 0, 0, 0))
            entry.compress_type = zipfile.ZIP_DEFLATED
            entry.external_attr = 0o100644 << 16
            pack.writestr(entry, data, compresslevel=9)
        if archive.stat().st_size >= 100 * 1024 * 1024:
            raise ValueError('Model pack exceeds GitHub file size limit: ' + str(archive))
        meta(archive)
        catalog['models'][key] = dict(archive=archive.name, file=model.name,
                                     sha256=hashlib.sha256(data).hexdigest(), sizeBytes=len(data))
        catalog['profiles'].append(document)
        print(key, 'packed:', archive.stat().st_size, 'bytes')
    target = ROOT / 'Assets/DroneUI/Resources/DroneLab/BuiltinDrones.json'
    target.write_bytes((json.dumps(catalog, ensure_ascii=False, indent=2) + '\n').encode('utf-8'))
    meta(target, 'TextScriptImporter')
    meta(ROOT / 'Assets/DroneUI/Runtime/Configurator/DroneBundledModels.cs', 'MonoImporter')

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    install(parser.parse_args().source)
