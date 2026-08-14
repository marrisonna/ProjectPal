# Runs the read-only smoke test against the running database and prints the results.
# Usage: .\scripts\verify.ps1

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
    $envMap = Read-EnvFile (Join-Path $root ".env")
    $pgUser = if ($envMap.ContainsKey('POSTGRES_USER')) { $envMap['POSTGRES_USER'] } else { 'projectpal' }
    $pgDb   = if ($envMap.ContainsKey('POSTGRES_DB'))   { $envMap['POSTGRES_DB']   } else { 'projectpal' }

    Get-Content (Join-Path $root "database\verify\smoke_test.sql") -Raw |
        docker compose exec -T db psql -U $pgUser -d $pgDb -v ON_ERROR_STOP=1
}
finally {
    Pop-Location
}
