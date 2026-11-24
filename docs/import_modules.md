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
| `ImportPlanContext` | Core | What a module needs to plan: the analysed drawing, the session options, the elements already in the model, the chosen categories, the module options |
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

1. **Read and interpret** the DXF entities in Core (a reader and a model, like `DxfColumnReader`
   and `ColumnModel`). If the module needs entities the pipeline does not read yet, add them to
   `DxfCenterLinePipeline.Result`.
2. **Request and creator**: a creation request, a validator, an `IFloorCreator` interface with an
   in-memory implementation, and the Revit implementation in `DxfToRevit.Revit`.
3. **Module**: a class implementing `IImportModule` with its category (`new ImportCategory("floors",
   "Floors")`), an `Order` (after the walls, for example 150), `CountInDrawing`, `Plan` (build the
   requests, use `ExistingElementMatcher` to skip floors already in the model) and `Create`.
4. **Register** it in `ImportModuleCatalog.Create`.
5. **Test** it with `ImportCoordinator` and the in-memory creator.

The settings dialog shows its check box with the count found in the DXF, the choice is remembered
for the session (`ImportSelectionPreferences`, by category key), and the summary lists what it
created, what failed and its notes. Module-specific settings (like the column options) are passed
through `ImportPlanContext.Options`.

A drawing of another kind (for example a section for roofs by extrusion) is a separate command on
the ribbon with its own analysis, but it can reuse the coordinator and the module pattern.
