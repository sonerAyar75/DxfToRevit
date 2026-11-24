using DxfToRevit.Core.Geometry;
using netDxf;

namespace DxfToRevit.Core.Walls;

/// <summary>
/// Forms L and T junctions between wall centerlines by extending segment ends to the
/// intersections of their supporting lines.
/// </summary>
/// <remarks>
/// Every pair of centerlines is examined once, in order; the list keeps its order and count.
/// When thicknesses are supplied, each intersection gets its own extension limit from
/// <see cref="JunctionExtensionLimit"/> (angle and crossing wall thickness), so distant walls whose
/// extensions happen to cross are not joined.
/// <list type="bullet">
/// <item><description>Intersection on neither segment → L junction (both lines extended).</description></item>
/// <item><description>Intersection on one segment only → T junction (the other line is the branch).</description></item>
/// <item><description>Intersection on both segments → already joined, nothing to do.</description></item>
/// </list>
/// </remarks>
public sealed class WallCenterLineJunctionProcessor
{
    private readonly LineIntersectionCalculator _intersectionCalculator;
    private readonly LineSegmentEndpointAdjuster _endpointAdjuster;

    /// <summary>Creates the processor with its intersection calculator and endpoint adjuster.</summary>
    public WallCenterLineJunctionProcessor()
    {
        _intersectionCalculator = new LineIntersectionCalculator();
        _endpointAdjuster = new LineSegmentEndpointAdjuster();
    }

    /// <summary>
    /// Legacy entry point with a single global extension cap for every junction.
    /// Prefer the thickness-aware overload for production use.
    /// </summary>
    /// <param name="centerLines">Wall centerlines.</param>
    /// <param name="maxExtensionDistanceMm">Maximum extension (mm) for every segment end.</param>
    /// <returns>Adjusted centerlines in the same order and count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The limit is negative.</exception>
    public IReadOnlyList<LineSegment2D> Process(
        IEnumerable<LineSegment2D> centerLines,
        double maxExtensionDistanceMm)
    {
        ArgumentNullException.ThrowIfNull(centerLines);

        if (maxExtensionDistanceMm < 0)
            throw new ArgumentOutOfRangeException(nameof(maxExtensionDistanceMm));

        var result = centerLines.ToList();

        for (int i = 0; i < result.Count; i++)
        {
            for (int j = i + 1; j < result.Count; j++)
            {
                TryProcessPair(
                    result,
                    i,
                    j,
                    maxExtensionDistanceMm,
                    maxExtensionDistanceMm);
            }
        }

        return result;
    }

    /// <summary>
    /// Processes junctions using per-wall thicknesses so each intersection gets
    /// its own extension limit from the angle and the crossing wall's thickness.
    /// </summary>
    /// <param name="centerLines">Wall centerlines.</param>
    /// <param name="thicknessMm">Thickness of each centerline (same order and count).</param>
    /// <returns>Adjusted centerlines in the same order and count.</returns>
    /// <exception cref="ArgumentException">The two lists have different lengths.</exception>
    public IReadOnlyList<LineSegment2D> Process(
        IReadOnlyList<LineSegment2D> centerLines,
        IReadOnlyList<double> thicknessMm)
    {
        ArgumentNullException.ThrowIfNull(centerLines);
        ArgumentNullException.ThrowIfNull(thicknessMm);

        if (centerLines.Count != thicknessMm.Count)
            throw new ArgumentException(
                "centerLines and thicknessMm must have the same length.");

        var result = centerLines.ToList();
        var thicknesses = thicknessMm.ToList();

        for (int i = 0; i < result.Count; i++)
        {
            for (int j = i + 1; j < result.Count; j++)
            {
                JunctionExtensionLimit.ForPair(
                    result[i].Direction,
                    thicknesses[i],
                    result[j].Direction,
                    thicknesses[j],
                    out double allowA,
                    out double allowB);

                TryProcessPair(result, i, j, allowA, allowB);
            }
        }

        return result;
    }

    /// <summary>
    /// Classifies the junction between centerlines <paramref name="i"/> and <paramref name="j"/>
    /// (L, T or none) and writes the adjusted segments back into <paramref name="result"/>.
    /// </summary>
    private void TryProcessPair(
        List<LineSegment2D> result,
        int i,
        int j,
        double allowA,
        double allowB)
    {
        LineSegment2D lineA = result[i];
        LineSegment2D lineB = result[j];

        if (!_intersectionCalculator.TryFindIntersection(
                lineA,
                lineB,
                out Vector2 intersection))
        {
            return;
        }

        bool intersectionOnA = IsPointOnSegment(lineA, intersection);
        bool intersectionOnB = IsPointOnSegment(lineB, intersection);

        if (intersectionOnA && intersectionOnB)
            return;

        // L junction: both lines must be extended.
        if (!intersectionOnA && !intersectionOnB)
        {
            if (_endpointAdjuster.TryJoinAtIntersection(
                    lineA,
                    lineB,
                    intersection,
                    allowA,
                    allowB,
                    out LineSegment2D? adjustedA,
                    out LineSegment2D? adjustedB))
            {
                result[i] = adjustedA!;
                result[j] = adjustedB!;
            }

            return;
        }

        // T junction: A is the main line, B is the branch.
        if (intersectionOnA && !intersectionOnB)
        {
            if (_endpointAdjuster.TryExtendBranchToMainLine(
                    lineA,
                    lineB,
                    intersection,
                    allowB,
                    out LineSegment2D? adjustedBranch))
            {
                result[j] = adjustedBranch!;
            }

            return;
        }

        // T junction: B is the main line, A is the branch.
        if (!intersectionOnA && intersectionOnB)
        {
            if (_endpointAdjuster.TryExtendBranchToMainLine(
                    lineB,
                    lineA,
                    intersection,
                    allowA,
                    out LineSegment2D? adjustedBranch))
            {
                result[i] = adjustedBranch!;
            }
        }
    }

    /// <summary>Whether the point lies on the segment (endpoints included) within the perpendicular tolerance.</summary>
    private static bool IsPointOnSegment(
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

        return parameter >= 0.0 && parameter <= 1.0;
    }
}
