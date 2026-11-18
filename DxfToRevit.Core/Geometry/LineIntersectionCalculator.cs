using netDxf;

namespace DxfToRevit.Core.Geometry;

/// <summary>
/// Finds the intersection of the supporting lines of two segments
/// (infinite-line intersection; segments need not contain the point).
/// Lines that are parallel within <see cref="GeometryTolerance.ParallelAngleDegrees"/>
/// are treated as non-intersecting.
/// </summary>
public sealed class LineIntersectionCalculator
{
    /// <summary>
    /// Tries to compute the intersection of the infinite lines through
    /// <paramref name="lineA"/> and <paramref name="lineB"/>.
    /// </summary>
    /// <returns>False when the lines are parallel (within tolerance) or a segment is degenerate.</returns>
    public bool TryFindIntersection(
        LineSegment2D lineA,
        LineSegment2D lineB,
        out Vector2 intersection)
    {
        ArgumentNullException.ThrowIfNull(lineA);
        ArgumentNullException.ThrowIfNull(lineB);

        intersection = default;

        if (lineA.IsDegenerate || lineB.IsDegenerate)
            return false;

        // Treat near-parallel lines as non-intersecting for junction purposes.
        if (lineA.IsParallelTo(lineB))
            return false;

        Vector2 p = lineA.Start;
        Vector2 r = lineA.Direction;
        Vector2 q = lineB.Start;
        Vector2 s = lineB.Direction;

        double rxs = Vector2.CrossProduct(r, s);
        if (Math.Abs(rxs) < 1e-12)
            return false;

        Vector2 qp = q - p;
        double t = Vector2.CrossProduct(qp, s) / rxs;
        intersection = p + r * t;
        return true;
    }
}
