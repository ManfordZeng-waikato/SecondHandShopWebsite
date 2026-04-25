param(
    [Parameter(Mandatory = $true)]
    [string]$ReportPath
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ReportPath)) {
    throw "Coverage report not found: $ReportPath"
}

$thresholds = [ordered]@{
    "SecondHandShop.Domain" = 85
    "SecondHandShop.Application" = 80
    "SecondHandShop.Infrastructure" = 65
    "SecondHandShop.WebApi" = 70
}

[xml]$coverage = Get-Content -Path $ReportPath
$failed = $false

foreach ($entry in $thresholds.GetEnumerator()) {
    $package = $coverage.coverage.packages.package | Where-Object { $_.name -eq $entry.Key } | Select-Object -First 1
    if ($null -eq $package) {
        Write-Error "Coverage package '$($entry.Key)' was not found."
        $failed = $true
        continue
    }

    $actual = [Math]::Round(([double]$package.'line-rate') * 100, 2)
    $required = [double]$entry.Value
    if ($actual -lt $required) {
        Write-Error "$($entry.Key) line coverage $actual% is below required $required%."
        $failed = $true
    } else {
        Write-Host "$($entry.Key) line coverage $actual% >= $required%."
    }
}

if ($failed) {
    exit 1
}
