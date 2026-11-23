# DxfToRevit

Imports walls and columns from 2D architectural DXF drawings into Autodesk Revit.

DxfToRevit reads wall face lines and column outlines from layer-coded DXF files, computes wall
centerlines with correct L/T junctions, and creates native Revit walls and columns bound to levels.
The geometry core is host-independent and fully unit-tested; the Revit add-in is a thin adapter.

## Features

- Walls from parallel face lines on `WALL-<cm>` layers (`WALL-20` = 200 mm).
- Centerlines between overlapping faces; one-side-open gaps bridged, real openings preserved.
- L and T junctions with angle- and thickness-aware extension limits.
- Columns from closed polylines and circles on `COLUMN*` layers: rectangular (also rotated),
  circular (automatic round family, cylinder fallback) and freeform (generated level-bound families).
- Doors, windows and empty openings from rectangular dynamic blocks (`door`, `window`, `opening`):
  width from the block, depth from the host wall, heights from attributes, hinge and swing from a
  circle, Revit types mapped per `TYPE` code (picked from loaded types or read from a file).
- Revit 2025 ribbon with light/dark theme icons, import settings dialog and a single undo step per import.
- DXF simulation output (`WALL-CENTERLINE`, `WALL-SOLID`, `COLUMN-SIM`, `OPENING-SIM`) for checking results without Revit.

## Projects

| Project | Role |
|---------|------|
| `DxfToRevit.Core` | Geometry, DXF reading, models, layer rules, pipeline, host-independent building API |
| `DxfToRevit.Revit` | Revit adapter (API-free planners + Revit document context) |
| `DxfToRevit.Addin` | Revit 2025 add-in |
| `DxfToRevit.App` | Windows desktop tool: DXF → simulation DXF |
| `DxfToRevit.Console` | Command line tool: DXF → simulation DXF |
| `DxfToRevit.Tests`, `DxfToRevit.Revit.Tests` | xUnit tests |
| `scripts/IconGenerator` | Ribbon icon generator |

## Getting started

Requirements: .NET 8 SDK; Revit 2025 for the add-in.

```bash
# Tests
dotnet test DxfToRevit.Tests
dotnet test DxfToRevit.Revit.Tests

# Command line: writes <input>_centerline.dxf next to the input
dotnet run --project DxfToRevit.Console -- Samples/Sample.dxf

# Desktop tool
dotnet run --project DxfToRevit.App

# Revit add-in: builds and deploys to %APPDATA%\Autodesk\Revit\Addins\2025 (close Revit first)
dotnet build DxfToRevit.Addin
```

## Installer

An Inno Setup 7 installer for the Revit add-in is built with:

```powershell
./scripts/build-installer.ps1
```

Output: `installer/Output/DxfToRevit-<version>-Revit2025-Setup.exe`. It installs per user by
default (no administrator rights) or for all users. See [installation](docs/installation.md).

## Documentation

The full documentation (guides and API reference) is generated with Doxygen:

```powershell
./scripts/generate-docs.ps1 -Open
```

```bash
./scripts/generate-docs.sh
```

Output: `doxygen_out/html/index.html`. The guide pages are in [`docs/`](docs):
[architecture](docs/architecture.md), [pipeline](docs/pipeline.md), [columns](docs/columns.md),
[openings](docs/openings.md), [Revit add-in](docs/revit_addin.md), [installation](docs/installation.md), [testing](docs/testing.md)
[documentation](docs/documentation.md) and [development history](docs/development_history.md).

## Ribbon icons

The icons in `DxfToRevit.Addin/Resources` (including `DxfToRevit.ico` for the installer) are generated:

```bash
dotnet run --project scripts/IconGenerator -- DxfToRevit.Addin/Resources
```

## License

DxfToRevit is released under the [MIT License](LICENSE.txt). Third-party components and their
licenses are listed in [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt).
