[CmdletBinding()]
param(
    [ValidatePattern('^[A-Za-z0-9._-]+\.dump$')]
    [string]$FileName = "web_demo_$(Get-Date -Format 'yyyyMMdd_HHmmss').dump"
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$backupDirectory = Join-Path $repositoryRoot 'backups'
New-Item -ItemType Directory -Force -Path $backupDirectory | Out-Null

$backupPath = Join-Path $backupDirectory $FileName
if (Test-Path -LiteralPath $backupPath) {
    throw "Backup already exists: $backupPath"
}

Push-Location $repositoryRoot
try {
    & docker compose exec -T database sh -c `
        'pg_dump --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --format=custom --file "/backups/$1"' `
        -- $FileName

    if ($LASTEXITCODE -ne 0) {
        throw "Database backup failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path -LiteralPath $backupPath)) {
    throw "Database reported success but the backup file was not created: $backupPath"
}

Write-Host "Backup created: $backupPath"
