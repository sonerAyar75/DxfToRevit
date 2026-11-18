# DxfToRevit.Revit.Tests

Tests for the Revit adapter (`DxfToRevit.Revit`). The core unit tests live in `DxfToRevit.Tests`.

## Folders

| Folder | Purpose | Needs Revit |
|--------|---------|-------------|
| `Mapping/` | Unit conversion, create-argument mapping, level/type planners, family search and naming | No |
| `Smoke/` | Adapter behaviour without a document (stubs fail safely) | No |
| `Integration/` | Wall and column creation in a real document; marked `Skip` | Yes |

## Running

```bash
# API-free build (default)
dotnet test DxfToRevit.Revit.Tests

# Also compile against the real Revit API (Windows, Revit 2025 installed)
dotnet test DxfToRevit.Revit.Tests -p:RevitApiDir="C:\Program Files\Autodesk\Revit 2025"
```

The integration tests are templates for a test runner hosted inside Revit: remove `Skip` and
provide a document through `RevitDocumentFixture` when such a host is available.

## Create arguments

The values sent to Revit are produced and tested without the API:

```text
ElementPlacement      → PlacementCreateArgsMapper → PlacementCreateArgs
WallCreationRequest   → WallCreateArgsMapper      → WallCreateArgs
ColumnCreationRequest → ColumnCreateArgsMapper    → ColumnCreateArgs
RevitElementCreatePlanner → PlanWall / PlanColumn
```

`RevitApiDocumentContext` uses the same arguments when it writes to the document.
