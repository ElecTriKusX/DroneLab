"""Export the DroneLab weather bridge, without redistributing Enviro. Windows/Linux, stdlib only."""
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import shutil
import uuid

ROOT = Path(__file__).resolve().parents[1]
NAME = "com.dronelab.environment"
VERSION = "0.1.0"
PACKAGE_ROOT = ROOT / "Distribution/Packages"
TEXT_SUFFIXES = {".cs", ".asmdef", ".json", ".md", ".meta"}


def canonical_bytes(path, data):
    if PurePosixPath(path).suffix not in TEXT_SUFFIXES:
        return data
    value = data.decode("utf-8-sig").replace("\r\n", "\n").replace("\r", "\n")
    if path.endswith(".meta"):
        value = "\n".join(line.rstrip() for line in value.splitlines()) + "\n"
    return value.encode("utf-8")


def collect(root=ROOT):
    source = root / "Assets/DroneEnvironment"
    files = {}
    for relative, destination in (("Core", "Runtime/Core"), ("Runtime", "Runtime/Enviro"),
                                  ("Editor", "Editor"), ("Tests/EditMode", "Tests/Editor")):
        src = source / relative
        if not src.is_dir():
            raise ValueError("Missing source directory: " + str(src))
        for p in sorted(src.rglob("*")):
            if p.is_file():
                files[(PurePosixPath(destination) / p.relative_to(src).as_posix()).as_posix()] = p.read_bytes()
        meta = Path(str(src) + ".meta")
        if not meta.exists():
            raise ValueError("Missing source metadata: " + str(meta))
        files[destination + ".meta"] = meta.read_bytes()
    for name in ("README.md", "README.md.meta"):
        files[name] = (source / name).read_bytes()
    files["LICENSE.md"] = (root / "LICENSE").read_bytes()
    files["package.json"] = (json.dumps({"name": NAME, "version": VERSION,
        "displayName": "DroneLab Environment", "unity": "6000.3", "license": "GPL-3.0-only",
        "description": "Enviro 3 weather and SI wind bridge for DroneLab; Enviro must be installed separately.",
        "dependencies": {"com.dronelab.physics": "0.3.3", "com.unity.modules.imgui": "1.0.0"}}, indent=2) + "\n").encode()
    directories = {str(parent) for path in files for parent in PurePosixPath(path).parents if str(parent) != "."}
    for path in sorted(directories) + list(files) + ["source-files.sha256.json"]:
        if path.endswith(".meta") or path + ".meta" in files:
            continue
        if path.endswith((".cs", ".asmdef")):
            raise ValueError("Missing source metadata: " + path)
        guid = uuid.uuid5(uuid.NAMESPACE_URL, NAME + "/" + path).hex
        folder = "folderAsset: yes\n" if path in directories else ""
        importer = "TextScriptImporter" if path.endswith(".json") else "DefaultImporter"
        files[path + ".meta"] = ("fileFormatVersion: 2\nguid: " + guid + "\n" + folder + importer +
            ":\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()
    files = {path: canonical_bytes(path, content) for path, content in files.items()}
    files["source-files.sha256.json"] = (json.dumps({path: hashlib.sha256(content).hexdigest()
        for path, content in sorted(files.items())}, indent=2) + "\n").encode()
    return {NAME: files}


def build(destination=PACKAGE_ROOT, check=False):
    packages = collect()
    folder = destination / NAME
    expected = packages[NAME]
    if check:
        actual = {p.relative_to(folder).as_posix(): canonical_bytes(p.relative_to(folder).as_posix(), p.read_bytes())
                  for p in folder.rglob("*") if p.is_file()}
        if actual != expected:
            raise ValueError("Stale environment package; run python Tools/build_environment_package.py")
    else:
        if folder.exists():
            shutil.rmtree(folder)
        for path, content in expected.items():
            target = folder / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(content)
    return packages


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    try:
        packages = build(check=args.check)
    except (OSError, ValueError) as error:
        parser.exit(1, str(error) + "\n")
    print(NAME + " " + VERSION + ": " + str(len(packages[NAME])) + " files " + ("verified" if args.check else "built"))


if __name__ == "__main__":
    main()
