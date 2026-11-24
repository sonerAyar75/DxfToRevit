# Development history {#development_history}

How DxfToRevit grew from a wall centerline tool into a DXF → Revit importer for walls, columns,
doors, windows and openings: which requirement started each stage, which classes were written to
meet it, and what their important methods do. The first part is a summary; the second part gives
the details; the third part is a class reference grouped by the need each class serves.

**Goal of the program:** remove the work of redrawing an AutoCAD plan in Revit. Drawing is faster
in AutoCAD, so the user prepares the plan there (wall layers, column layers, opening blocks) and
DxfToRevit builds the Revit model from it.

---

# Part 1 - Summary

## Stages at a glance

| # | Requirement | Result | Main classes |
|---|-------------|--------|--------------|
| 0 | Turn wall lines of a DXF into Revit walls | Centerline pipeline, host-independent building API, Revit adapter, desktop and console tools | `DxfCenterLinePipeline`, `WallFacePairFinder`, `OpenSideCenterLineBridger`, `WallCenterLineJunctionProcessor`, `BuildingModelService`, `RevitApiDocumentContext` |
| 1 | Review the program and fix what is wrong | No file written during a Revit import, structural flag honoured, walls of different thickness kept apart, DXF read once, one wall layer rule | `DxfCenterLinePipeline.Analyze`, `CollinearCenterLineMerger.Merge(lines, thicknesses)`, `WallLayerRule` |
| 2 | Revit-like ribbon icons for light and dark themes, gear icon for settings | Generated vector icon sets | `scripts/IconGenerator`, `DxfToRevitApplication` |
| 3 | Columns must be bound to levels like walls (they were always unconnected) | Top level / unconnected height for columns | `RevitColumnPlacement`, `ElementPlacement` |
| 4 | Circular columns without a round family were squared or skipped | Automatic round family (loaded or from the library), cylinder DirectShape fallback, four modes in the dialog | `ColumnTypeKeys`, `RoundColumnFamilyLocator`, `LibraryFileSearch`, `RevitElementResolver.ResolveColumnSymbol` |
| 5 | Rotated rectangular columns came in as freeform shapes | Real rectangular family, true sizes, rotated to the DXF angle | `PolygonMetrics2D.TryGetOrientedRectangle`, `ColumnCreationRequest.RotationRadians` |
| 6 | Freeform (amorphous) columns: Columns category, then level-bound | Columns-category DirectShape, then a generated column family per section shape | `RevitFreeformColumnFamilyBuilder`, `FreeformColumnFamilyNaming` |
| 7 | Remove leftovers, document everything in English, Doxygen | Dead code removed, XML docs on every type and member, Doxygen site with guide pages, 0 warnings | `docs/*.md`, `Doxyfile`, `scripts/generate-docs.*` |
| 8 | Installer | Inno Setup 7 script, version 2.0.0, MIT licence and third-party notices | `installer/DxfToRevit.iss`, `scripts/build-installer.ps1` |
| 9 | Doors, windows and empty openings must not be misread | Rectangular dynamic block markers, wall matching, attributes, swing from a hinge circle, type map, Revit creation | `DxfOpeningReader`, `OpeningInterpreter`, `OpeningWallMatcher`, `OpeningTypeMap`, `RevitOpeningCreator` |
| 10 | Load the DXF first, then map its opening codes to Revit families | DXF-first import flow; the type map editor lists the codes found in the drawing | `OpeningCodeSummary`, `OpeningTypeMapRows`, `OpeningTypeMapForm` |
| 11 | Doors came in mirrored in both directions | Door orientation conventions corrected (verified in Revit 2025) | `OpeningOrientationPlanner` |
| 12 | The import drew elements one by one and was slow | Compute first, then write everything in one batch transaction (unit of work) | `RevitImportBatch`, `RevitChangeScope`, `RevitBatchFailureHandler` |
| 13 | Windows came in facing a random side; double, sliding and multi-panel variants | Windows read hinge circles like doors; the circles' count and position give the leaf layout; separate window convention | `OpeningLeafLayout`, `OpeningHanding`, `OpeningOrientationConvention` |
| 14 | Circle count duplicated what the `TYPE` family already says | One circle per door or window: swing side and hinge end only | `OpeningInterpreter`, `OpeningHanding` |
| 15 | Importing the same drawing twice gave no message | Revit's overlap warnings are collected and shown first in the summary | `RevitWarningLog`, `RevitBatchResult` |
| 16 | Adding elements to an imported drawing meant importing everything again | Category check boxes; elements already in the model are skipped and host the new doors and windows | `ExistingElementMatcher`, `RevitExistingElementReader`, `ImportSelectionPreferences` |
| 17 | Floors, rooms and roofs are next; every element type was wired in by hand | Every kind of element is an import module run by a coordinator | `IImportModule`, `ImportCoordinator`, `ImportModuleCatalog` |
| 18 | Mixed folder conventions, several types per file, outdated names | Folders and namespaces by feature, one type per file, sorted usings | (structure only) |
| 19 | One Revit class created every element and another resolved every type | A facade over one writer and one type resolver per kind of element; batch participants | `RevitWallWriter`, `RevitColumnWriter`, `RevitOpeningWriter`, `IRevitBatchParticipant` |

