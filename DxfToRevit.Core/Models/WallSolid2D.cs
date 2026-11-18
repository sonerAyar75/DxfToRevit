using DxfToRevit.Core.Geometry;
using netDxf;

namespace DxfToRevit.Core.Models;

/// <summary>
/// 2D wall footprint: the closed rectangle obtained by offsetting a centerline by half the wall
/// thickness on each side. Used for the pre-Revit DXF simulation and as input for wall creation.
/// </summary>
public sealed class WallSolid2D
{
    /// <summary>Source centerline in millimetres.</summary>
    public LineSegment2D CenterLine { get; }

    /// <summary>Wall thickness in millimetres.</summary>
    public double ThicknessMm { get; }

    /// <summary>
    /// Closed polygon vertices; the first vertex is not repeated (write it with the closed flag set).
    /// Order: start-left, end-left, end-right, start-right.
    /// </summary>
    public IReadOnlyList<Vector2> Outline { get; }

    /// <summary>Creates a footprint.</summary>
    /// <param name="centerLine">Source centerline.</param>
    /// <param name="thicknessMm">Wall thickness in millimetres; must be positive.</param>
    /// <param name="outline">Footprint vertices (at least three).</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="thicknessMm"/> is not positive.</exception>
    /// <exception cref="ArgumentException">The outline has fewer than three vertices.</exception>
    public WallSolid2D(
        LineSegment2D centerLine,
        double thicknessMm,
        IReadOnlyList<Vector2> outline)
    {
        ArgumentNullException.ThrowIfNull(centerLine);
        ArgumentNullException.ThrowIfNull(outline);

        if (thicknessMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(thicknessMm));

        if (outline.Count < 3)
            throw new ArgumentException("Outline must have at least 3 vertices.", nameof(outline));

        CenterLine = centerLine;
        ThicknessMm = thicknessMm;
        Outline = outline.ToList().AsReadOnly();
    }
}
