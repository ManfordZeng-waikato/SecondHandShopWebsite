#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${1:-Debug}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COVERAGE_DIR="$ROOT/.coverage"
REPORT_DIR="$COVERAGE_DIR/report"

rm -rf "$COVERAGE_DIR"

dotnet test "$ROOT/SecondHandShopWebsite.slnx" \
  --configuration "$CONFIGURATION" \
  --collect:"XPlat Code Coverage" \
  --results-directory "$COVERAGE_DIR"

dotnet tool restore --tool-manifest "$ROOT/.config/dotnet-tools.json"
dotnet tool run reportgenerator \
  "-reports:$COVERAGE_DIR/**/coverage.cobertura.xml" \
  "-targetdir:$REPORT_DIR" \
  "-reporttypes:Html;TextSummary;Cobertura"
