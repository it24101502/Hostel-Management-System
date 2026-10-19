# testing/ui/run-ui-local.ps1
# Runs the Selenium suite. Start the stack, seed the accounts and run the
# local frontend first (see README, "Selenium UI / end-to-end tests").
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $root
if (-not $env:E2E_HEADLESS) { $env:E2E_HEADLESS = "1" }
py -m pip install -r tests/ui_e2e/requirements.txt
py tests/ui_e2e/run_tests.py
if ($LASTEXITCODE -ne 0) { throw "Selenium tests failed" }