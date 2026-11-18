<#
.SYNOPSIS
    Builds the DxfToRevit add-in and its Inno Setup 7 installer.

.DESCRIPTION
    1. Builds DxfToRevit.Addin in the given configuration without copying it into Revit's
       Addins folder (-p:DeployAddin=false).
    2. Compiles installer/DxfToRevit.iss with ISCC (Inno Setup 7).
    The installer is written to installer/Output.

    ISCC is taken from PATH, or from the default Inno Setup 7 folders (per-user and all-users).

.PARAMETER Configuration
    Build configuration of the add-in (default: Release).

.PARAMETER RevitVersion
    Target Revit version passed to the add-in build and the installer (default: 2025).

.PARAMETER SkipBuild
    Uses the existing add-in build output instead of building it again.

.EXAMPLE
    ./scripts/build-installer.ps1
.EXAMPLE
    ./scripts/build-installer.ps1 -SkipBuild
#>
param(
    [string]$Configuration = 'Release',
    [string]$RevitVersion = '2025',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Find-Iscc {
    $onPath = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }

    $candidates = @(
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 7\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 7\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 7\ISCC.exe')
    )
    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path $candidate)) { return $candidate }
    }
    return $null
}

$iscc = Find-Iscc
if (-not $iscc) {
    Write-Error 'ISCC.exe (Inno Setup 7) was not found. Install it from https://jrsoftware.org/isinfo.php or add it to PATH.'
    exit 1
}

$sourceDir = Join-Path $root "DxfToRevit.Addin\bin\$Configuration\net8.0-windows"

if (-not $SkipBuild) {
    Write-Host "Building DxfToRevit.Addin ($Configuration, Revit $RevitVersion)..."
    dotnet build DxfToRevit.Addin -c $Configuration -p:DeployAddin=false -p:RevitVersion=$RevitVersion
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if (-not (Test-Path (Join-Path $sourceDir 'DxfToRevit.Addin.dll'))) {
    Write-Error "Add-in build output not found in $sourceDir. Run without -SkipBuild."
    exit 1
}

Write-Host "Compiling installer with $iscc..."
& $iscc "/DSourceDir=$sourceDir" "/DRevitVersion=$RevitVersion" (Join-Path $root 'installer\DxfToRevit.iss')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$setup = Get-ChildItem (Join-Path $root 'installer\Output') -Filter '*.exe' |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
Write-Host "Installer: $($setup.FullName)"
