# Stops the container and DELETES the local database volume, so the next
# .\scripts\setup.ps1 rebuilds everything from scratch. Destructive — asks first.
# Usage: .\scripts\reset.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Push-Location $root
try {
    Write-Host "This will DELETE the local 'projectpal-db-data' volume and everything in it." -ForegroundColor Yellow
    $confirm = Read-Host "Type 'yes' to continue"
    if ($confirm -ne 'yes') {
        Write-Host "Cancelled — nothing was changed."
        return
    }
    docker compose down -v
    Write-Host "Volume removed. Run .\scripts\setup.ps1 to recreate the database from scratch." -ForegroundColor Green
}
finally {
    Pop-Location
}
