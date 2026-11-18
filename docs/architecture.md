# Architecture {#architecture}

## Layers

The solution separates *what* is imported (host-independent core) from *how* it is created in a
host application (adapter). Revit is only referenced by the add-in.

```
DxfToRevit.App / DxfToRevit.Console        DxfToRevit.Addin (Revit 2025)
              │                                   │  compiles DxfToRevit.Revit sources
              ▼                                   ▼  with REVIT_API_AVAILABLE
        DxfToRevit.Core  ◄─────────────────  DxfToRevit.Revit
   Dxf · Geometry · Models · Rules         planners (API-free) · RevitApiDocumentContext
   Services · IO · Building · Composition  RevitElementResolver · family builders
```

| Namespace | Responsibility |
|-----------|----------------|
| `DxfToRevit.Core.Dxf` | Loading DXF files (`DxfReader`), extracting lines (`DxfGeometryReader`) and columns (`DxfColumnReader`) |
| `DxfToRevit.Core.Rules` | Layer naming conventions (`WallLayerRule`, `ColumnLayerRule`) |
| `DxfToRevit.Core.Models` | Immutable data: raw/classified lines, face pairs, walls, footprints, columns |
| `DxfToRevit.Core.Geometry` | Pure 2D algorithms: segments, overlaps, junctions, bridging, merging, polygons |
| `DxfToRevit.Core.Services` | Line classification, face pairing, wall models and the end-to-end pipeline |
| `DxfToRevit.Core.IO` | Writing the DXF simulation output |
| `DxfToRevit.Core.Building` | Host-independent creation API: requests, factories, validators, creator interfaces |
| `DxfToRevit.Core.Building.InMemory` | In-memory host used by tests and simulations |
| `DxfToRevit.Core.Composition` | Dependency injection registrations |
| `DxfToRevit.Revit` | Revit adapter: unit conversion, create-argument planners, document context, resolvers |
| `DxfToRevit.Addin` | Ribbon application, commands, settings dialog |

## Design principles

- **Host independence.** The core never references Revit. Walls and columns are described by
  `WallCreationRequest` and `ColumnCreationRequest` (millimetres) and created through the
  `IWallCreator` / `IColumnCreator` interfaces.
- **Testable decisions.** Every decision that does not need the Revit API is made by an API-free
  class: unit conversion (`LengthUnitConverter`), argument mapping (`WallCreateArgsMapper`,
  `ColumnCreateArgsMapper`), level and type selection (`LevelMatchPlanner`, `WallTypeMatchPlanner`,
  `ColumnTypeMatchPlanner`), family file search (`LibraryFileSearch`, `RoundColumnFamilyLocator`)
  and freeform family naming (`FreeformColumnFamilyNaming`). Only the thin `RevitApiDocumentContext`,
  `RevitElementResolver`, `RevitColumnPlacement` and `RevitFreeformColumnFamilyBuilder` call the API.
- **Composition over inheritance.** There is no class hierarchy; types are `sealed` and collaborate
  through constructor injection. Parameterless constructors create default collaborators for
  simple callers; `ServiceCollectionExtensions.AddDxfToRevitCore` registers everything for DI.
- **No silent side effects.** `DxfCenterLinePipeline.Analyze` never writes files; only `Run` writes
  the simulation DXF. The Revit add-in uses `Analyze`.

## Key types and relationships

```
«interface» IWallCreator                   «interface» IColumnCreator
    ├── InMemoryWallCreator                    ├── InMemoryColumnCreator
    └── RevitWallCreator ─┐                    └── RevitColumnCreator ─┐
                          └──► IRevitDocumentContext ◄─────────────────┘
                                   ├── RevitApiDocumentContext (real document)
                                   └── NullRevitDocumentContext (no document)

BuildingModelService  uses → IWallCreator, IColumnCreator, WallCreationValidator, ColumnCreationValidator
BuildingExportFromDxf uses → BuildingModelService, WallCreationRequestFactory,
                             ColumnCreationRequestFactory, DxfColumnReader
WallCreationRequest   has  → WallTypeProperties, ElementPlacement
ColumnCreationRequest has  → ColumnTypeProperties, ElementPlacement, plan rotation
```

## Data flow

```
DxfDocument
  → DxfLineModel → ClassifiedDxfLine → WallFacePair → WallModel
  → LineSegment2D (centerlines) → WallSolid2D → WallCreationRequest → WallCreateArgs → Revit Wall
  → ColumnModel → ColumnCreationRequest → ColumnCreateArgs → Revit column / DirectShape
  → WallCreationResult / ColumnCreationResult
```

See @ref pipeline for the geometry stages and @ref columns for column creation.
