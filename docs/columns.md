# Columns {#columns}

## Classification

`ColumnCreationRequestFactory` classifies each `ColumnModel` (in this order):

| Section | Kind | Size and placement |
|---------|------|--------------------|
| Circle | `Circular` | Diameter from the circle; placed at the centre |
| Axis-aligned rectangle | `Rectangular` | Width = X extent, depth = Y extent; rotation 0 |
| Rotated rectangle | `Rectangular` | True side lengths; plan rotation normalised to (−45°, 45°] |
| Any other closed polygon | `Freeform` | The outline itself |

A rotated rectangle is recognised by `PolygonMetrics2D.TryGetOrientedRectangle`: four vertices,
every corner within 1° of 90°, and opposite sides within 2 mm of each other. The normalised angle
makes the result independent of vertex order and winding; each 90° step swaps width and depth.

Validation (`ColumnCreationValidator`): circular radius ≥ 25 mm, section area ≥ 2500 mm² and,
for rectangles, each true side ≥ 50 mm.

## Creation in Revit

All columns are placed at the section centroid on the base level, with the base offset applied.
With a top level selected they are bound to it (Top Offset 0), otherwise they get the unconnected
height. Structural columns use `StructuralType.Column`.

### Rectangular

A loaded rectangular column type with the right width and depth is used; otherwise a type of the
chosen family is duplicated and resized (`RevitColumnTypeResolver`). The instance is then rotated
about its vertical axis by the plan rotation.

### Circular

The settings dialog offers four modes:

| Mode | Behaviour |
|------|-----------|
| Use a circular column family (default: *Automatic*) | See the chain below |
| Round solids | Cylindrical DirectShape, no family needed |
| Replace with squares | Square column whose side equals the diameter |
| Do not import | Circular columns are skipped |

The *Automatic* option (`ColumnTypeKeys.AutoRoundFamily`) tries, in order:

1. a loaded column type with a diameter parameter (same diameter first, then families whose name
   suggests a round column), duplicated with the DXF diameter when needed;
2. a round family loaded from disk: first any `.rfa` in the add-in's `Resources/Families` folder,
   then the Revit library (`M_Round Column.rfa`, `M_Concrete-Round-Column.rfa`, …);
3. a cylindrical DirectShape. The import summary lists these columns under "Column notes".

### Freeform

With *Create freeform columns as column families* (default), `RevitFreeformColumnFamilyBuilder`
builds a loadable family from the Revit column template (`Metric Column.rft`):

- if the template contains a locked extrusion, only its sketch is replaced with the DXF section,
  so the extrusion stays locked to the lower and upper reference levels;
- otherwise a new extrusion is created and its top and bottom faces are aligned and locked to
  the levels in an elevation view.

The family is saved to a temporary file, loaded into the project and placed like any other
column, so it follows the base and top levels. Identical sections share one family: the name is
derived from the shape (`FreeformColumnFamilyNaming`, e.g. `DxfToRevit Freeform 400x400 - 1a2b3c4d`),
independent of position, start vertex and winding.

If the family cannot be built, or the option is off, the column becomes an extruded DirectShape
in the Columns category (Generic Model when Revit does not allow Columns). DirectShapes cannot be
bound to levels; their height is fixed at creation time.

## Parameter names

Family size parameters are matched by name (`RevitColumnParameterNames`): `Width`/`b`,
`Depth`/`h`, `Diameter`/`d`, plus the names used by Turkish-localised families. When no name
matches, the error lists the writable length parameters the family actually has.
