# Import modules {#import_modules}

[TOC]

Every kind of building element the import creates is an **import module** (`IImportModule`):
walls, columns and openings today, floors, rooms or roofs later. The import flow, the category
check boxes of the settings dialog and the import summary work with every module the same way, so
a new kind of element does not touch them.

## The parts

| Type | Project | Role |
|------|---------|------|
| `IImportModule` | Core | One kind of element: its categories, its place in the import, planning and creation |
| `ImportCategory`, `ImportCategories` | Core | A check box of the settings dialog (`Walls`, `Doors`, ...); a module offers one or more |
| `ImportCoordinator` | Core | Orders the modules, lets them plan, then create, and regenerates the model where asked |
| `DxfDrawing` | Core | The input of the modules: the loaded DXF document and the analysis of walls, columns and openings |
| `LayerRules` | Core | Which layers hold wall faces and columns; data, so another office standard is another set of rules |
| `ImportPlanContext` | Core | What a module needs to plan: the drawing, the session options, the elements already in the model, the chosen categories, the module options |
| `ImportBag` | Core | Typed values: module options set by the host, and data a module hands to later modules |
| `ImportModuleResult`, `ImportEntry`, `ImportReportSection` | Core | What a module created, failed to create and wants to report |
| `ImportModuleCatalog` | Add-in | The modules of the add-in, wired to the Revit creators |

Modules are host-independent: they create through `BuildingModelService` and the creator
interfaces, so the whole flow runs in tests with the in-memory creators (`ImportCoordinatorTests`).

## The flow

```
ImportModuleCatalog.Create(context) ─► ImportCoordinator (modules ordered by Order)
settings dialog  ◄── categories and counts (IImportModule.Categories, CountInDrawing)
Plan:   every module, in order ── requests, elements already in the model, ImportBag values
Create: every module, in order, inside RevitImportBatch ── Regenerate after RegenerateAfter modules
commit ─► ImportModuleResult.WithCommitOutcome (removed elements, notes) ─► summary
```

| Module | Order | Categories | Provides | Uses |
|--------|-------|------------|----------|------|
| `WallImportModule` | 100 | Walls | `HostWallMap` (walls created now or already in the model) | |
| `ColumnImportModule` | 200 | Columns | | `ColumnImportOptions` |
| `OpeningImportModule` | 300 | Doors, Windows, Empty openings | marker sections in the summary | `HostWallMap`, `OpeningImportOptions` |

`Plan` is called for every module, also when none of its categories is chosen: the wall module
then creates nothing but still finds the walls already in the model, so doors and windows can be
imported alone.

## Adding a module

For example floors drawn as closed polylines on a `FLOOR-<cm>` layer:

1. **Read and interpret** the DXF entities in a `Core/Floors` folder: a layer rule (like
   `ColumnLayerRule`, with the names as data), a reader and a model (like `DxfColumnReader` and
   `ColumnModel`). The reader reads `DxfDrawing.Document`, so the analysis
   (`DxfCenterLinePipeline`) does not change. Only elements that depend on the wall analysis
   (bridged gaps, wall footprints) belong in the analysis itself.
2. **Request and creator**: a creation request, a validator, an `IFloorCreator` interface with an
   in-memory implementation, and the Revit side in `DxfToRevit.Revit/Floors`: an argument mapper,
   a type resolver, a writer and the `RevitFloorCreator` adapter, built by `RevitApiDocumentContext`
   (a writer that defers work to the end of the batch implements `IRevitBatchParticipant`).
3. **Module**: a class implementing `IImportModule` with its category (`new ImportCategory("floors",
   "Floors")`), an `Order` (after the walls, for example 150), `CountInDrawing`, `Plan` (build the
   requests, use `ExistingElementMatcher` to skip floors already in the model) and `Create`.
4. **Settings**, when the module has options: a `FloorImportSettings` class in Core with a
   `ToOptions()` method (like `ColumnImportSettings`), a property on `ImportSettings`, and an
   `IImportSettingsSection` in the add-in (like `ColumnSettingsSection`) that builds its group box,
   disables it when floors are not chosen, checks the choices and stores them.
5. **Register** the module in `ImportModuleCatalog.Create`, its section in
   `ImportModuleCatalog.CreateSettingsSections` and its options in
   `ImportModuleCatalog.CreateModuleOptions`.
6. **Test** it with `ImportCoordinator` and the in-memory creator, and its settings with `ToOptions`.

The settings dialog shows its check box with the count found in the DXF, the choice is remembered
for the session (`ImportSelectionSettings`, by category key), and the summary lists what it
created, what failed and its notes. Module options reach the module through
`ImportPlanContext.Options`.

A drawing of another kind (for example a section for roofs by extrusion) is a separate command on
the ribbon with its own analysis, but it can reuse the coordinator and the module pattern.
