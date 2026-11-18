using DxfToRevit.Core.Geometry;

namespace DxfToRevit.Core.Models;

/// <summary>
/// A DXF line after layer classification: whether it belongs to a wall layer, the wall thickness
/// encoded in the layer name (if any) and the source entity handle.
/// </summary>
public sealed class ClassifiedDxfLine
{
    /// <summary>2D line geometry in millimetres.</summary>
    public LineSegment2D Geometry { get; }

    /// <summary>Source DXF layer name (e.g. <c>WALL-20</c>).</summary>
    public string SourceLayer { get; }

    /// <summary>Whether the layer is a wall layer (<c>WALL</c> / <c>WALL-*</c>).</summary>
    public bool IsWallLayer { get; }

    /// <summary>
    /// Wall thickness in millimetres decoded from the layer name (<c>WALL-20</c> → 200);
    /// <c>null</c> when the layer carries no thickness code.
    /// </summary>
    public double? WallThicknessMm { get; }

    /// <summary>DXF entity handle (unique within the drawing).</summary>
    public string Handle { get; }

    /// <summary>Creates a classified line.</summary>
    /// <param name="geometry">Line segment.</param>
    /// <param name="sourceLayer">Layer name.</param>
    /// <param name="isWallLayer">Whether the layer is a wall layer.</param>
    /// <param name="wallThicknessMm">Thickness in millimetres, or <c>null</c>.</param>
    /// <param name="handle">DXF entity handle.</param>
    /// <exception cref="ArgumentNullException"><paramref name="geometry"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    /// A text value is empty, or a thickness is given for a non-wall layer.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="wallThicknessMm"/> is not positive.</exception>
    public ClassifiedDxfLine(
        LineSegment2D geometry,
        string sourceLayer,
        bool isWallLayer,
        double? wallThicknessMm,
        string handle)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        if (string.IsNullOrWhiteSpace(sourceLayer))
            throw new ArgumentException("Source layer cannot be empty.", nameof(sourceLayer));

        if (string.IsNullOrWhiteSpace(handle))
            throw new ArgumentException("DXF handle cannot be empty.", nameof(handle));

        if (wallThicknessMm.HasValue && wallThicknessMm.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(wallThicknessMm));

        if (wallThicknessMm.HasValue && !isWallLayer)
            throw new ArgumentException("Only wall layers can have a wall thickness.");

        Geometry = geometry;
        SourceLayer = sourceLayer;
        IsWallLayer = isWallLayer;
        WallThicknessMm = wallThicknessMm;
        Handle = handle;
    }
}
