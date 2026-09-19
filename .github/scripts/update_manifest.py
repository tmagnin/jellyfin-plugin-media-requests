#!/usr/bin/env python3
"""Adds (or replaces) one version entry in manifest.json.

Usage: update_manifest.py VERSION SOURCE_URL MD5 CHANGELOG
targetAbi is read from build.yaml so it only lives in one place.
"""
import datetime
import json
import pathlib
import re
import sys

version, source_url, checksum, changelog = sys.argv[1:5]

build_yaml = pathlib.Path("build.yaml").read_text()
abi_match = re.search(r'^targetAbi:\s*"?([\d.]+)"?', build_yaml, re.MULTILINE)
if not abi_match:
    sys.exit("targetAbi not found in build.yaml")

manifest_path = pathlib.Path("manifest.json")
manifest = json.loads(manifest_path.read_text())
plugin = manifest[0]

entry = {
    "version": version,
    "changelog": changelog,
    "targetAbi": abi_match.group(1),
    "sourceUrl": source_url,
    "checksum": checksum,
    "timestamp": datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
}

# Newest first; keep older versions so servers on older Jellyfin releases still get a compatible build.
plugin["versions"] = [entry] + [v for v in plugin["versions"] if v["version"] != version]

manifest_path.write_text(json.dumps(manifest, indent=2) + "\n")
print(f"manifest.json now lists {len(plugin['versions'])} version(s); newest is {version}")
