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

$projects = @(
    "tests/SecondHandShop.Domain.UnitTests/SecondHandShop.Domain.UnitTests.csproj",
    "tests/SecondHandShop.Application.UnitTests/SecondHandShop.Application.UnitTests.csproj",
    "tests/SecondHandShop.Infrastructure.UnitTests/SecondHandShop.Infrastructure.UnitTests.csproj",
    "tests/SecondHandShop.Infrastructure.IntegrationTests/SecondHandShop.Infrastructure.IntegrationTests.csproj",
    "tests/SecondHandShop.WebApi.IntegrationTests/SecondHandShop.WebApi.IntegrationTests.csproj"
)

foreach ($project in $projects) {
    dotnet test (Join-Path $root $project) `
        --configuration $Configuration `
        --collect:"XPlat Code Coverage" `
        --results-directory $coverageDir

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

dotnet tool restore --tool-manifest (Join-Path $root ".config/dotnet-tools.json")
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

dotnet tool run reportgenerator `
    "-reports:$coverageDir/**/coverage.cobertura.xml" `
    "-targetdir:$reportDir" `
    "-reporttypes:Html;TextSummary;Cobertura"
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& (Join-Path $PSScriptRoot "check-coverage-thresholds.ps1") `
    -ReportPath (Join-Path $reportDir "Cobertura.xml")
