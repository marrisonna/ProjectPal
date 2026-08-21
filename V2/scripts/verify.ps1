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
    $psqlArgs = @('compose', 'exec', '-T', 'db', 'psql', "--username=$pgUser", "--dbname=$pgDb", '-v', 'ON_ERROR_STOP=1')
    $sqlPath = Join-Path $root "database\verify\smoke_test.sql"

    # Docker Desktop's CLI on Windows has occasionally been observed to drop
    # or scramble arguments to `docker compose exec` - sometimes landing on
    # docker's own usage banner with exit code 0, which a plain exit-code
    # check can't catch. Retry a few times before giving up; this is a
    # read-only query with no side effects either way.
    $maxAttempts = 4
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        $prevEAP = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        $output = Get-Content $sqlPath -Raw | & docker @psqlArgs 2>&1
        $exitCode = $LASTEXITCODE
        $ErrorActionPreference = $prevEAP
        $output | ForEach-Object { Write-Host $_ }
        $gotUsageBanner = ($output -join "`n") -match 'Usage:\s+docker \[OPTIONS\]'
        if ($exitCode -eq 0 -and -not $gotUsageBanner) { break }
        Write-Host "  (attempt $attempt of $maxAttempts failed, retrying...)" -ForegroundColor Yellow
        if ($attempt -eq $maxAttempts) { throw "Failed to run smoke_test.sql after $maxAttempts attempts." }
        Start-Sleep -Seconds 2
    }
}
finally {
    Pop-Location
}
