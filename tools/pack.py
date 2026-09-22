"""Build the Thunderstore/r2modman package zip from package/ into dist/.

Run after `dotnet build -c Release` (which drops Homeward.dll into package/):
    python3 tools/pack.py
Files sit at the zip root, as r2modman's "Import local mod" and Thunderstore expect.
"""
import json, zipfile, pathlib, sys

root = pathlib.Path(__file__).resolve().parent.parent
pkg = root / "package"
version = json.load(open(pkg / "manifest.json"))["version_number"]
files = ["manifest.json", "README.md", "CHANGELOG.md", "icon.png", "Homeward.dll"]
missing = [f for f in files if not (pkg / f).exists()]
if missing:
    sys.exit(f"missing in package/: {missing} (build first?)")
(root / "dist").mkdir(exist_ok=True)
out = root / "dist" / f"Homeward-{version}.zip"
with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as z:
    for f in files:
        z.write(pkg / f, arcname=f)
print(out)
