# User guide {#user_guide}

[TOC]

This guide is for the people who prepare drawings in AutoCAD and import them into Revit. It
explains how to draw walls, columns, doors, windows and openings so that DxfToRevit recognises
them, how to run the import and how to read the summary. The other pages of this documentation
describe how the program works inside.

## The idea

You draw the plan in AutoCAD, where drawing is fast, and DxfToRevit builds the Revit model from
it: walls, columns, doors, windows and wall openings, on the levels you choose. The program never
guesses: an element is created only when the drawing follows the rules below, and everything
that does not fit is listed in the summary with its DXF coordinates.

## 1. Preparing the drawing in AutoCAD

### General

| Rule | Detail |
|------|--------|
| Units | Millimetres (1 drawing unit = 1 mm) |
| Space | Model space |
| File | Save as **AutoCAD 2018 DXF** (recommended: it is the newest format, also used by AutoCAD 2019 and later). AutoCAD 2000 to 2018 DXF formats are read. DWG files cannot be read directly: save them as DXF (SAVEAS). |
| One storey per file | All elements of the file are placed on the same level |

### Walls

Draw both faces of every wall as `LINE` entities on a layer that carries the wall thickness in
centimetres:

| Layer | Thickness |
|-------|-----------|
| `WALL-10` | 100 mm |
| `WALL-15` | 150 mm |
| `WALL-20` | 200 mm |
| `WALL-25` | 250 mm |

- The two faces of a wall must be parallel and exactly the thickness apart (±1 mm), on the same layer.
- Use lines, not polylines. Explode polylines before saving.
- Corners (L) and T junctions are found automatically; you do not need to draw the centreline.
- A wall may have a niche or a pocket on one side: as long as the other face continues, the wall
  continues.
- If **both** faces stop (a gap in the wall), the wall is interrupted. Mark every door, window
  and opening with its block (see below); unmarked gaps stay open and are reported as warnings.
- Jamb lines (the short lines that close the wall at a door or window) may stay in the drawing:
  inside a door, window or opening block they are ignored.
- When the same face line could pair with two different lines, the program does not choose; the
  wall is skipped and reported as an *ambiguous face pair*.

### Columns

Draw each column as a **closed** polyline or a circle on a layer `COLUMN`, `COLUMNS` or `COLUMN-*`.

| Section | Revit result |
|---------|--------------|
| Rectangle (also rotated) | Rectangular column of the right width and depth, rotated as drawn |
| Circle | Circular column (see the *Circular columns* options) |
| Any other closed shape | Freeform column, built as a column family (or a solid) |

Very small sections are ignored (circles under 10 mm radius, sections under 100 mm²); columns
smaller than 50 mm per side or 25 mm radius are rejected.

### Doors, windows and openings

Every door, window and empty opening is marked with a block placed **in the wall**:

| Item | Requirement |
|------|-------------|
| Block name | `door`, `window` or `opening` (upper or lower case). Dynamic blocks are fine. |
| Rectangle | One closed polyline with 4 corners: the opening. Its long side lies along the wall, from jamb to jamb; its depth equals the wall thickness. |
| Hinge circle | Doors and windows: **one** circle (see below). Not needed for `opening`. |
| Attributes | `TYPE`, `HEIGHT`, `SILL_HEIGHT` (see below) |
| Placement | Any rotation, scale or mirroring. Stretch the dynamic block to the real width. |

The width of the opening is read from the rectangle as it is drawn (after stretching and scaling),
and the thickness is taken from the wall, so one block serves every width.

#### Attributes

| Attribute | Meaning | Unit | Default when missing |
|-----------|---------|------|----------------------|
| `TYPE` | Type code of the door or window, for example `1`, `2`, `D90`. All blocks with the same code become the same Revit type. | text | none |
| `HEIGHT` | Opening height | cm | door 210, window 150, opening 210 |
| `SILL_HEIGHT` | Height of the sill above the level (windows and openings) | cm | window 90, opening 0 |

