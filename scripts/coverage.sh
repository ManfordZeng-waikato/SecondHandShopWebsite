#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${1:-Debug}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COVERAGE_DIR="$ROOT/.coverage"
REPORT_DIR="$COVERAGE_DIR/report"

rm -rf "$COVERAGE_DIR"

for project in \
  "$ROOT/tests/SecondHandShop.Domain.UnitTests/SecondHandShop.Domain.UnitTests.csproj" \
  "$ROOT/tests/SecondHandShop.Application.UnitTests/SecondHandShop.Application.UnitTests.csproj" \
  "$ROOT/tests/SecondHandShop.Infrastructure.UnitTests/SecondHandShop.Infrastructure.UnitTests.csproj" \
  "$ROOT/tests/SecondHandShop.Infrastructure.IntegrationTests/SecondHandShop.Infrastructure.IntegrationTests.csproj" \
  "$ROOT/tests/SecondHandShop.WebApi.IntegrationTests/SecondHandShop.WebApi.IntegrationTests.csproj"; do
  dotnet test "$project" \
    --configuration "$CONFIGURATION" \
    --collect:"XPlat Code Coverage" \
    --results-directory "$COVERAGE_DIR"
done

dotnet tool restore --tool-manifest "$ROOT/.config/dotnet-tools.json"
dotnet tool run reportgenerator \
  "-reports:$COVERAGE_DIR/**/coverage.cobertura.xml" \
  "-targetdir:$REPORT_DIR" \
  "-reporttypes:Html;TextSummary;Cobertura"

bash "$ROOT/scripts/check-coverage-thresholds.sh" "$REPORT_DIR/Cobertura.xml"
