namespace DxfToRevit.Core.Building;

/// <summary>
/// Classification of a column cross-section, which decides how the host creates the column.
/// </summary>
public enum ColumnSectionKind
{
    /// <summary>
    /// Rectangle (axis-aligned or rotated). <see cref="ColumnTypeProperties.WidthMm"/> and
    /// <see cref="ColumnTypeProperties.DepthMm"/> are the true side lengths; any rotation is carried by
    /// <see cref="ColumnCreationRequest.RotationRadians"/>.
    /// </summary>
    Rectangular,

    /// <summary>Circle. <see cref="ColumnTypeProperties.RadiusMm"/> / diameter describe the section.</summary>
    Circular,

    /// <summary>Any other closed polygon (L, T, wall-like piers, ...). The outline itself is the geometry.</summary>
    Freeform
}
