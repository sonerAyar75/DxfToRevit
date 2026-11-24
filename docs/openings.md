# Openings: doors, windows and empty openings {#openings}

[TOC]

## Goal

The add-in removes the need to redraw a project in Revit. The user prepares the plan in AutoCAD,
where drawing is faster, and marks every opening with a **marker block**. The program must never
*guess* an opening: position, width and kind come from the marker, the thickness comes from the
wall it sits on, and anything that does not fit the rules is reported instead of interpreted.

## Decisions

| # | Topic | Decision |
|---|-------|----------|
| 1 | Marking | One rectangular dynamic block per opening, placed in the wall. |
| 2 | Kind | From the block name (`door`, `window`, `opening`); the block layer is a fallback. |
| 3 | Width | Measured from the stretched rectangle (world coordinates, scale and mirroring applied). |
| 4 | Thickness | Taken from the host wall, never from the block depth. |
| 5 | Heights | Attributes, in **centimetres**: `HEIGHT`, `SILL_HEIGHT`; defaults when missing. |
| 6 | Revit type | Attribute `TYPE` is a code, mapped to a Revit type by a table (pick from a list or read from a file). |
| 7 | Swing (doors and windows) | One circle in the block marks the hinge: its wall face is the swing side, its half of the opening the hinge end. Door and window variants (double, sliding, multi-panel) are not separate blocks and are not read from circles: they come from the family mapped to `TYPE`. |
| 8 | Jamb lines | Wall lines inside a marker are ignored. |
| 9 | Walls | Walls run continuously through marked openings; the opening is then cut by the door, window or opening. |
| 10 | Unmarked gaps | Left open, as before, and reported. |

## Marker block (AutoCAD)

| Item | Requirement |
|------|-------------|
| Block name | `door`, `window` or `opening` (case-insensitive); dynamic blocks keep their name through the anonymous `*U` copies |
| Geometry | One closed 4-vertex polyline: the opening rectangle. Its long side lies along the wall. |
| Hinge | Doors and windows: one circle on the face the leaf swings to, at the hinge end (see *Swing* below). Without a circle the orientation is left as Revit places it, with a warning. Circles in `opening` blocks are ignored. Circles hidden by a visibility state are ignored, so a block may move the circle with named states; the state name itself is not read. |
| Placement | Inside the wall, spanning the opening from jamb to jamb. Any rotation, scale or mirroring. |
| Attributes | `TYPE` (code), `HEIGHT` (cm), `SILL_HEIGHT` (cm, windows and empty openings) |

Defaults when an attribute is missing (a warning is reported):

| Kind | `HEIGHT` | `SILL_HEIGHT` |
|------|----------|---------------|
| door | 210 cm | 0 |
| window | 150 cm | 90 cm |
| opening | 210 cm | 0 |

## Processing

```
DXF ──► DxfOpeningReader ──► OpeningMarker[]  (+ OpeningIssue[] for unusable markers)
        DynamicBlockNameResolver: *U26 → door
lines ─► OpeningRegionFilter: drop wall lines inside markers (jamb lines)
      ─► face pairs ─► wall models
      ─► OpenSideCenterLineBridger: gaps covered by a marker are bridged,
                                    other gaps stay open and are reported (UnmarkedGap)
      ─► junctions ─► collinear merge ─► wall footprints
      ─► OpeningWallMatcher ──► OpeningModel[] (+ OpeningIssue[])
```

### Matching a marker to its wall

A marker is attached to a wall footprint when its long side is parallel to the wall (1°) and its
centre lies inside the wall strip. Then:

| Check | Result when it fails |
|-------|----------------------|
| exactly one wall | `NoHostWall` / `MultipleWalls` (error) |
| inside the wall length | `OutsideWall` (error) |
| no overlap with another opening on the same wall | `Overlapping` (error, both) |
| rectangle depth ≈ wall thickness (5 mm) | `DepthMismatch` (warning) |
| attributes present and numeric | `MissingAttribute` / `InvalidAttribute` (warning, default used) |

Openings with an error are not created. Warnings are listed in the import summary.

### Swing (doors and windows)

The block only says what the Revit family cannot know: which face the leaf swings to and at
which end the hinge is. Single, double, sliding or multi-panel comes from the family mapped to
the `TYPE` code, so a block needs **exactly one visible circle**. With the wall direction defined
by its centerline (start → end):

| Circle | Result |
|--------|--------|
| One circle, off the axis and off the middle | `SwingSide` = its wall face, `HingeEnd` = the half of the opening it lies in |
| One circle in the middle of the opening | swing side only, warning |
| One circle on the wall axis | no orientation, warning |
| No circle, or more than one | no orientation, warning |

For a double, sliding or fixed family the circle still sets the facing; the hand flip only
mirrors the family, which is harmless for symmetric ones and meaningful for asymmetric ones.

In Revit the instance is placed, its actual hand and facing orientations are read, and it is
flipped only when they differ from the desired ones, so any door or window family works.

## Classes

