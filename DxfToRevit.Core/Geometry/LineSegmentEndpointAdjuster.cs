using netDxf;

namespace DxfToRevit.Core.Geometry;

/// <summary>
/// Moves segment endpoints onto an intersection point to form L junctions (both segments
/// extended) and T junctions (a branch extended onto a main line).
/// </summary>
/// <remarks>
/// Point-on-line checks use a 1 mm tolerance. Segments are only ever lengthened: an intersection
/// inside a segment interior is refused for L junctions so a segment is never shortened.
/// </remarks>
public sealed class LineSegmentEndpointAdjuster
{
    /// <summary>L junction: extends both segments to the intersection without a length limit.</summary>
    /// <param name="lineA">First segment.</param>
    /// <param name="lineB">Second segment.</param>
    /// <param name="intersection">Intersection of the supporting lines.</param>
    /// <param name="adjustedA">Extended first segment, when successful.</param>
    /// <param name="adjustedB">Extended second segment, when successful.</param>
    /// <returns><c>true</c> when both segments were joined.</returns>
    public bool TryJoinAtIntersection(
        LineSegment2D lineA,
        LineSegment2D lineB,
        Vector2 intersection,
        out LineSegment2D? adjustedA,
        out LineSegment2D? adjustedB)
    {
        return TryJoinAtIntersectionCore(
            lineA,
            lineB,
            intersection,
            null,
            null,
            out adjustedA,
            out adjustedB);
    }

    /// <summary>L junction: extends both segments to the intersection, each by at most the same limit.</summary>
    /// <param name="lineA">First segment.</param>
    /// <param name="lineB">Second segment.</param>
    /// <param name="intersection">Intersection of the supporting lines.</param>
    /// <param name="maxExtensionDistanceMm">Maximum extension per segment (mm).</param>
    /// <param name="adjustedA">Extended first segment, when successful.</param>
    /// <param name="adjustedB">Extended second segment, when successful.</param>
    /// <returns><c>true</c> when both segments were joined within the limit.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The limit is negative.</exception>
    public bool TryJoinAtIntersection(
        LineSegment2D lineA,
        LineSegment2D lineB,
        Vector2 intersection,
        double maxExtensionDistanceMm,
        out LineSegment2D? adjustedA,
        out LineSegment2D? adjustedB)
    {
        if (maxExtensionDistanceMm < 0)
            throw new ArgumentOutOfRangeException(nameof(maxExtensionDistanceMm));

        return TryJoinAtIntersectionCore(
            lineA,
            lineB,
            intersection,
            maxExtensionDistanceMm,
            maxExtensionDistanceMm,
            out adjustedA,
            out adjustedB);
    }

    /// <summary>
    /// L junction: extends each segment to the intersection within its own limit.
    /// </summary>
    /// <param name="lineA">First segment.</param>
    /// <param name="lineB">Second segment.</param>
    /// <param name="intersection">Intersection of the supporting lines.</param>
    /// <param name="maxExtensionA">Maximum extension of <paramref name="lineA"/> (mm).</param>
    /// <param name="maxExtensionB">Maximum extension of <paramref name="lineB"/> (mm).</param>
    /// <param name="adjustedA">Extended first segment, when successful.</param>
    /// <param name="adjustedB">Extended second segment, when successful.</param>
    /// <returns><c>true</c> when both segments were joined within their limits.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A limit is negative.</exception>
    public bool TryJoinAtIntersection(
        LineSegment2D lineA,
        LineSegment2D lineB,
        Vector2 intersection,
        double maxExtensionA,
        double maxExtensionB,
        out LineSegment2D? adjustedA,
        out LineSegment2D? adjustedB)
    {
        if (maxExtensionA < 0)
            throw new ArgumentOutOfRangeException(nameof(maxExtensionA));
        if (maxExtensionB < 0)
            throw new ArgumentOutOfRangeException(nameof(maxExtensionB));

        return TryJoinAtIntersectionCore(
            lineA,
            lineB,
            intersection,
            maxExtensionA,
            maxExtensionB,
            out adjustedA,
            out adjustedB);
    }