## Classes by need (short)

| Need | Classes |
|------|---------|
| Read the DXF | `DxfReader`, `DxfGeometryReader`, `DxfColumnReader`, `DxfOpeningReader`, `DynamicBlockNameResolver` |
| Know what a layer means | `WallLayerRule`, `ColumnLayerRule`, `DxfLineClassifier` |
| Find walls in lines | `WallFacePairFinder`, `WallModelManager`, `SegmentOverlapGeometry` |
| Make walls continuous and joined | `OpenSideCenterLineBridger`, `WallCenterLineJunctionProcessor`, `JunctionExtensionLimit`, `CollinearCenterLineMerger` |
| Wall shapes | `WallFootprintBuilder`, `WallSolid2D` |
| Column shapes | `PolygonMetrics2D`, `ColumnModel`, `ColumnCreationRequestFactory` |
| Openings | `OpeningDefinitionRegistry`, `OpeningRegionFilter`, `OpeningWallMatcher`, `OpeningInterpreter`, `UnmarkedGapReporter` |
| Run it all | `DxfCenterLinePipeline` |
| Check without Revit | `DxfSimulationWriter`, `InMemoryBuildingModel` and the in-memory creators |
| Describe what to build, independent of Revit | `WallCreationRequest`, `ColumnCreationRequest`, `OpeningCreationRequest`, their factories and validators, `BuildingModelService` |
| Decide Revit details without the API (testable) | `LengthUnitConverter`, the `*CreateArgsMapper`s, `LevelMatchPlanner`, `WallTypeMatchPlanner`, `ColumnTypeMatchPlanner`, `OpeningOrientationPlanner`, `OpeningTypeKey` |
| Write to Revit | `RevitApiDocumentContext`, the writers and type resolvers of each feature, `RevitColumnPlacement`, `RevitFreeformColumnFamilyBuilder`, `RevitImportBatch` |
| User interface | `DxfToRevitApplication`, `ImportDxfCommand`, `ImportSettingsForm`, `OpeningTypeMapForm`, the preferences classes |

---

# Part 2 - Details by stage

## Stage 0 - Baseline: walls from DXF lines

**Requirement.** The user draws both faces of every wall as `LINE`s on layers that encode the
thickness (`WALL-20` = 200 mm). Revit needs one centerline and a thickness per wall.

**Design.** Three layers, so the geometry can be developed and tested without Revit:

- `DxfToRevit.Core`: reading, rules, models, geometry, the pipeline and a host-independent
  building API (requests → validators → creator interfaces);
- `DxfToRevit.Revit`: the Revit adapter;
- `DxfToRevit.Addin`, `DxfToRevit.App`, `DxfToRevit.Console`: user entry points.

**Pipeline** (`DxfCenterLinePipeline`):

1. `DxfLineImportService.Import` extracts the lines (`DxfGeometryReader`) and classifies them
   (`DxfLineClassifier` + `WallLayerRule.TryGetThicknessMm`).
2. `WallFacePairFinder.FindPairs` pairs parallel lines one thickness apart on the same layer.
   When one line has two possible partners, all candidates are rejected and reported as
   ambiguities rather than guessed.
3. `WallModelManager.CreateFromFacePair` trims both faces to their common stretch
   (`SegmentOverlapGeometry.TryCreate`) and puts the centerline midway.
4. `OpenSideCenterLineBridger.Bridge` closes centerline gaps where one face continues (niches,
   pockets): a gap is a real opening only when both faces stop.
5. `WallCenterLineJunctionProcessor.Process` extends ends to L and T junctions, limited by
   `JunctionExtensionLimit` so that unrelated walls are not joined.
6. `CollinearCenterLineMerger.Merge` joins collinear pieces separated by at most 5 mm.
7. `WallFootprintBuilder.Build` offsets each centerline by ±T/2 into a closed `WallSolid2D`.

**Building API.** `WallCreationRequestFactory.FromWallSolids` turns footprints into
`WallCreationRequest`s using the session policy (`ImportSessionOptions`: levels, height, wall
type per thickness). `BuildingModelService.CreateWalls` validates (`WallCreationValidator`) and
forwards to an `IWallCreator`: `InMemoryWallCreator` in tests, `RevitWallCreator` in Revit.

**Revit adapter.** `WallCreateArgsMapper.Map` converts millimetres to feet
(`LengthUnitConverter`); `RevitElementResolver.ResolveWallType` finds or duplicates a Basic Wall
type of the right thickness; `RevitApiDocumentContext.CreateBasicWall` calls `Wall.Create`.

## Stage 1 - Review and fixes

**Requirement.** "Review the program; do you have suggestions?" - then "apply them".

| Problem | Fix |
|---------|-----|
| A Revit import wrote `*_centerline.dxf` next to the DXF and failed in read-only folders | `DxfCenterLinePipeline.Analyze(path)` computes without writing; only `Run` writes the simulation |
| The structural column option was ignored | `StructuralType.Column` when `IsStructural` is set |
| Collinear walls of different thickness were merged into one | `CollinearCenterLineMerger.Merge(lines, thicknesses)` merges only equal thicknesses |
| The DXF file was loaded three times | `DxfLineImportService.Import(DxfDocument)` and `DxfSimulationWriter.Write(DxfDocument, ...)` reuse one document |
| Layer names such as `WALLS` or `WALLPAPER` counted as walls | `WallLayerRule.IsWallLayer` accepts only `WALL` and `WALL-*`, and is the single rule used everywhere |