The default heights can be changed in the import settings. A missing or invalid value is
replaced by the default and listed as a warning.

**Why `TYPE`?** The code groups the doors and windows of the drawing into types. In Revit each
type is assigned a family type once (*door type 1 → single flush door, door type 2 → double
door, window type 3 → sliding window*). Single, double, sliding or multi-panel is therefore
decided by the Revit family, not by the block: one `door` block and one `window` block are
enough for every variant.

#### Hinge circle (doors and windows)

Put **one** circle in the block:

- on the **face of the wall the leaf opens to**,
- at the **end where the hinge is**.

The circle decides two things: which side the door or window opens to, and which end the hinge
is on. For a double or sliding family, place it at either end on the side it opens to (or, for a
sliding window, on the side of the sliding sash).

| Circle | Result |
|--------|--------|
| One circle at one end, on one face | Opening side and hinge end are applied |
| One circle exactly in the middle | Only the opening side is applied (warning) |
| One circle on the wall axis | Orientation left as Revit places it (warning) |
| No circle, or more than one | Orientation left as Revit places it (warning) |

If you switch the circle position with **visibility states** of a dynamic block (for example
`left-in`, `right-out`), only the visible circle counts; the state names are free.

#### Rules checked for each block

| Problem | Result |
|---------|--------|
| The block is not in exactly one wall | Not imported (error) |
| The block runs past the end of the wall | Not imported (error) |
| Two blocks overlap in the same wall | Neither is imported (error) |
| The rectangle depth differs from the wall thickness by more than 5 mm | Imported, warning |

## 2. Importing into Revit

### Before you start

- Open the Revit project.
- The levels you want to use must exist.
- Load the door and window families you want to use. Wall and column types are created
  automatically when a matching size does not exist.

### Step by step

1. **DxfToRevit** tab → **Import DXF** → choose the DXF file. The drawing is analysed; nothing
   is written yet.
2. In the settings dialog choose:
   - **Import**: which categories to import (*Walls*, *Columns*, *Doors*, *Windows*, *Empty
     openings*, each with the number found in the DXF) and whether to **skip elements that already
     exist in the model** (see *Importing an updated drawing* below);
   - **Base Level** and **Top Constraint** (a level, or *Unconnected* with an **Unconnected
     Height** in mm);
   - **Circular columns**: a round column family (*Automatic* finds or loads one), round solids,
     square columns, or do not import;
   - **Other columns**: import freeform columns, and whether to build them as column families
     bound to the levels;
   - **Doors, windows and openings**: shows how many the DXF contains and how many type codes
     still have no Revit type.
3. Click **Opening types and heights...** to assign a Revit type to every code (see below) and to
   set the default heights.
4. Click **Continue**. All elements are created in one step; the summary appears at the end.

The choices are remembered until Revit is closed. **Column Settings** on the ribbon opens the
same settings without importing.

### Importing an updated drawing

When doors, windows or other elements are added to a drawing that was already imported, import
the whole DXF again with **Skip elements that already exist in the model** checked: only the new
elements are created, and the summary lists how many were already in the model.

| Element | Counts as already in the model when… |
|---------|--------------------------------------|
| Wall | a wall of the same thickness on the same level covers its centreline (within 10 mm) |
| Column | a column on the same level stands at the same place (within 10 mm) |
| Door, window, empty opening | an element of the same kind on the same level stands at the same place (within 10 mm) |

- New doors and windows are placed in the walls that are already in the model, so they can be
  imported without importing the walls again. Uncheck *Walls* to import only doors and windows.
- A wall that was lengthened, moved or changed in thickness in the drawing does not match: it is
  created again and Revit reports it as overlapping the old one. Delete the old wall, or undo and
  correct the drawing.
- Elements deleted from the drawing are not deleted from the model.
- Without the option, every chosen element is created again; Revit then reports the overlaps.

### Opening types and heights

The dialog lists every door and window `TYPE` code found in the DXF, with how many blocks use
it and their sizes. For each row:

