[CmdletBinding()]
param(
    [switch]$SkipUnit,
    [switch]$SkipQuality,
    [switch]$SkipUi,
    [switch]$SkipLoad
)

$ErrorActionPreference = "Continue"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $root

$results = [ordered]@{}

function Invoke-Stage {
    param([string]$Name, [scriptblock]$Action)

    Write-Host "`n=== $Name ===" -ForegroundColor Cyan
    $global:LASTEXITCODE = 0
    $timer = [Diagnostics.Stopwatch]::StartNew()

    try {
        & $Action
        if ($LASTEXITCODE -ne 0) { throw "exit code $LASTEXITCODE" }
        $script:results[$Name] = "PASSED ({0:n0}s)" -f $timer.Elapsed.TotalSeconds
    }
    catch {
        $script:results[$Name] = "FAILED - $($_.Exception.Message)"
    }
}

if (-not $SkipUnit) {
    Invoke-Stage "Unit tests and mocking" {
        dotnet test backend/HostelManagement.sln -c Release `
            --collect:"XPlat Code Coverage" --results-directory TestResults
    }
}

if (-not $SkipQuality) {
    Invoke-Stage "Code quality and coverage" {
        & "$root/testing/quality/run-quality-local.ps1"
    }
}

if (-not $SkipUi) {
    Invoke-Stage "UI testing" {
        & "$root/testing/ui/run-ui-local.ps1"
    }
}

if (-not $SkipLoad) {
    Invoke-Stage "Load testing" {
        if (-not (Get-Command jmeter -ErrorAction SilentlyContinue)) {
            throw "jmeter is not on PATH"
        }
        if (Test-Path testing/load/report) { Remove-Item testing/load/report -Recurse -Force }
        jmeter -n -t testing/load/leave-load.jmx -l testing/load/results.jtl -e -o testing/load/report
    }
}

Write-Host "`n=========== SUMMARY ===========" -ForegroundColor Yellow
$results.GetEnumerator() | ForEach-Object {
    $colour = if ($_.Value -like "PASSED*") { "Green" } else { "Red" }
    Write-Host ("{0,-30} {1}" -f $_.Key, $_.Value) -ForegroundColor $colour
}

if ($results.Values | Where-Object { $_ -like "FAILED*" }) { exit 1 }