## Stage 2 - Ribbon icons

**Requirement.** Revit-like icons, visible on light and dark themes; a gear for settings.

`scripts/IconGenerator` draws the icons from vectors into PNG sets for both themes;
`DxfToRevitApplication` creates the *DxfToRevit* tab and picks the set that matches the Revit theme.

## Stage 3 - Columns bound to levels

**Requirement.** "Walls work; do the same for columns - they are always unconnected."

The top level was passed down but Revit always got *Top Level = Base Level + offset*.
`RevitColumnPlacement.TryApplyHeight` now sets the base level and offset and either the top level
(Top Offset 0, so the column follows level changes) or the unconnected height. The placement data
is the same `ElementPlacement` that walls use.

## Stage 4 - Circular columns

**Requirement.** Without a round family, circular columns were squared or skipped.

- `ColumnTypeKeys` defines special keys that are not family names: `AutoRoundFamily` (let the
  host choose or load a round family) and `RoundDirectShape` (cylinder solid).
- `RevitElementResolver.ResolveColumnSymbol` with `AutoRoundFamily`: a loaded type with a
  diameter parameter (same diameter first); otherwise `RoundColumnFamilyLocator.Find` searches the
  add-in's `Resources/Families` folder and the Revit library (`LibraryFileSearch` prefers the
  English metric folder) and loads the family; otherwise `RevitApiDocumentContext` creates a
  cylindrical DirectShape and reports it as a note.
- `ColumnImportSettingsForm` offers four modes: circular family, round solids, squares, skip.

## Stage 5 - Rotated rectangular columns

**Requirement.** Rectangles drawn at an angle must be real rectangular columns.

`PolygonMetrics2D.TryGetOrientedRectangle` recognises a rectangle at any rotation (four corners
within 1° of 90°, opposite sides within 2 mm) and returns its true side lengths and an angle
normalised to (−45°, 45°], so vertex order and winding do not matter. `ColumnCreationRequestFactory`
stores the angle in the request; the Revit context places the column at the centroid and rotates
it about its vertical axis. Type names now show the true size (e.g. `300x600mm`).

## Stage 6 - Freeform columns

**Requirement.** Amorphous columns into the Columns category, then: "can't we make them families,
so they bind to levels?"

1. The extruded DirectShape got the Columns category when Revit allows it (Generic Model otherwise).
2. `RevitFreeformColumnFamilyBuilder.GetOrCreate` builds a family from `Metric Column.rft`: when
   the template has a locked extrusion, only its sketch is replaced (`SketchEditScope`), so the
   locks to the reference levels remain; otherwise a new extrusion is created and locked. The
   family is saved, loaded and placed like any other column, so it follows the levels.
3. `FreeformColumnFamilyNaming.FamilyName` derives the name from the shape only (not position,
   start vertex or winding), so identical columns share one family across imports.

## Stage 7 - Cleanup and documentation

**Requirement.** "Remove leftovers in all files; prepare advanced automatic documentation
(Doxygen); detailed English comments on all classes and methods; no Turkish words in files or tests."

Unused commands and helpers were deleted, duplicated logic merged, every public and internal type
and member received English XML documentation (`GenerateDocumentationFile` makes missing docs a
warning), and Doxygen builds the site from the code plus the guide pages in `docs/` with zero
warnings. The only non-English text left is data: the Turkish parameter names in
`RevitColumnParameterNames` and `RevitOpeningParameterNames`, used to match localised families.

## Stage 8 - Installer and release

**Requirement.** An Inno Setup 7 installer; then preparation for GitHub.

`installer/DxfToRevit.iss` installs the add-in for the current user (or all users) and shows the
MIT licence; `scripts/build-installer.ps1` builds with `DeployAddin=false` and compiles the
installer. The version (2.0.0) comes from `Directory.Build.props`. `LICENSE.txt` (MIT) and
`THIRD-PARTY-NOTICES.txt` were added and the repository was reduced to a single commit.

## Stage 9 - Doors, windows and empty openings

**Requirement.** "Openings must not be misinterpreted." The user marks each opening in AutoCAD
with a rectangular **dynamic block** named `door`, `window` or `opening`; the block's current
width is the opening width; depth comes from the wall; heights come from attributes in
centimetres (`TYPE`, `HEIGHT`, `SILL_HEIGHT`); a circle in a door block is the hinge; jamb lines
are ignored; `TYPE` codes are mapped to Revit types either by picking from a list or from a file.
The full design is in @ref openings.

**Kinds as data.** `OpeningDefinition` describes a kind (block and layer names, category,
creation mode, handing mode, default heights, id prefix); `OpeningDefinitionRegistry.CreateDefault`
defines door, window and opening. A future kind (for example a sliding door) is a new definition,
not a new class.

