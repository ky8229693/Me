[CmdletBinding()]
param(
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$certificateDirectory = Join-Path $repositoryRoot 'certs'
$certificatePath = Join-Path $certificateDirectory 'localhost.crt'
$privateKeyPath = Join-Path $certificateDirectory 'localhost.key'

New-Item -ItemType Directory -Force -Path $certificateDirectory | Out-Null

if (-not $Force -and ((Test-Path -LiteralPath $certificatePath) -or (Test-Path -LiteralPath $privateKeyPath))) {
    throw 'localhost certificate files already exist. Use -Force only when you intend to replace them.'
}

if ($Force) {
    Remove-Item -LiteralPath $certificatePath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $privateKeyPath -Force -ErrorAction SilentlyContinue
}

& dotnet dev-certs https `
    --export-path $certificatePath `
    --format Pem `
    --no-password `
    --trust

if ($LASTEXITCODE -ne 0) {
    throw "dotnet dev-certs failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path -LiteralPath $certificatePath) -or -not (Test-Path -LiteralPath $privateKeyPath)) {
    throw 'Certificate generation completed without producing both certs/localhost.crt and certs/localhost.key.'
}

Write-Host "Created and trusted $certificatePath"
Write-Host "Created private key $privateKeyPath (ignored by Git)"
