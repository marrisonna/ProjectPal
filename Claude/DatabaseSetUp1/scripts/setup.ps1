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

Push-Location $root
try {
    $envPath = Join-Path $root ".env"
    if (-not (Test-Path $envPath)) {
        Write-Host "No .env found — copying .env.example to .env." -ForegroundColor Yellow
        Copy-Item (Join-Path $root ".env.example") $envPath
        Write-Host "Edit .env and set a real POSTGRES_PASSWORD, then re-run this script." -ForegroundColor Yellow
        return
    }

    $envMap = Read-EnvFile $envPath
    $pgUser = if ($envMap.ContainsKey('POSTGRES_USER')) { $envMap['POSTGRES_USER'] } else { 'projectpal' }
    $pgDb   = if ($envMap.ContainsKey('POSTGRES_DB'))   { $envMap['POSTGRES_DB']   } else { 'projectpal' }

    Write-Host "Starting PostgreSQL container..." -ForegroundColor Cyan
    docker compose up -d
    if ($LASTEXITCODE -ne 0) { throw "docker compose up failed — is Docker Desktop running?" }

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

    Write-Host "Applying schema migrations..." -ForegroundColor Cyan
    Get-ChildItem (Join-Path $root "database\migrations") -Filter "*.sql" | Sort-Object Name | ForEach-Object {
        Write-Host "  -> $($_.Name)"
        Get-Content $_.FullName -Raw | docker compose exec -T db psql -U $pgUser -d $pgDb -v ON_ERROR_STOP=1
        if ($LASTEXITCODE -ne 0) { throw "Migration $($_.Name) failed." }
    }

    if (-not $SkipSeed) {
        Write-Host "Loading example data..." -ForegroundColor Cyan
        Get-ChildItem (Join-Path $root "database\seed") -Filter "*.sql" | Sort-Object Name | ForEach-Object {
            Write-Host "  -> $($_.Name)"
            Get-Content $_.FullName -Raw | docker compose exec -T db psql -U $pgUser -d $pgDb -v ON_ERROR_STOP=1
            if ($LASTEXITCODE -ne 0) { throw "Seed script $($_.Name) failed." }
        }
    }

    Write-Host "Done. Run .\scripts\verify.ps1 to sanity-check the install." -ForegroundColor Green
}
finally {
    Pop-Location
}