| Area | Types |
|------|-------|
| Kinds (data-driven) | `OpeningCategory`, `OpeningCreationMode`, `OpeningHandingMode`, `OpeningDefinition`, `OpeningDefinitionRegistry` |
| DXF | `DynamicBlockNameResolver`, `DxfOpeningReader`, `OpeningMarker`, `OpeningAttributeParser`, `OpeningAttributes` |
| Interpretation | `OpeningRegionFilter`, `OpeningWallMatcher`, `OpeningPlacement`, `OpeningHanding`, `OpeningModel`, `OpeningIssue` |
| Building API | `OpeningCreationRequest`, `OpeningCreationRequestFactory`, `OpeningCreationValidator`, `IOpeningCreator`, `OpeningCreationResult`, `InMemoryOpeningCreator`, `BuildingModelService.CreateOpenings` |
| Type mapping | `OpeningTypeMap`, `OpeningTypeMapFile` |
| Revit | `OpeningCreateArgs`, `OpeningCreateArgsMapper`, `OpeningOrientationPlanner`, `OpeningOrientationConvention`, `OpeningTypeKey`, `RevitOpeningParameterNames`, `RevitOpeningCreator`, `RevitApiDocumentContext.CreateOpening`, `RevitElementResolver.ResolveOpeningSymbol` |
| Add-in | `OpeningTypeMapForm`, `OpeningImportPreferences`, `ColumnImportSettingsForm` (openings group), `ImportDxfCommand` |

A new kind (for example a sliding door) is a new `OpeningDefinition` entry, not a new class:
it declares its block and layer names, Revit category, creation mode, handing mode and defaults.

## Type mapping

`TYPE` codes are mapped per kind to a Revit type (`Family::Type`):

```
# DxfToRevit opening type map
Category;Code;RevitType
Door;1;M_Single-Flush::0915 x 2134mm
Door;2;M_Double-Flush::1830 x 2134mm
Window;1;M_Fixed::0915 x 1220mm
Door;*;M_Single-Flush::0915 x 2134mm
```

- Code `*` is the default for a kind when a code has no row.
- The Revit type is the template: it is used as is when its width and height match the DXF;
  otherwise a type of the same family with that size is reused, and only then is the template
  duplicated and resized (`<Family> - <width>x<height>mm`), as for columns. Width and height are
  read from the `Width`/`Height` type parameters, then from the built-in family width/height.
- Empty openings need no type: they are cut as rectangular wall openings.

The Revit import first asks for the DXF and analyses it, then opens the settings dialog, so the
codes of the drawing are known before mapping. The **Doors, windows and openings** group shows how
many doors, windows and empty openings the DXF contains and how many codes still have no Revit
type; it switches the import on or off and opens **Opening types and heights...**. That dialog
lists every door and window `TYPE` code found in the DXF first (`OpeningCodeSummary`,
`OpeningTypeMapRows`), with its count and sizes, followed by the saved rows that are not in the
DXF (for example `*`). A DXF row without its own type shows the `*` type it falls back to, or
*not mapped* in red; blocks without a `TYPE` attribute can only use the `*` row. Right-clicking
(or double-clicking) a *Revit type* cell offers the loaded door or window types grouped by family,
and types that are not loaded in the project are shown in red. *Load from file* and *Save to file*
read and write the format above. The same dialog sets the default heights (cm) used when a block
has no `HEIGHT` or `SILL_HEIGHT` attribute. Confirming with unmapped codes asks whether to
continue; those openings are then not created. The choices are kept for the Revit session.

## Revit creation

| Kind | Revit |
|------|-------|
| Door, window | Family instance hosted by the wall, at the opening centre on the base level; sill height set (windows); flipped to match the circles |
| Empty opening | Rectangular wall opening from sill to sill + height |

Openings are created after the walls and are hosted by the wall created from the same footprint.
If that wall was not created, the opening fails with a clear message.

### Door and window orientation

After placing a door or window, `OpeningOrientationPlanner` compares the instance's facing and
hand orientation with the marker, using the `OpeningOrientationConvention` of its category:

| Convention | Facing | Hand | Status |
|------------|--------|------|--------|
| `StandardDoor` (`Door`) | points **to** the swing side | points **towards** the hinge | verified in Revit 2025 |
| `StandardWindow` (`Window`) | points **to** the swing side | points **towards** the hinge | facing verified in Revit 2025 |

The facing is flipped first, then the hand is checked again and flipped if needed; the hand is only
checked when the marker gives a hinge end. When a family cannot flip, the element is kept and a note is
shown in the summary. Both conventions are properties of the planner, so a family library with
other conventions only needs a different planner passed to `RevitApiDocumentContext`.

The door defaults were checked in Revit 2025 with `OpeningsDoors.dxf`: the first assumption (both
pointing away) mirrored every door in both directions, so both conventions were inverted. Windows
were first assumed to face away from the swing side (exterior); in Revit 2025 every window opened
to the wrong side, so the window facing now follows the door convention.

## Import summary

The summary dialog shows the number of openings created, failed openings grouped by message,
notes (for example a family that cannot flip), markers that were not imported (errors) and
warnings (missing attributes, depth mismatch, missing hinge, unmarked wall gaps) with their DXF
coordinates.
