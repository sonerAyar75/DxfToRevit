# DxfToRevit {#mainpage}

**DxfToRevit** turns 2D architectural DXF drawings into Autodesk Revit walls and columns.
It reads wall face lines and column outlines from layer-coded DXF files, computes wall
centerlines with correct L/T junctions, and creates native Revit elements bound to levels.

## Documentation map

| Page | Contents |
|------|----------|
| @subpage user_guide | For users: preparing the drawing in AutoCAD, importing into Revit, the type map file, the summary |
| @subpage architecture | Projects, layers, dependencies and the main types |
| @subpage pipeline | The DXF → centerline → footprint algorithm, stage by stage |
| @subpage columns | How column sections are classified and created in Revit |
| @subpage openings | Doors, windows and empty openings: DXF markers, wall matching, type mapping |
| @subpage revit_addin | Ribbon commands, import settings, transactions, deployment |
| @subpage installation | Building and using the Inno Setup installer |
| @subpage testing | Test projects and how to run them |
| @subpage documentation | How this documentation is generated |
| @subpage development_history | Development stages, the requirement behind each class and its key methods |

The API reference (namespaces, classes, members) is available from the navigation tree.

## Features

- Wall detection from parallel face lines on `WALL-<cm>` layers (`WALL-20` = 200 mm).
- Centerlines between overlapping faces, with gaps bridged where only one side is open.
- L and T junctions with angle- and thickness-aware extension limits.
- Thickness-aware collinear merge (walls of different thickness on one axis stay separate).
- Columns from closed polylines and circles on `COLUMN*` layers:
  rectangular (also rotated), circular and freeform sections.
- Revit add-in: Basic Walls and column family instances bound to a base level and either a
  top level or an unconnected height; freeform columns as generated, level-bound families.
- DXF simulation output for checking the result in any CAD viewer before importing.

## Solution

| Project | Role |
|---------|------|
| `DxfToRevit.Core` | Geometry, DXF reading, models, layer rules, pipeline, host-independent building API |
| `DxfToRevit.Revit` | Revit adapter: API-free create planners plus the Revit document context |
| `DxfToRevit.Addin` | Revit 2025 add-in (ribbon, settings dialog, import command) |
| `DxfToRevit.App` | Windows desktop tool: DXF → simulation DXF |
| `DxfToRevit.Console` | Command line tool: DXF → simulation DXF |
| `DxfToRevit.Tests` | Core unit tests (xUnit) |
| `DxfToRevit.Revit.Tests` | Adapter tests (mapping, planners, smoke, skipped integration tests) |
| `scripts/IconGenerator` | Generates the light/dark ribbon icons |

## Quick start

```bash
# Build and run all tests
dotnet test DxfToRevit.Tests
dotnet test DxfToRevit.Revit.Tests

# Simulation DXF from the command line (writes <input>_centerline.dxf)
dotnet run --project DxfToRevit.Console -- Samples/Sample.dxf

# Build and deploy the Revit add-in (Revit must be closed)
dotnet build DxfToRevit.Addin
```

## Conventions

| Item | Convention |
|------|------------|
| Units | DXF coordinates are millimetres; Revit values are converted to feet (1 ft = 304.8 mm) at the adapter boundary |
| Wall layers | `WALL` or `WALL-*`; `WALL-<n>` encodes the thickness as n × 10 mm |
| Column layers | `COLUMN`, `COLUMNS` or `COLUMN-*` |
| Tolerances | Points/collinearity 1 mm, parallelism 0.1° (`GeometryTolerance`) |
