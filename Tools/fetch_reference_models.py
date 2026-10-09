"""Fetch pinned public reference geometry; no third-party mesh enters the SDK itself."""
import argparse
import hashlib
import json
from pathlib import Path
from urllib.request import Request, urlopen

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", type=Path, default=ROOT / "ReferenceModels")
    args = parser.parse_args()
    manifest = json.loads((ROOT / "Docs/Physics/Data/reference-model-manifest.json").read_text(encoding="utf-8-sig"))
    for item in manifest["files"]:
        target = args.out / item["path"]
        if target.exists() and hashlib.sha256(target.read_bytes()).hexdigest() == item["sha256"]:
            print("Verified", target)
            continue
        with urlopen(Request(item["url"], headers={"User-Agent": "DroneLab-reference-fetcher"}), timeout=60) as response:
            content = response.read()
        if hashlib.sha256(content).hexdigest() != item["sha256"]:
            raise ValueError("Unexpected geometry/license content: " + item["url"])
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(content)
        print("Downloaded and verified", target)
    notice = (ROOT / "Docs/Physics/THIRD_PARTY.md").read_text(encoding="utf-8-sig")
    (args.out / "README.md").write_bytes(notice.replace("\r\n", "\n").encode("utf-8"))


if __name__ == "__main__":
    main()