**Reading.** AutoCAD stores a stretched dynamic block as an anonymous block (`*U26`).
`DynamicBlockNameResolver.Resolve` follows the block record's `AcDbBlockRepBTag` link back to the
original name. `DxfOpeningReader.ReadMarkers` then transforms the block's closed four-vertex
polyline to world coordinates (scale, mirror, rotation, position), takes the first circle as the
hinge and collects the attributes into an `OpeningMarker`.

**Pipeline changes.**

- `OpeningRegionFilter.RemoveLinesInside` drops lines that lie completely inside a marker
  (jambs, leaf lines) before face pairing.
- `OpenSideCenterLineBridger.Bridge(walls, pairs, openingOutlines, out openGaps)` also bridges a
  gap covered by a marker and extends wall ends that stop inside a marker to its far edge. Gaps
  left open are returned and `UnmarkedGapReporter.Report` warns about them (unless a column or a
  crossing wall fills the gap), so a forgotten door is noticed.
- `OpeningInterpreter.Interpret` runs on the final footprints: `OpeningWallMatcher.TryMatch`
  finds exactly one wall that is parallel to the marker and contains it (errors: no wall, several
  walls, outside the wall; warning: depth differs from the wall); `OpeningAttributeParser.Parse`
  converts the attributes (cm → mm, decimal comma accepted, defaults with warnings);
  the hinge circle gives `HingeEnd` (nearest end) and `SwingSide` (wall face); overlapping
  openings on one wall are rejected; ids are `D001`, `W001`, `O001`.
- Every problem is an `OpeningIssue` (error = not created, warning = created) shown in the summary.

**Building API.** `OpeningCreationRequestFactory.FromOpeningModels` resolves the Revit type with
`OpeningTypeMap.Resolve` (exact code, then `*`) and links each opening to its host wall request
id (`WallCreationRequestFactory.WallIdForIndex`). `BuildingModelService.CreateOpenings` runs
after the walls and receives the map *wall request id → created element id*.
`OpeningTypeMapFile` reads and writes `Category;Code;RevitType` files.

**Revit.** `OpeningCreateArgsMapper.Map` converts to feet; `RevitElementResolver.ResolveOpeningSymbol`
uses the mapped type as template, reuses a same-family type of the right size or duplicates and
resizes it (`<Family> - <w>x<h>mm`); `RevitApiDocumentContext.CreateOpening` places a hosted
family instance (sill height for windows) or cuts a rectangular wall opening (`NewOpening`).

## Stage 10 - DXF first, then mapping

**Requirement.** "The DXF must be loaded first so its openings are listed; then we can match them
with Revit families."

`ImportDxfCommand` now picks and analyses the DXF before the settings dialog.
`OpeningCodeSummary.Build` groups the openings by category and `TYPE` code with counts and sizes;
`OpeningTypeMapRows.Build` lists those codes first (with their mapped type) followed by the saved
rows that are not in the drawing; `OpeningTypeMapRows.Unmapped` finds codes with no type (neither
their own row nor `*`). `OpeningTypeMapForm` shows the rows with a right-click type picker grouped
by family, marks unloaded types in red, shows the `*` fallback as a hint and asks before
continuing with unmapped codes. When the default heights are changed, the DXF is analysed again.

## Stage 11 - Door orientation

**Requirement.** "Door opening directions are reversed in both X and Y."

`OpeningOrientationPlanner` compares a placed door's facing and hand orientation with the swing
derived from the marker. Both conventions were assumed the wrong way round; verified in Revit 2025,
the facing points to the swing side and the hand points towards the hinge
(`FacingPointsToSwingSide = true`, `HandPointsToHinge = true`). Both stay configurable for family
libraries with other conventions.

## Stage 12 - Batch writing (unit of work)

**Requirement.** "Elements appear on screen one by one. Wouldn't it be faster to buffer all
computed objects and write them when all calculations are done? Apply the professional pattern."

All objects were already computed before writing; the slowness came from committing one Revit
transaction per element, which regenerates the model and redraws the view each time. The fix is a
*unit of work*:

- `RevitApiDocumentContext.BeginBatch` opens one `RevitImportBatch` transaction.
- `RevitChangeScope.Start` writes each element (and each new type) in a `SubTransaction` when a
  batch is open, or in its own `Transaction` otherwise; a failing element still rolls back alone.
- `RevitImportBatch.Regenerate` runs once after the walls, so columns join and openings find hosts.
- Door flips are collected and applied together with two regenerations in total.
- `RevitImportBatch.Commit` commits once. `RevitBatchFailureHandler` suppresses warnings and
  deletes elements that cause errors; `ImportDxfCommand` turns those into failures with Revit's
  reason. The import is one undo step.

## Stage 13 - Window orientation and leaf layouts

**Requirement.** Doors work in tests and with a real DXF; next, windows. Windows had no
orientation at all, so Revit placed them facing a random side. Doors and windows also come in many
variants (double, sliding, multiple, sliding window): should they be separate blocks?

**Decision.** No new blocks. The variant is already chosen by `TYPE` → Revit family; the DXF only
has to say what the family cannot know: the side and, for a single leaf, the hinge end. The user
adds a hinge circle to the window block as in the door block.