    /// <summary>
    /// Shared L-junction logic: the intersection must lie on both supporting lines and outside
    /// both segment interiors; the nearest endpoint of each segment is moved onto it when the
    /// required extension is within the optional limits.
    /// </summary>
    private bool TryJoinAtIntersectionCore(
        LineSegment2D lineA,
        LineSegment2D lineB,
        Vector2 intersection,
        double? maxExtensionA,
        double? maxExtensionB,
        out LineSegment2D? adjustedA,
        out LineSegment2D? adjustedB)
    {
        ArgumentNullException.ThrowIfNull(lineA);
        ArgumentNullException.ThrowIfNull(lineB);

        adjustedA = null;
        adjustedB = null;

        if (lineA.IsDegenerate || lineB.IsDegenerate)
            return false;

        if (!IsPointOnLine(lineA, intersection) ||
            !IsPointOnLine(lineB, intersection))
        {
            return false;
        }

        // L-join only when the intersection is outside both segment interiors.
        // Intersection exactly at an endpoint is fine (zero-length extension).
        // Intersection in the interior would shrink the segment — refuse.
        if (IsPointInSegmentInterior(lineA, intersection) ||
            IsPointInSegmentInterior(lineB, intersection))
        {
            return false;
        }

        double distanceA = Math.Min(
            Vector2.Distance(lineA.Start, intersection),
            Vector2.Distance(lineA.End, intersection));

        double distanceB = Math.Min(
            Vector2.Distance(lineB.Start, intersection),
            Vector2.Distance(lineB.End, intersection));

        if (maxExtensionA.HasValue && distanceA > maxExtensionA.Value)
            return false;

        if (maxExtensionB.HasValue && distanceB > maxExtensionB.Value)
            return false;

        adjustedA = MoveNearestEndpoint(lineA, intersection);
        adjustedB = MoveNearestEndpoint(lineB, intersection);

        return true;
    }

    /// <summary>T junction: extends the branch onto the main line without a length limit.</summary>
    /// <param name="mainLine">Line the branch ends on; it is not modified.</param>
    /// <param name="branchLine">Line to extend.</param>
    /// <param name="intersection">Intersection of the supporting lines.</param>
    /// <param name="adjustedBranch">Extended branch, when successful.</param>
    /// <returns><c>true</c> when the branch was extended.</returns>
    public bool TryExtendBranchToMainLine(
        LineSegment2D mainLine,
        LineSegment2D branchLine,
        Vector2 intersection,
        out LineSegment2D? adjustedBranch)
    {
        return TryExtendBranchToMainLineCore(
            mainLine,
            branchLine,
            intersection,
            null,
            out adjustedBranch);
    }

    /// <summary>T junction: extends the branch onto the main line by at most the given limit.</summary>
    /// <param name="mainLine">Line the branch ends on; it is not modified.</param>
    /// <param name="branchLine">Line to extend.</param>
    /// <param name="intersection">Intersection of the supporting lines.</param>
    /// <param name="maxExtensionDistanceMm">Maximum extension (mm).</param>
    /// <param name="adjustedBranch">Extended branch, when successful.</param>
    /// <returns><c>true</c> when the branch was extended within the limit.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The limit is negative.</exception>
    public bool TryExtendBranchToMainLine(
        LineSegment2D mainLine,
        LineSegment2D branchLine,
        Vector2 intersection,
        double maxExtensionDistanceMm,
        out LineSegment2D? adjustedBranch)
    {
        if (maxExtensionDistanceMm < 0)
            throw new ArgumentOutOfRangeException(nameof(maxExtensionDistanceMm));

        return TryExtendBranchToMainLineCore(
            mainLine,
            branchLine,
            intersection,
            maxExtensionDistanceMm,
            out adjustedBranch);
    }

