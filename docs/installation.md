# Installer {#installation}

The add-in is distributed as an Inno Setup 7 installer built from `installer/DxfToRevit.iss`.

## Building the installer

Requirements: .NET 8 SDK, Revit 2025 (for the Revit API references) and
[Inno Setup 7](https://jrsoftware.org/isinfo.php).

```powershell
./scripts/build-installer.ps1             # build the add-in (Release) and the installer
./scripts/build-installer.ps1 -SkipBuild  # reuse the existing add-in build output
```

The script builds `DxfToRevit.Addin` with `-p:DeployAddin=false` (so nothing is copied into your
own Revit folder) and compiles the script with `ISCC`. The result is
`installer/Output/DxfToRevit-<version>-Revit2025-Setup.exe`.

The version is taken from `DxfToRevit.Addin.dll`, i.e. from `<Version>` in `Directory.Build.props`.
Change it there for a new release.

## What the installer does

| Item | Installed to |
|------|--------------|
| `DxfToRevit.addin` (manifest) | `<Addins>` |
| Add-in assemblies, `DxfToRevit.Addin.deps.json` | `<Addins>\DxfToRevit` |
| `LICENSE.txt`, `THIRD-PARTY-NOTICES.txt` | `<Addins>\DxfToRevit` |
| Ribbon icons, application icon | `<Addins>\DxfToRevit\Resources` |
| Empty folder for an optional round column family | `<Addins>\DxfToRevit\Resources\Families` |
| Uninstaller | `<Addins>\DxfToRevit\Uninstall` |

`<Addins>` depends on the choice made at the start of the installation:

| Choice | `<Addins>` | Administrator rights |
|--------|------------|----------------------|
| Install for me only (default) | `%APPDATA%\Autodesk\Revit\Addins\2025` | No |
| Install for all users | `%ProgramData%\Autodesk\Revit\Addins\2025` | Yes |

Further behaviour:

- **64-bit installer** (`SetupArchitecture=x64`), because Revit is 64-bit only; Windows 10 or later.
- **Revit check**: if `Revit.exe` of the target version is not found in its default folder, the
  installer asks whether to continue.
- **Duplicate registration**: Revit reports an error when the same add-in is registered both for
  the current user and for all users. If a manifest exists in the other location (for example from
  a development build), the installer offers to remove it.
- **Upgrades** install over the previous version (same `AppId`); icons of older versions are
  removed. If Revit is running and the add-in files are in use, the installer offers to close it.
- **Uninstall** is available from *Settings > Apps* as "DxfToRevit for Revit 2025".

## Silent installation

```bat
DxfToRevit-2.1.0-Revit2025-Setup.exe /VERYSILENT /SUPPRESSMSGBOXES /CURRENTUSER
DxfToRevit-2.1.0-Revit2025-Setup.exe /VERYSILENT /SUPPRESSMSGBOXES /ALLUSERS
```

With `/SUPPRESSMSGBOXES` the questions above take their default answer: continue without Revit,
and keep (do not remove) a duplicate registration.

## Other Revit versions

The add-in is compiled against the Revit 2025 API. For another version, build against that
version's API and pass it to both steps:

```powershell
./scripts/build-installer.ps1 -RevitVersion 2026
```

This sets `RevitVersion` for the add-in build (API path and default deploy folder) and for the
installer (target folder, Revit check and names).
