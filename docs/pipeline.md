# Centerline pipeline {#pipeline}

`DxfToRevit.Core.Services.DxfCenterLinePipeline` turns the wall lines of a DXF drawing into
centerlines and closed wall footprints, and reads the columns. `Analyze(path)` returns the
result without writing anything; `Run(path, output)` also writes the simulation DXF.

## Input conventions

| Element | DXF representation |
|---------|--------------------|
| Wall face | `LINE` on a layer `WALL` or `WALL-*` |
| Wall thickness | Layer suffix in centimetres: `WALL-15` → 150 mm, `WALL-20` → 200 mm |
| Column | Closed `LWPOLYLINE` or `CIRCLE` on `COLUMN`, `COLUMNS` or `COLUMN-*` |
| Units | Millimetres |

Only wall lines whose layer carries a thickness code take part in face pairing.

## Stages

### 1. Load and classify

`DxfReader` loads the file once. `DxfLineImportService` extracts the `LINE` entities
(`DxfGeometryReader`) and classifies each by layer (`DxfLineClassifier` + `WallLayerRule`):
wall or not, and the encoded thickness.

### 2. Face pairing

`WallFacePairFinder` compares every pair of wall lines. A candidate pair must be on the same
layer, parallel (within 0.1°), overlap along their length and be one wall thickness apart
(±1 mm). When one line has two partners over the same stretch, all of those candidates are
rejected and reported as *ambiguities* instead of guessing.

### 3. Wall models

`WallModelManager.CreateFromFacePair` trims both faces to their overlapping stretch
(`SegmentOverlapGeometry`) and places the centerline midway between them.

### 4. Open-side bridging

Pairing only yields centerlines where both faces overlap, so a wall with a niche, door frame or
column pocket on one side gets a broken centerline. `OpenSideCenterLineBridger` groups collinear
centerlines of the same layer and thickness and closes a gap when at least one *original* face
runs continuously across it. If both faces are interrupted, the gap is a real opening and is kept.

### 5. Junctions

`WallCenterLineJunctionProcessor` extends centerline ends to the intersections of their
supporting lines:

- intersection outside both segments → **L junction**, both ends extended;
- intersection on one segment only → **T junction**, the other line (the branch) is extended;
- intersection on both segments → already joined.

Each extension is limited by `JunctionExtensionLimit` so unrelated walls are not joined:

```
L = (T_other / 2) / tan(α / 2) + 5 mm
```

where `T_other` is the thickness of the crossing wall and `α` the acute angle between the walls
(clamped at 5°). At 90° the limit is about half the crossing wall thickness; it grows as the
angle gets sharper.

### 6. Collinear merge

`CollinearCenterLineMerger` merges collinear pieces of **equal thickness** whose gap is at most
5 mm. Pieces of different thickness on the same axis stay separate walls.

### 7. Footprints

`WallFootprintBuilder` offsets every centerline by ±T/2 into a closed rectangle (`WallSolid2D`).
These footprints are what the Revit add-in turns into walls.

### 8. Columns

`DxfColumnReader` reads closed polylines (flagged closed, or with first and last vertex within
1 mm) and circles on column layers, skipping sections below 100 mm² or circles below 10 mm radius.
Columns are numbered `C001`, `C002`, … See @ref columns.

## Thickness assignment

After bridging, every centerline gets the thickness of the closest overlapping parallel wall
centerline (within 1 mm, ties prefer the thicker wall). A centerline that matches no wall gets
`DxfCenterLinePipeline.DefaultThicknessMm` (200 mm).

## Simulation output

`DxfSimulationWriter` copies the input drawing and adds three layers:

| Layer | Contents |
|-------|----------|
| `WALL-CENTERLINE` | Final centerlines |
| `WALL-SOLID` | Closed wall footprints |
| `COLUMN-SIM` | Column sections |