    /// <summary>
    /// Shared T-junction logic: the intersection must lie within the main segment (1 mm
    /// tolerance) and beyond the branch segment; the nearest branch endpoint is moved onto it
    /// when the extension is within the optional limit.
    /// </summary>
    private bool TryExtendBranchToMainLineCore(
        LineSegment2D mainLine,
        LineSegment2D branchLine,
        Vector2 intersection,
        double? maxExtensionDistanceMm,
        out LineSegment2D? adjustedBranch)
    {
        ArgumentNullException.ThrowIfNull(mainLine);
        ArgumentNullException.ThrowIfNull(branchLine);

        adjustedBranch = null;

        if (mainLine.IsDegenerate || branchLine.IsDegenerate)
            return false;

        Vector2 mainDirection = mainLine.Direction;

        double mainLengthSquared =
            Vector2.DotProduct(mainDirection, mainDirection);

        double mainParameter = Vector2.DotProduct(
            intersection - mainLine.Start,
            mainDirection) / mainLengthSquared;

        double distanceFromMain =
            Math.Abs(Vector2.CrossProduct(
                mainDirection,
                intersection - mainLine.Start))
            / mainDirection.Modulus();

        const double toleranceMm = 1.0;

        if (!IsPointOnLine(mainLine, intersection) ||
            !IsPointOnLine(branchLine, intersection))
        {
            return false;
        }

        if (mainParameter < 0.0 ||
            mainParameter > 1.0 ||
            distanceFromMain > toleranceMm)
        {
            return false;
        }

        Vector2 branchDirection = branchLine.Direction;

        double branchLengthSquared =
            Vector2.DotProduct(
                branchDirection,
                branchDirection);

        double branchParameter = Vector2.DotProduct(
            intersection - branchLine.Start,
            branchDirection) / branchLengthSquared;

        if (branchParameter >= 0.0 &&
            branchParameter <= 1.0)
        {
            return false;
        }

        double distanceToStart =
            Vector2.Distance(branchLine.Start, intersection);

        double distanceToEnd =
            Vector2.Distance(branchLine.End, intersection);

        double extensionDistance = Math.Min(
            distanceToStart,
            distanceToEnd);

        if (maxExtensionDistanceMm.HasValue &&
            extensionDistance > maxExtensionDistanceMm.Value)
        {
            return false;
        }

        adjustedBranch = distanceToStart <= distanceToEnd
            ? new LineSegment2D(intersection, branchLine.End)
            : new LineSegment2D(branchLine.Start, intersection);

        return true;
    }

    /// <summary>Returns a copy of the segment whose endpoint nearest to <paramref name="intersection"/> is moved onto it.</summary>
    private static LineSegment2D MoveNearestEndpoint(
        LineSegment2D line,
        Vector2 intersection)
    {
        double distStart = Vector2.Distance(line.Start, intersection);
        double distEnd = Vector2.Distance(line.End, intersection);

        return distStart <= distEnd
            ? new LineSegment2D(intersection, line.End)
            : new LineSegment2D(line.Start, intersection);
    }

    /// <summary>
    /// Whether the point lies on the segment strictly between its endpoints (within the
    /// perpendicular tolerance); endpoints themselves do not count.
    /// </summary>
    private static bool IsPointInSegmentInterior(
        LineSegment2D line,
        Vector2 point,
        double toleranceMm = 1.0)
    {
        Vector2 direction = line.Direction;
        double length = direction.Modulus();
        if (length == 0.0)
            return false;

        Vector2 offset = point - line.Start;
        double distance = Math.Abs(
            Vector2.CrossProduct(direction, offset)) / length;
        if (distance > toleranceMm)
            return false;

        double parameter =
            (offset.X * direction.X + offset.Y * direction.Y)
            / (length * length);

        // Strict interior — endpoints are allowed for L-join.
        return parameter > 1e-9 && parameter < 1.0 - 1e-9;
    }

    /// <summary>Whether the point lies on the infinite supporting line within the tolerance.</summary>
    private static bool IsPointOnLine(
        LineSegment2D line,
        Vector2 point,
        double toleranceMm = 1.0)
    {
        Vector2 direction = line.Direction;
        double length = direction.Modulus();

        if (length == 0.0)
            return false;

        double distance = Math.Abs(
            Vector2.CrossProduct(
                direction,
                point - line.Start)) / length;

        return distance <= toleranceMm;
    }
}
