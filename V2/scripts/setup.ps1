# Starts the PostgreSQL container and applies migrations + (optionally) example data.
# Usage:
#   .\scripts\setup.ps1              # apply migrations and load example data
#   .\scripts\setup.ps1 -SkipSeed    # apply migrations only, no example data

param(
    [switch]$SkipSeed
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

function Read-EnvFile($path) {
    $map = @{}
    Get-Content $path | Where-Object { $_ -match '^\s*[^#].*=' } | ForEach-Object {
        $parts = $_ -split '=', 2
        $map[$parts[0].Trim()] = $parts[1].Trim()
    }
    return $map
}

# Docker Desktop's CLI on Windows has been observed to occasionally drop or
# scramble arguments to `docker compose exec` (e.g. psql connecting as the
# wrong role, or receiving no arguments at all, which prints docker's own
# usage banner and exits 0 - a plain exit-code check can't catch that, so we
# also check the output for that banner). Every .sql file here is wrapped in
# its own BEGIN/COMMIT, so a failed attempt always rolls back cleanly -
# retrying a few times is safe and works around the flake.
function Invoke-SqlFile($path, $psqlArgs) {
    $maxAttempts = 4
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        $prevEAP = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        $output = Get-Content $path -Raw | & docker @psqlArgs 2>&1
        $exitCode = $LASTEXITCODE
        $ErrorActionPreference = $prevEAP
        $output | ForEach-Object { Write-Host $_ }
        $gotUsageBanner = ($output -join "`n") -match 'Usage:\s+docker \[OPTIONS\]'
        if ($exitCode -eq 0 -and -not $gotUsageBanner) { return }
        Write-Host "  (attempt $attempt of $maxAttempts failed, retrying...)" -ForegroundColor Yellow
        Start-Sleep -Seconds 2
    }
    throw "Failed to apply $path after $maxAttempts attempts."
}

Push-Location $root
try {
    $envPath = Join-Path $root ".env"
    if (-not (Test-Path $envPath)) {
        Write-Host "No .env found - copying .env.example to .env." -ForegroundColor Yellow
        Copy-Item (Join-Path $root ".env.example") $envPath
        Write-Host "Edit .env and set a real POSTGRES_PASSWORD, then re-run this script." -ForegroundColor Yellow
        return
    }

    $envMap = Read-EnvFile $envPath
    $pgUser = if ($envMap.ContainsKey('POSTGRES_USER')) { $envMap['POSTGRES_USER'] } else { 'projectpal' }
    $pgDb   = if ($envMap.ContainsKey('POSTGRES_DB'))   { $envMap['POSTGRES_DB']   } else { 'projectpal' }
    $psqlArgs = @('compose', 'exec', '-T', 'db', 'psql', "--username=$pgUser", "--dbname=$pgDb", '-v', 'ON_ERROR_STOP=1')

    Write-Host "Starting PostgreSQL container..." -ForegroundColor Cyan
    docker compose up -d
    if ($LASTEXITCODE -ne 0) { throw "docker compose up failed - is Docker Desktop running?" }

    Write-Host "Waiting for PostgreSQL to become healthy..." -ForegroundColor Cyan
    $healthy = $false
    $prevEAP = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    for ($i = 1; $i -le 30; $i++) {
        $status = docker inspect --format='{{.State.Health.Status}}' projectpal-db 2>$null
        if ($status -eq "healthy") { $healthy = $true; break }
        Start-Sleep -Seconds 2
    }
    $ErrorActionPreference = $prevEAP
    if (-not $healthy) { throw "PostgreSQL did not become healthy within 60 seconds." }

    # Brief settle time: back-to-back `docker inspect` polls immediately
    # followed by `docker compose exec` have been observed to trigger the
    # CLI flake described above; a short pause avoids piling more requests
    # on top of Docker Desktop right after container startup.
    Start-Sleep -Seconds 2

    Write-Host "Applying schema migrations..." -ForegroundColor Cyan
    Get-ChildItem (Join-Path $root "database\migrations") -Filter "*.sql" | Sort-Object Name | ForEach-Object {
        Write-Host "  -> $($_.Name)"
        Invoke-SqlFile $_.FullName $psqlArgs
    }

    if (-not $SkipSeed) {
        Write-Host "Loading example data..." -ForegroundColor Cyan
        Get-ChildItem (Join-Path $root "database\seed") -Filter "*.sql" | Sort-Object Name | ForEach-Object {
            Write-Host "  -> $($_.Name)"
            Invoke-SqlFile $_.FullName $psqlArgs
        }
    }

    Write-Host "Done. Run .\scripts\verify.ps1 to sanity-check the install." -ForegroundColor Green
}
finally {
    Pop-Location
}