- `DxfOpeningReader` keeps every circle of the block (`OpeningMarker.HingePoints`).
- `OpeningDefinitionRegistry`: windows are `Hinged` like doors.
- `OpeningInterpreter.DeriveHanding` classifies the circles by wall face and by position (end zone
  = one third of the width, `HingeEndZoneFraction`): one end → `SingleHinged` (side + hinge end),
  both ends → `DoubleHinged` (side only), middle only → `SideOnly`, no circle → no orientation
  and a warning (the earlier fallback to the insertion point was dropped). Circles on both faces:
  the majority wins, with a warning.
- `OpeningHanding` became a record with `Layout`, an optional `HingeEnd` and `SwingSide`, built
  with `Single`, `Double` and `SideOnly`.
- `OpeningOrientationPlanner` holds one `OpeningOrientationConvention` per category: doors keep the
  verified convention; windows default to facing away from the swing side (exterior, sashes open
  inwards), to be verified in Revit. The hand is only flipped for a single leaf.
- The user switches the circles with named visibility states (`single`, `double`, `sliding`) in
  one block. A test DXF showed that AutoCAD marks the circles hidden by the current state as
  invisible (group code 60) in the block definition, also in the base block for the default state,
  so `DxfOpeningReader` only keeps visible circles. The state name is not read: netDxf does not
  parse the dynamic block parameters, and the visible circles already carry the same information.
- Revit test with the corrected block: every window opened to the wrong side, so
  `StandardWindow` was changed to facing towards the swing side, the same as doors.

## Stage 14 - One circle per door or window

**Requirement.** Blocks carry a `TYPE` code so that the drawing groups its doors and windows into
types, and every type is mapped to a Revit family (door type 1 → family A, door type 2 →
family B). The family already knows whether it is single, double or sliding, so reading the leaf
layout from the number and position of circles duplicated that information.

**Decision.** One circle per door or window, only for what the family cannot know.

- `OpeningInterpreter.DeriveHanding` expects exactly one visible circle: its wall face is the swing
  side, the half of the opening it lies in is the hinge end. A circle in the middle gives the side
  only; no circle, more than one, or a circle on the wall axis gives no orientation. Each case is a
  `MissingHinge` warning.
- `OpeningLeafLayout` and `OpeningInterpreter.HingeEndZoneFraction` were removed; `OpeningHanding`
  became `OpeningHanding(HingeEnd?, SwingSide)`.
- For a symmetric double or sliding family the hand flip only mirrors it, which does no harm; for an
  asymmetric family it chooses the side, as wanted.

## Stage 15 - Warning when a drawing is imported over itself

**Requirement.** Importing the same drawing again at the same place created a second set of walls,
columns, doors and windows without any message.

