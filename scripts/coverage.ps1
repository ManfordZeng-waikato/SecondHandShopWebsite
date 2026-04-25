param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$coverageDir = Join-Path $root ".coverage"
$reportDir = Join-Path $coverageDir "report"

if (Test-Path $coverageDir) {
    Remove-Item $coverageDir -Recurse -Force
}

dotnet test (Join-Path $root "SecondHandShopWebsite.slnx") `
    --configuration $Configuration `
    --collect:"XPlat Code Coverage" `
    --results-directory $coverageDir

dotnet tool restore --tool-manifest (Join-Path $root ".config/dotnet-tools.json")
dotnet tool run reportgenerator `
    "-reports:$coverageDir/**/coverage.cobertura.xml" `
    "-targetdir:$reportDir" `
    "-reporttypes:Html;TextSummary;Cobertura"
