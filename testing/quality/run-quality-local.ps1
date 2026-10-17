[CmdletBinding()]
param(
    [string]$ProjectKey = "HMS",
    [string]$SonarHost = $(if ($env:SONAR_HOST_URL) { $env:SONAR_HOST_URL } else { "http://localhost:9000" })
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $root

foreach ($tool in @(
    @{ Cmd = "reportgenerator"; Package = "dotnet-reportgenerator-globaltool" },
    @{ Cmd = "dotnet-sonarscanner"; Package = "dotnet-sonarscanner" })) {
    if (-not (Get-Command $tool.Cmd -ErrorAction SilentlyContinue)) {
        dotnet tool install --global $tool.Package
    }
}

$useSonar = [bool]$env:SONAR_TOKEN
Remove-Item TestResults -Recurse -Force -ErrorAction SilentlyContinue

if ($useSonar) {
    dotnet sonarscanner begin /k:$ProjectKey `
        /d:sonar.host.url=$SonarHost `
        /d:sonar.token=$env:SONAR_TOKEN `
        /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml" `
        /d:sonar.exclusions="**/bin/**,**/obj/**,frontend/node_modules/**,**/wwwroot/**" `
        /d:sonar.coverage.exclusions="**/Program.cs,**/Repositories/**,**/Events/Kafka*.cs,**/BackgroundServices/**,frontend/**"
    if ($LASTEXITCODE -ne 0) {
        throw "Sonar begin failed. Check that SonarQube is running at $SonarHost (docker start sonarqube), the token is valid and the project key '$ProjectKey' matches."
    }
}
else {
    Write-Host "SONAR_TOKEN not set: skipping Sonar, producing the coverage report only." -ForegroundColor Yellow
}

dotnet build backend/HostelManagement.sln -c Release
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

dotnet test backend/HostelManagement.sln -c Release --no-build `
    --settings testing/quality/coverlet.runsettings `
    --results-directory TestResults
$testExit = $LASTEXITCODE

if ($useSonar) {
    dotnet sonarscanner end /d:sonar.token=$env:SONAR_TOKEN
}

reportgenerator -reports:"TestResults/**/coverage.opencover.xml" `
    -targetdir:testing/quality/coverage-report `
    -reporttypes:"Html;JsonSummary"

$summary = (Get-Content testing/quality/coverage-report/Summary.json | ConvertFrom-Json).summary
Write-Host ("Line coverage:   {0}%" -f $summary.linecoverage) -ForegroundColor Green
Write-Host ("Branch coverage: {0}%" -f $summary.branchcoverage) -ForegroundColor Green

Start-Process (Resolve-Path testing/quality/coverage-report/index.html)

if ($testExit -ne 0) { throw "Some tests failed" }