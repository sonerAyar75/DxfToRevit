# Architecture {#architecture}

## Layers

The solution separates *what* is imported (host-independent core) from *how* it is created in a
host application (adapter). Revit is only referenced by the add-in.

```
DxfToRevit.App / DxfToRevit.Console        DxfToRevit.Addin (Revit 2025)
              │                                   │  compiles DxfToRevit.Revit sources
              ▼                                   ▼  with REVIT_API_AVAILABLE
        DxfToRevit.Core  ◄─────────────────  DxfToRevit.Revit
   Walls · Columns · Openings              Walls · Columns · Openings · Import · Common
   Import · Dxf · Geometry · InMemory      planners (API-free) · writers · type resolvers
```

The core is organised **by feature**: every kind of element keeps everything it needs in its own
folder and namespace, from the layer or block rule to the import module. Shared parts have their
own folders. Folders and namespaces match, and every file holds one type.

| Namespace | Responsibility |
|-----------|----------------|
| `DxfToRevit.Core.Walls` | Wall layer rule, line classification, face pairing, centerlines, junctions, bridging, footprints, wall requests and validation, `IWallCreator`, `WallImportModule` |
| `DxfToRevit.Core.Columns` | Column layer rule, `DxfColumnReader`, column models, requests and validation, `IColumnCreator`, `ColumnImportModule` |
| `DxfToRevit.Core.Openings` | Door, window and opening definitions (block rules), marker reading, wall matching, hinge interpretation, type map, requests and validation, `IOpeningCreator`, `OpeningImportModule` |
| `DxfToRevit.Core.Import` | The modular import (`IImportModule`, `ImportCoordinator`), session options, placement, elements already in the model, `BuildingModelService` |
| `DxfToRevit.Core.Dxf` | Loading DXF files (`DxfReader`), extracting lines, dynamic block names, the analysis pipeline and the simulation output |
| `DxfToRevit.Core.Geometry` | Pure 2D algorithms shared by the features: segments, overlaps, intersections, polygons, tolerances |
| `DxfToRevit.Core.InMemory` | In-memory host used by tests and simulations |
| `DxfToRevit.Core.Composition` | Dependency injection registrations |
| `DxfToRevit.Revit.Walls` / `.Columns` / `.Openings` | Revit side of each feature: argument mappers, API-free type planners, writers, type resolvers, the `I…Creator` adapters |
| `DxfToRevit.Revit.Import` | The import batch (unit of work), its failure handler, the Revit warning log, the reader of elements already in the model |
| `DxfToRevit.Revit.Common` | The document context facade, the level resolver, unit conversion, placement, sub-transactions, parameter helpers |
| `DxfToRevit.Addin` | Ribbon application, commands, settings dialog, module catalog |

A new kind of element (for example floors) is a new feature folder; see @ref import_modules.

## Design principles

- **Host independence.** The core never references Revit. Walls and columns are described by
  `WallCreationRequest` and `ColumnCreationRequest` (millimetres) and created through the
  `IWallCreator` / `IColumnCreator` interfaces.
- **Testable decisions.** Every decision that does not need the Revit API is made by an API-free
  class: unit conversion (`LengthUnitConverter`), argument mapping (`WallCreateArgsMapper`,
  `ColumnCreateArgsMapper`), level and type selection (`LevelMatchPlanner`, `WallTypeMatchPlanner`,
  `ColumnTypeMatchPlanner`), family file search (`LibraryFileSearch`, `RoundColumnFamilyLocator`)
  and freeform family naming (`FreeformColumnFamilyNaming`). Only the writers (`RevitWallWriter`,
  `RevitColumnWriter`, `RevitColumnSolidWriter`, `RevitOpeningWriter`), the resolvers (`RevitLevelResolver`,
  `RevitWallTypeResolver`, `RevitColumnTypeResolver`, `RevitOpeningTypeResolver`), `RevitColumnPlacement`
  and `RevitFreeformColumnFamilyBuilder` call the API.
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
                                   ├── RevitApiDocumentContext (real document, a facade)
                                   │      ├── RevitWallWriter    ── RevitWallTypeResolver
                                   │      ├── RevitColumnWriter  ── RevitColumnTypeResolver, RevitColumnSolidWriter
                                   │      ├── RevitOpeningWriter ── RevitOpeningTypeResolver (batch participant)
                                   │      └── RevitLevelResolver (shared)
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