**Cause.** Revit does detect it ("Highlighted walls overlap", "There are identical instances in the
same place"), but `RevitBatchFailureHandler` deleted every warning so that Revit would not show a
dialog per element.

**Decision.** Use Revit's own detection instead of a geometric check of our own.

- `RevitBatchFailureHandler` records each warning in a `RevitWarningLog` (API-free, tested) before
  deleting it. Overlap warnings (`WallsOverlap`, `DuplicateInstances`, `FloorsOverlap`,
  `OverlappingElementsTest`) are of kind `RevitWarningKind.Overlap`.
- `RevitBatchResult.Warnings` carries the groups (text, count, elements); `HasOverlaps` tells whether
  there is an overlap.
- The summary title says *elements overlap existing elements*; the overlaps come first with a note
  that one Undo removes the whole import, and other Revit warnings are listed at the end.
- A check before writing (asking whether to continue) is possible later; since the import is one
  undo step, the warning after the import was enough for now.

## Stage 16 - Choosing what to import, skipping what exists

**Requirement.** When doors, windows or other elements are added to a drawing that was already
imported, the user should not have to import the whole drawing again (which duplicated every
element) and should be able to choose what is imported.

**Decision.** Category check boxes instead of a list of single elements, and a geometric check
against the model instead of marking imported elements, so that elements drawn or changed by hand
in Revit are recognised too.

- The settings dialog has an **Import** group: *Walls*, *Columns*, *Doors*, *Windows*, *Empty
  openings* with their counts in the DXF (`ImportCounts`), and *Skip elements that already exist
  in the model* (`ImportSelectionPreferences`, remembered for the session).
- `RevitExistingElementReader` reads the walls, columns, doors, windows and wall openings of the
  model into an API-free `ExistingElements` snapshot; `ExistingElementMatcher` (tested) finds the
  element a request would duplicate: a wall of the same thickness on the same level covering the
  centreline, or a column, door, window or opening of the same kind at the same place (10 mm).
- Doors and windows whose wall is not created now are hosted by the matching existing wall
  (`MapHostWallIds` merges created and existing walls), so they can be imported alone.
- The summary lists the elements that were already in the model (`SkippedCounts`).
- Not done on purpose: elements deleted from the drawing are not deleted from the model, and a wall
  changed in the drawing is created again (Revit reports the overlap).

## Stage 17 - Import modules

**Requirement.** Next come floors, rooms, ceilings and roofs. Walls, columns and openings were
wired into the import command, the settings dialog and the summary one by one, so every new kind
of element would have touched all of them. The structure should be flexible and easy to extend.

**Decision.** Every kind of element is an import module; the flow works with modules only. No
behaviour changes: the existing tests pass unchanged, and new tests run the whole flow with the
in-memory creators.

- `IImportModule` (Core): categories (check boxes), `Order`, `RegenerateAfter`, `CountInDrawing`,
  `Plan` (requests, elements already in the model) and `Create`. `WallImportModule`,
  `ColumnImportModule` and `OpeningImportModule` hold what the command did before.
- `ImportCoordinator` orders the modules, plans, creates and regenerates; `ImportBag` carries the
  module options (`ColumnImportOptions`, `OpeningImportOptions`) and the data modules share
  (`HostWallMap`: walls created now or already in the model, for the doors and windows).
- `ImportModuleResult` is the same for every module: entries, notes, extra summary sections (the
  opening marker issues) and `WithCommitOutcome` for elements Revit removed at commit.
- Add-in: `ImportModuleCatalog` registers the modules; the settings dialog builds its check boxes
  from the module categories; `ImportSelectionPreferences` keeps the choices by category key, so a
  new module is imported by default; the summary is written from the module results.
- How to add a module (for example floors) is described in `docs/import_modules.md`. A drawing of
  another kind (a section for extrusion roofs) would be a separate ribbon command reusing the same
  pattern.

## Stage 18 - Folders by feature, one type per file

**Requirement.** The structure should be modular, easy to extend and easy to read. Walls and
columns were spread over technical folders (`Dxf`, `Models`, `Rules`, `Services`, `Geometry`,
`Building`) while openings had their own folder; 30 files held several types; some names no longer
matched what the classes do.

**Decision.** Organise the core by feature, as the import modules are. No behaviour changes: the
same tests pass (309 core, 88 Revit adapter tests), the build has no warnings.

- Core folders and namespaces: `Walls`, `Columns`, `Openings` (each with its rule, reading,
  interpretation, requests, validation, creator interface and import module), `Import` (the
  modular import, session options, placement, elements already in the model,
  `BuildingModelService`), `Dxf` (loading, line extraction, the analysis pipeline, the simulation
  output), `Geometry` (shared 2D algorithms), `InMemory`, `Composition`. The old `Building`,
  `Models`, `Rules`, `Services` and `IO` folders are gone. Files were moved with `git mv`, so their
  history is kept.
- One type per file: 30 files were split with a small Roslyn tool (doc comments travel with their
  types); `RevitImportBatch.cs` was split by hand because of its `#if` block.
- Test folders mirror the core folders; `LengthUnitConverterTests` moved to the Revit adapter
  tests, next to the class it tests.
- Unused `using` directives were removed (IDE0005) and every using block is sorted. Revit
  sources are compiled twice (without and with `REVIT_API_AVAILABLE`), so usings needed only by
  the Revit API code were kept.
- Renamed: `ColumnImportSettingsForm` → `ImportSettingsForm`, `ColumnSettingsCommand` →
  `SettingsCommand` (the ribbon button keeps its internal name).

## Stage 19 - One Revit writer per kind of element

**Requirement.** `RevitApiDocumentContext` (about 730 lines) created walls, columns in three ways,
doors, windows and openings, flipped doors and windows, and ran the batch; `RevitElementResolver`
(about 650 lines) resolved levels, wall types, column types and door/window types. Floors would
have made both larger again.

**Decision.** Split by kind of element, keep the public surface. No behaviour changes: the code was
moved, not rewritten, and the add-in and the tests use the same `IRevitDocumentContext`.

- `RevitApiDocumentContext` is a **facade**: it builds one writer per kind of element and
  delegates (`RevitWallWriter`, `RevitColumnWriter` with `RevitColumnSolidWriter` for columns
  without a family, `RevitOpeningWriter`).
- The resolver is split into `RevitLevelResolver` (shared; it also checks that the top level is
  above the base level, which three writers did by hand), `RevitWallTypeResolver`,
  `RevitColumnTypeResolver` and `RevitOpeningTypeResolver`; `RevitParameters` holds the parameter
  helpers they share.
- The batch no longer knows about doors: writers that defer work implement
  `IRevitBatchParticipant` (started, before commit, ended). The opening writer collects the door and
  window flips and applies them before the commit; a floor writer could take part the same way.
- The Revit project is organised by feature like the core: `Walls`, `Columns`, `Openings`,
  `Import` (batch, failure handler, warnings, existing elements) and `Common`.
- The unused `GetOpeningTypeKeys` of the context was removed (the settings dialog reads the types
  itself).

---

# Part 3 - Class reference by need

Only the members that matter for understanding the flow are listed; every member is documented in
the generated API reference.

## Reading the DXF

| Class | Why it exists | Important members |
|-------|---------------|-------------------|
| `DxfReader` | One place that loads a DXF file with path checks and consistent errors | `Read(path)`, `ReadAsync(path)` |
| `DxfGeometryReader` | Plan-view lines with layer and handle, Z dropped | converts `LINE` entities to `DxfLineModel` |
| `DxfColumnReader` | Column sections from `COLUMN*` layers | `ReadColumns(document)`: closed polylines and circles, tiny sections skipped, ids `C001…` |
| `DynamicBlockNameResolver` | Stretched dynamic blocks have anonymous names | `Resolve(block)`: follows `AcDbBlockRepBTag` to the real name |
| `DxfOpeningReader` | Opening markers from the model space | `ReadMarkers(document)`: kind lookup, +Z check, rectangle transform, hinge circle, attributes |

## Layer rules and classification

| Class | Why it exists | Important members |
|-------|---------------|-------------------|
| `WallLayerRule` | The single definition of a wall layer | `IsWallLayer(name)`, `TryGetThicknessMm(name, out t)`: `WALL-20` → 200 |
| `ColumnLayerRule` | The single definition of a column layer | `IsColumnLayer(name)`: `COLUMN`, `COLUMNS`, `COLUMN-*` |
| `DxfLineClassifier`, `DxfLineClassificationService` | Attach the rule result to each line | `Classify(line)`, `Classify(lines)` |
| `DxfLineImportService` | Lines + classification in one call, reusing a loaded document | `Import(path)`, `Import(document)` |

## Wall geometry

| Class | Why it exists | Important members |
|-------|---------------|-------------------|
| `LineSegment2D` | Immutable segment in millimetres | `IsParallelTo`, `GetDistanceToParallelLine`, `OverlapsAlongLength` |
| `LocalCoordinateSystem2D` | Measure along and across a wall | `ToLocal`, `ToWorld` |
| `SegmentOverlapCalculator`, `SegmentOverlapGeometry` | The common stretch of two faces | `TryGetOverlapInterval`, `TryCreate` |
| `WallFacePairFinder` | Turn loose lines into wall face pairs safely | `FindPairs(lines)`: same layer, parallel, overlapping, one thickness apart; ambiguities rejected |
| `WallModelManager` | Wall models from pairs or ready centerlines | `CreateFromFacePair`, `CreateFromCenterLine` |
| `OpenSideCenterLineBridger` | Keep a wall continuous across one-sided gaps and marked openings | `Bridge(walls, pairs)`, `Bridge(walls, pairs, openingOutlines, out openGaps)` |
| `LineIntersectionCalculator`, `LineSegmentEndpointAdjuster` | Corner geometry | `TryFindIntersection`, `TryJoinAtIntersection` (L), `TryExtendBranchToMainLine` (T) |
| `JunctionExtensionLimit` | Do not join unrelated walls | `ForPair`, `ForCrossingWall`: `(T_other / 2) / tan(α / 2) + 5 mm` |
| `WallCenterLineJunctionProcessor` | L and T junctions | `Process(...)`: extends ends to the intersections, within the limits |
| `CollinearCenterLineMerger` | One wall instead of fragments, but never across thicknesses or real gaps | `Merge(lines)`, `Merge(lines, thicknesses)` |
| `WallFootprintBuilder` | Closed wall rectangles for Revit and the simulation | `Build(centerLine, thickness)`, `BuildMany` |
| `PolygonMetrics2D` | Column and marker geometry | `TryGetAreaCentroid`, `TryGetOrientedRectangle`, `ContainsPoint`, `DistanceToSegment` |

## Openings (Core)

| Class | Why it exists | Important members |
|-------|---------------|-------------------|
| `OpeningDefinition`, `OpeningDefinitionRegistry` | Kinds as data, extensible without new classes | `CreateDefault(doorH, windowH, sill, openingH)`, `Find(blockName, layer)` |
| `OpeningMarker` | The raw marker before interpretation | `Outline`, `Center`, `HingePoints`, `Attributes` |
| `OpeningRegionFilter` | Jamb lines must not create walls or gaps | `RemoveLinesInside(lines, markers, out removed)` |
| `OpeningWallMatcher` | Each marker on exactly one wall | `TryMatch(marker, walls, out placement, issues)` |
| `OpeningAttributeParser` | Centimetre attributes with safe defaults | `Parse(marker, issues)` |
| `OpeningInterpreter` | Markers → openings on the final walls | `Interpret(markers, walls)`: match, parse, handing, overlaps, ids |
| `UnmarkedGapReporter` | Notice forgotten doors and windows | `Report(openGaps, walls, columns)` |
| `OpeningIssue` | Errors and warnings with a DXF location | `Error(...)`, `Warning(...)` |
| `OpeningCodeSummary` | Know the codes of a drawing before mapping | `Build(openings)` |

## Pipeline and simulation

| Class | Why it exists | Important members |
|-------|---------------|-------------------|
| `DxfCenterLinePipeline` | The whole DXF → model computation in one place | `Analyze(path)` (no file written, used by Revit), `Run(path, output)` (also writes the simulation) |
| `DxfSimulationWriter` | Check results in any DXF viewer, without Revit | `Write(document, …)`: `WALL-CENTERLINE`, `WALL-SOLID`, `COLUMN-SIM`, `OPENING-SIM` |

## Host-independent building API

| Class | Why it exists | Important members |
|-------|---------------|-------------------|
| `ImportSessionOptions` | The import policy in one object | levels, default height, wall type per thickness, default column type |
| `ElementPlacement` | Base level, offset, top level or height, shared by all elements | |
| `WallCreationRequestFactory` | Footprints → wall requests | `FromWallSolids`, `FromCenterLines`, `WallIdForIndex` |
| `ColumnCreationRequestFactory` | Column sections → requests, classified | `FromColumnModels`: circular / rectangular (also rotated) / freeform |
| `OpeningCreationRequestFactory` | Openings → requests with Revit type and host wall | `FromOpeningModels` |
| `*CreationValidator` | Business rules before anything reaches a host | `Validate(request)` |
| `OpeningTypeMap`, `OpeningTypeMapFile`, `OpeningTypeMapRows` | `TYPE` code → Revit type, from the UI or a file | `Resolve(category, code)`, `Read`, `Write`, `Build`, `Unmapped` |
| `BuildingModelService` | Validate and forward to the host creators | `CreateWalls`, `CreateColumns`, `CreateOpenings(requests, hostWallIds)` |
| `IWallCreator`, `IColumnCreator`, `IOpeningCreator` | Hosts are interchangeable | implemented by in-memory and Revit creators |
| `InMemoryBuildingModel` and in-memory creators | Test the whole flow without Revit | `Walls`, `Columns`, `Openings` |
| `ServiceCollectionExtensions` | Dependency injection registration | `AddDxfToRevitCore`, `AddDxfToRevitBuildingInMemory` |

## Revit decisions without the API (unit-tested)

| Class | Why it exists | Important members |
|-------|---------------|-------------------|
| `LengthUnitConverter` | Millimetres ↔ Revit feet | `MmToFeet`, `FeetToMm` |
| `WallCreateArgsMapper`, `ColumnCreateArgsMapper`, `PlacementCreateArgsMapper`, `OpeningCreateArgsMapper`, `RevitElementCreatePlanner` | The exact values sent to Revit, testable | `Map`, `PlanWall`, `PlanColumn` |
| `LevelMatchPlanner` | Level by name, else the lowest | `Plan(candidates, name, out error)` |
| `WallTypeMatchPlanner`, `ColumnTypeMatchPlanner` | Type name → family with matching size → duplicate | `Plan(...)`, `BuildUniqueName(...)` |
| `LibraryFileSearch`, `RoundColumnFamilyLocator` | Find family files, English metric first | `FindFirst`, `Find` |
| `FreeformColumnFamilyNaming` | One family per shape | `FamilyName(outline)` |
| `OpeningOrientationPlanner`, `OpeningOrientationConvention` | Door and window flips for any family, one convention per category | `NeedsFacingFlip`, `NeedsHandFlip`, `ConventionFor` |
| `OpeningTypeKey` | Parse `Family::Type` keys | `TryParse`, `Format`, `Matches` |

## Writing to Revit

| Class | Why it exists | Important members |
|-------|---------------|-------------------|
| `IRevitDocumentContext`, `NullRevitDocumentContext` | Creators work with or without a document | `CreateBasicWall`, `CreateColumn`, `CreateOpening` |
| `RevitApiDocumentContext` | Facade over the writers; runs the import batch and notifies its participants | the create methods, `BeginBatch(name)` |
| `RevitWallWriter`, `RevitColumnWriter`, `RevitColumnSolidWriter`, `RevitOpeningWriter` | One writer per kind of element; the opening writer defers door and window flips to the end of the batch (`IRevitBatchParticipant`) | `Create`, `BeforeCommit` |
| `RevitLevelResolver`, `RevitWallTypeResolver`, `RevitColumnTypeResolver`, `RevitOpeningTypeResolver` | Levels and types, created when missing, cached | `TryResolve`, `Resolve` |
| `RevitColumnPlacement` | Column level constraints | `TryApplyHeight` |
| `RevitFreeformColumnFamilyBuilder` | Level-bound families for freeform sections | `GetOrCreate(profile, out error)` |
| `RevitWallCreator`, `RevitColumnCreator`, `RevitOpeningCreator` | Building API → document context | `CreateWall`, `CreateColumn`, `CreateOpening` |
| `RevitImportBatch`, `RevitChangeScope`, `RevitBatchFailureHandler` | Fast, single-undo import that survives failing elements | `Regenerate`, `Commit`, `Start`, `PreprocessFailures` |
| `RevitWarningLog`, `RevitWarningGroup` | Keeps the warnings Revit raises at commit, so overlaps (a drawing imported twice) reach the summary | `Add`, `Groups`, `RevitBatchResult.HasOverlaps` |
| `IImportModule`, `ImportCoordinator`, `ImportModuleCatalog` | One kind of element per module; the flow, the settings check boxes and the summary work with any module | `Plan`, `Create`, `CountInDrawing`, `WithCommitOutcome` |

## User interface (add-in)

| Class | Why it exists | Important members |
|-------|---------------|-------------------|
| `DxfToRevitApplication` | Ribbon tab with themed icons | `OnStartup` |
| `ImportDxfCommand` | The import: DXF → analysis → settings → requests → batch → summary | `Execute` |
| `SettingsCommand`, `AboutCommand` | Settings without importing; about box | `Execute` |
| `ImportSettingsForm` | Categories to import, levels, column modes, DXF summary | `IncludedCategoryKeys`, `SkipExisting`, `SelectedBaseLevelName`, … |
| `OpeningTypeMapForm` | Map DXF codes to Revit types, default heights | right-click type menu, load/save file |
| `ColumnImportPreferences`, `OpeningImportPreferences` | Remember choices for the Revit session | `CreateRegistry()` |
