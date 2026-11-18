using netDxf;

namespace DxfToRevit.Core.Geometry;

/// <summary>
/// Immutable 2D line segment. Coordinates use the DXF unit (millimetres).
/// </summary>
public sealed class LineSegment2D
{
    /// <summary>Start point.</summary>
    public Vector2 Start { get; }

    /// <summary>End point.</summary>
    public Vector2 End { get; }

    /// <summary>Creates a segment.</summary>
    /// <param name="start">Segment start.</param>
    /// <param name="end">Segment end.</param>
    public LineSegment2D(Vector2 start, Vector2 end)
    {
        Start = start;
        End = end;
    }

    /// <summary>Segment length.</summary>
    public double Length => Vector2.Distance(Start, End);

    /// <summary>Start → End vector (not normalised).</summary>
    public Vector2 Direction => End - Start;

    /// <summary>Midpoint.</summary>
    public Vector2 MidPoint => Vector2.MidPoint(Start, End);

    /// <summary>Whether the segment has zero length.</summary>
    public bool IsDegenerate => Vector2.IsZero(Direction);

    /// <summary>
    /// Returns true if this segment is parallel to <paramref name="other"/>
    /// within <see cref="GeometryTolerance.ParallelAngleDegrees"/>.
    /// Degenerate segments are never parallel.
    /// </summary>
    /// <param name="other">Segment to compare with.</param>
    public bool IsParallelTo(LineSegment2D other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (IsDegenerate || other.IsDegenerate)
            return false;

        Vector2 directionA = Direction;
        Vector2 directionB = other.Direction;

        double crossProduct = Vector2.CrossProduct(directionA, directionB);
        double lengthA = directionA.Modulus();
        double lengthB = directionB.Modulus();

        double sinTolerance =
            Math.Sin(GeometryTolerance.ParallelAngleDegrees * Math.PI / 180.0);

        return Math.Abs(crossProduct) / (lengthA * lengthB) <= sinTolerance;
    }

    /// <summary>
    /// Perpendicular distance between the supporting lines of two parallel segments.
    /// </summary>
    /// <param name="other">Parallel segment.</param>
    /// <exception cref="InvalidOperationException">Segments are not parallel.</exception>
    public double GetDistanceToParallelLine(LineSegment2D other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (!IsParallelTo(other))
        {
            throw new InvalidOperationException(
                "Distance can only be calculated for parallel lines.");
        }

        Vector2 direction = Direction;
        Vector2 offset = other.Start - Start;
        double crossProduct = Vector2.CrossProduct(direction, offset);
        return Math.Abs(crossProduct) / direction.Modulus();
    }

    /// <summary>
    /// Returns true if the projections of the two parallel segments onto this
    /// segment's axis overlap with a positive length.
    /// </summary>
    /// <param name="other">Segment to compare with.</param>
    public bool OverlapsAlongLength(LineSegment2D other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (IsDegenerate || other.IsDegenerate || !IsParallelTo(other))
            return false;

        Vector2 direction = Direction;
        double length = direction.Modulus();
        Vector2 unitDirection = direction / length;

        double projectionBStart =
            Vector2.DotProduct(other.Start - Start, unitDirection);
        double projectionBEnd =
            Vector2.DotProduct(other.End - Start, unitDirection);

        double bMin = Math.Min(projectionBStart, projectionBEnd);
        double bMax = Math.Max(projectionBStart, projectionBEnd);

        double overlapStart = Math.Max(0, bMin);
        double overlapEnd = Math.Min(length, bMax);

        return overlapEnd > overlapStart;
    }
}
