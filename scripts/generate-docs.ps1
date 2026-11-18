<#
.SYNOPSIS
    Generates the DxfToRevit documentation with Doxygen.

.DESCRIPTION
    Runs doxygen with the repository Doxyfile. The output is written to doxygen_out/html and the
    warnings to doxygen_out/doxygen_warnings.log. Doxygen is taken from PATH, or from the default
    installation folder "C:\Program Files\doxygen\bin".

.PARAMETER Open
    Opens doxygen_out/html/index.html in the default browser when generation succeeds.

.EXAMPLE
    ./scripts/generate-docs.ps1 -Open
#>
param(
    [switch]$Open
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$doxygen = (Get-Command doxygen -ErrorAction SilentlyContinue).Source
if (-not $doxygen) {
    $candidate = Join-Path $env:ProgramFiles 'doxygen\bin\doxygen.exe'
    if (Test-Path $candidate) { $doxygen = $candidate }
}
if (-not $doxygen) {
    Write-Error 'doxygen was not found. Install it from https://www.doxygen.nl or add it to PATH.'
    exit 1
}

New-Item -ItemType Directory -Force -Path (Join-Path $root 'doxygen_out') | Out-Null
Write-Host "Using: $doxygen"
& $doxygen Doxyfile
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$index = Join-Path $root 'doxygen_out\html\index.html'
$log = Join-Path $root 'doxygen_out\doxygen_warnings.log'
$warnings = if (Test-Path $log) { (Get-Content $log | Where-Object { $_.Trim() }).Count } else { 0 }
Write-Host "Documentation: $index"
Write-Host "Warnings: $warnings (see $log)"

if ($Open) { Start-Process $index }
