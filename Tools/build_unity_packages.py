"""Build versioned UPM folders from Assets/DronePhysics; standard library only.
Generated folders live outside Assets/Packages so this development project does not compile copies.
Run --check in CI to detect stale packages; edit source files, not Distribution copies.
"""
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import shutil
import uuid

ROOT = Path(__file__).resolve().parents[1]
VERSION = "0.3.4"
PACKAGE_ROOT = ROOT / "Distribution/Packages"
TEXT_SUFFIXES = {".cs", ".asmdef", ".json", ".md", ".txt", ".csv", ".meta"}


def text(value):
    return (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode()


def canonical_bytes(path, content):
    """Git may check out CRLF on Windows; exported text is UTF-8 without BOM, LF."""
    if PurePosixPath(path).suffix not in TEXT_SUFFIXES:
        return content
    value = content.decode("utf-8-sig").replace("\r\n", "\n").replace("\r", "\n")
    if path.endswith(".meta"):
        value = "\n".join(line.rstrip() for line in value.splitlines()) + "\n"
    return value.encode("utf-8")


def collect(root=ROOT):
    source = root / "Assets/DronePhysics"
    for relative in ("Core", "Unity", "Resources", "Editor", "Demo/Runtime", "Demo/Editor", "Tests/EditMode", "Tests/PlayMode"):
        if not (source / relative).is_dir():
            raise ValueError("Missing source directory: " + str(source / relative) +
                             "; this exporter reads Assets/DronePhysics, not installed package copies.")
    result = {}
    for name, demo in (("com.dronelab.physics", False), ("com.dronelab.demo", True)):
        files = {}

        def copy_tree(src, destination):
            for p in sorted(src.rglob("*")):
                if p.is_file():
                    files[(Path(destination) / p.relative_to(src)).as_posix()] = p.read_bytes()
            meta = Path(str(src) + ".meta")
            if meta.exists():
                files[destination + ".meta"] = meta.read_bytes()

        if demo:
            copy_tree(source / "Demo/Runtime", "Runtime")
            copy_tree(source / "Demo/Editor", "Editor")
            copy_tree(source / "Tests/PlayMode", "Tests/Runtime")
            dependencies = {"com.dronelab.physics": VERSION, "com.unity.inputsystem": "1.20.0"}
        else:
            copy_tree(source / "Core", "Runtime/Core")
            copy_tree(source / "Unity", "Runtime/Unity")
            copy_tree(source / "Resources", "Runtime/Resources")
            copy_tree(source / "Editor", "Editor")
            copy_tree(source / "Tests/EditMode", "Tests/Editor")
            dependencies = {"com.unity.nuget.newtonsoft-json": "3.2.1"}
            for module in ("physics", "terrain", "terrainphysics", "jsonserialize", "imgui"):
                dependencies["com.unity.modules." + module] = "1.0.0"
        files["package.json"] = text({"name": name, "version": VERSION,
            "displayName": "DroneLab Test Pilot" if demo else "DroneLab Physics",
            "description": "Optional Angle/Acro/H test controller, input and camera." if demo else "Parameterized multirotor physics, authoring and telemetry; no input system dependency.",
            "unity": "6000.3", "license": "GPL-3.0-only", "dependencies": dependencies})
        files["LICENSE.md"] = (root / "LICENSE").read_bytes()
        files["README.md"] = (root / "Docs/Physics" / ("DEMO_PACKAGE_README.md" if demo else "PHYSICS_PACKAGE_README.md")).read_bytes()
        for doc in (root / "Docs/Physics").rglob("*"):
            if doc.name in {"PHYSICS_PACKAGE_README.md", "DEMO_PACKAGE_README.md"}:
                continue  # Templates are copied to each package root, where their links resolve.
            if doc.is_file():
                files[(Path("Documentation~") / doc.relative_to(root / "Docs/Physics")).as_posix()] = doc.read_bytes()
        files["CHANGELOG.md"] = (root / "CHANGELOG.md").read_bytes()
        # Deterministic metadata for generated folders; source script GUIDs stay unchanged.
        directories = set()
        for path in files:
            for parent in PurePosixPath(path).parents:
                if str(parent) != "." and "Documentation~" not in parent.parts:
                    directories.add(str(parent))
        for directory in sorted(directories):
            key = directory + ".meta"
            if key not in files:
                guid = uuid.uuid5(uuid.NAMESPACE_URL, name + "/" + directory).hex
                files[key] = ("fileFormatVersion: 2\nguid: " + guid + "\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()
        # Unity cannot generate metadata inside immutable Git/cache packages.
        # The checksum file is added later but needs its own metadata too.
        for path in list(files) + ["source-files.sha256.json"]:
            if path.endswith(".meta") or path.startswith("Documentation~/") or path + ".meta" in files:
                continue
            if path.endswith((".cs", ".asmdef", ".json")) and path not in {"package.json", "source-files.sha256.json"}:
                raise ValueError("Missing source metadata: " + name + "/" + path)
            guid = uuid.uuid5(uuid.NAMESPACE_URL, name + "/" + path).hex
            importer = "TextScriptImporter" if path.endswith(".json") else "DefaultImporter"
            files[path + ".meta"] = ("fileFormatVersion: 2\nguid: " + guid + "\n" + importer + ":\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()
        files = {path: canonical_bytes(path, content) for path, content in files.items()}
        files["source-files.sha256.json"] = text({path: hashlib.sha256(content).hexdigest() for path, content in sorted(files.items())})
        result[name] = files
    return result


def build(destination=PACKAGE_ROOT, check=False):
    packages = collect()
    if check:
        for name, expected in packages.items():
            actual = {p.relative_to(destination / name).as_posix(): canonical_bytes(p.relative_to(destination / name).as_posix(), p.read_bytes()) for p in (destination / name).rglob("*") if p.is_file()}
            if expected != actual:
                raise ValueError("Stale generated package: " + name + "; run python Tools/build_unity_packages.py")
    else:
        for name, files in packages.items():
            folder = destination / name
            if folder.exists():
                shutil.rmtree(folder)
            for path, content in files.items():
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
    for name, files in packages.items():
        print(name + " " + VERSION + ": " + str(len(files)) + " files" + (" verified" if args.check else " built"))


if __name__ == "__main__":
    main()
