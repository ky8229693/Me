[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Za-z0-9._-]+\.dump$')]
    [string]$FileName,

    [Parameter(Mandatory)]
    [switch]$ConfirmRestore
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$backupDirectory = Join-Path $repositoryRoot 'backups'
$backupPath = Join-Path $backupDirectory $FileName

if (-not (Test-Path -LiteralPath $backupPath -PathType Leaf)) {
    throw "Backup file does not exist: $backupPath"
}

$resolvedBackupDirectory = (Resolve-Path -LiteralPath $backupDirectory).Path.TrimEnd('\')
$resolvedBackupPath = (Resolve-Path -LiteralPath $backupPath).Path
if (-not $resolvedBackupPath.StartsWith("$resolvedBackupDirectory\", [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The selected backup must remain inside the repository backups directory.'
}

Push-Location $repositoryRoot
try {
    Write-Warning 'This operation replaces database objects with the selected backup.'
    & docker compose stop gateway api
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to stop application services (exit code $LASTEXITCODE)."
    }

    & docker compose exec -T database sh -c `
        'pg_restore --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --clean --if-exists --no-owner --no-privileges "/backups/$1"' `
        -- $FileName

    if ($LASTEXITCODE -ne 0) {
        throw "Database restore failed with exit code $LASTEXITCODE."
    }
}
finally {
    & docker compose start api gateway
    Pop-Location
}

Write-Host "Restore completed from: $resolvedBackupPath"
