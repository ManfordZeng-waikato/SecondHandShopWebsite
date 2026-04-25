#!/usr/bin/env bash
set -euo pipefail

REPORT_PATH="${1:?Usage: check-coverage-thresholds.sh <cobertura.xml>}"

python - "$REPORT_PATH" <<'PY'
import sys
import xml.etree.ElementTree as ET

report_path = sys.argv[1]
thresholds = {
    "SecondHandShop.Domain": 85.0,
    "SecondHandShop.Application": 80.0,
    "SecondHandShop.Infrastructure": 65.0,
    "SecondHandShop.WebApi": 70.0,
}

root = ET.parse(report_path).getroot()
packages = {package.attrib["name"]: package for package in root.findall("./packages/package")}
failed = False

for name, required in thresholds.items():
    package = packages.get(name)
    if package is None:
        print(f"Coverage package '{name}' was not found.", file=sys.stderr)
        failed = True
        continue

    actual = float(package.attrib["line-rate"]) * 100
    if actual < required:
        print(f"{name} line coverage {actual:.2f}% is below required {required:.2f}%.", file=sys.stderr)
        failed = True
    else:
        print(f"{name} line coverage {actual:.2f}% >= {required:.2f}%.")

if failed:
    sys.exit(1)
PY