- **Right-click** (or double-click) the *Revit type* cell and choose a type from the loaded
  families. Types that are not loaded in the project are shown in red.
- Rows cannot be added; deleting a row only clears its type.
- *not mapped* (red) means the code has no type: those doors or windows are not created. When
  you confirm with unmapped codes, the dialog asks whether to continue.
- Blocks without a `TYPE` attribute appear as one *(no TYPE)* row per category. Its type is also
  the default for codes without their own type, shown as *(default: …)* in grey.
- When the size in the DXF differs from the chosen type, a type of the same family with that
  size is used, or the chosen type is duplicated and resized
  (`<Family> - <width>x<height>mm`).

### Saving and loading the type map

**Save to file...** writes the current assignments to a text file, **Load from file...** reads
them back, so the same assignments can be reused in other projects. The easiest way to make a
file is to choose the types once in the dialog and save them.

The file format, one assignment per line:

```
# DxfToRevit opening type map
Category;Code;RevitType
Door;1;M_Single-Flush::0915 x 2134mm
Door;2;M_Double-Flush::1830 x 2134mm
Window;1;M_Fixed::0915 x 1220mm
Window;3;M_Sliding::1300 x 1500mm
Door;*;M_Single-Flush::0915 x 2134mm
```

| Field | Content |
|-------|---------|
| `Category` | `Door` or `Window` (upper or lower case) |
| `Code` | The `TYPE` value of the block; `*` = blocks without `TYPE` and codes without their own line |
| `RevitType` | `Family::Type` as shown in Revit; `Type` alone takes the first type with that name |

- Fields are separated by a semicolon or a tab, so columns copied from Excel also work.
- Empty lines, lines starting with `#` and the `Category;Code;RevitType` header are skipped.
- Save the file as UTF-8 (`.txt` or `.csv`).
- Lines that cannot be read are listed with their line number and reason; the other lines are
  loaded.
- Codes in the file that are not in the current DXF are not listed but are kept for other
  drawings.

## 3. The import summary

At the end a summary shows what was created and what needs attention:

| Section | Meaning | What to do |
|---------|---------|------------|
| Walls / Columns / Openings created | Created count / requested count | |
| Already in the model, not created again | Elements skipped because they already exist | Nothing; uncheck the option to create them anyway |
| **Elements overlap existing elements** (title) and *Overlaps* | Revit found walls on top of walls or identical elements in the same place, usually because the drawing was imported twice | Undo (**Ctrl+Z**) removes the whole import in one step |
| Ambiguous face pairs skipped | A wall face could pair with two lines | Clean up the lines around the given coordinates |
| Walls / Columns / Openings failed | Elements Revit could not create, with the reason | Check the type, level or geometry named in the message |
| Column notes, Opening notes | Created, but differently (for example a round solid instead of a family, a family that cannot flip) | Usually nothing |
| Opening markers not imported | Blocks that broke a rule (no wall, outside the wall, overlapping) | Fix the block at the given DXF coordinates |
| Opening warnings | Missing attributes, depth mismatch, missing or unclear hinge circle, unmarked gaps in walls | Fix if the default or the orientation is not what you want |
| Other Revit warnings | Any other warning Revit gave while creating the elements | Read the text |

The whole import is **one undo step**: if the result is not what you expected, press Ctrl+Z
once, correct the drawing and import again.

## 4. Checklist

- Units are millimetres; the plan is in model space.
- Wall faces are `LINE`s on `WALL-<cm>` layers, parallel and exactly the thickness apart.
- Columns are closed polylines or circles on `COLUMN` layers.
- Every door, window and opening has a `door`, `window` or `opening` block in its wall,
  stretched from jamb to jamb.
- Doors and windows have one visible hinge circle, on the opening side, at the hinge end.
- Every door and window block has a `TYPE`; `HEIGHT` and `SILL_HEIGHT` are set where the
  defaults do not apply.
- In Revit, the levels exist and the door and window families are loaded.
- Every code has a Revit type in *Opening types and heights...*.
