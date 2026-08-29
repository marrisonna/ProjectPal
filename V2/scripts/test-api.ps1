# Brings the stack up (db + rest-api) and runs the REST API test suite.
# Mirrors setup.ps1/verify.ps1's shape. Usage: .\scripts\test-api.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$apiRoot = Join-Path $root "rest-api"
$venv = Join-Path $apiRoot ".venv-test"

Push-Location $root
try {
    Write-Host "Starting the stack (db + rest-api)..." -ForegroundColor Cyan
    # docker compose writes its normal progress output to stderr, which
    # PowerShell treats as a terminating error under $ErrorActionPreference =
    # "Stop" even on success (same flake noted in setup.ps1) - relax it just
    # for this call and check the real exit code instead.
    $prevEAP = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    docker compose up -d --build
    $composeExitCode = $LASTEXITCODE
    $ErrorActionPreference = $prevEAP
    if ($composeExitCode -ne 0) { throw "docker compose up failed - is Docker Desktop running?" }

    Write-Host "Waiting for the REST API to respond..." -ForegroundColor Cyan
    $ready = $false
    for ($i = 1; $i -le 30; $i++) {
        try {
            Invoke-WebRequest -Uri "http://127.0.0.1:8000/openapi.json" -UseBasicParsing -TimeoutSec 2 | Out-Null
            $ready = $true
            break
        } catch {
            Start-Sleep -Seconds 2
        }
    }
    if (-not $ready) { throw "REST API did not become ready within 60 seconds. Check 'docker logs projectpal-rest-api'." }

    if (-not (Test-Path $venv)) {
        Write-Host "Creating test virtualenv..." -ForegroundColor Cyan
        python -m venv $venv
        & "$venv\Scripts\pip" install -q -r (Join-Path $apiRoot "tests\requirements-test.txt")
    }

    Write-Host "Running the test suite..." -ForegroundColor Cyan
    Push-Location $apiRoot
    try {
        & "$venv\Scripts\python" -m pytest tests -v
        $testExitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }

    if ($testExitCode -ne 0) { throw "Test suite failed." }
    Write-Host "All tests passed." -ForegroundColor Green
}
finally {
    Pop-Location
}
