using DxfToRevit.Core.Geometry;
using netDxf;

namespace DxfToRevit.Core.Walls;

/// <summary>
/// Merges collinear centerline pieces. Pieces whose gap is at most the configured maximum are
/// combined into one segment; larger gaps (real openings) are preserved.
/// </summary>
/// <remarks>
/// Pieces are first grouped by collinearity (parallel within <see cref="GeometryTolerance.ParallelAngleDegrees"/>
/// and offset within <see cref="GeometryTolerance.PointDistanceMm"/>). Each group is projected onto
/// the axis of its longest piece, the 1D intervals are merged, and the merged intervals are mapped
/// back to segments. The thickness-aware overload only groups pieces of equal thickness, so collinear
/// walls of different thickness stay separate.
/// </remarks>
public sealed class CollinearCenterLineMerger
{
    private readonly double _maxGapMm;

    /// <summary>Creates the merger.</summary>
    /// <param name="maxGapMm">Largest gap (mm) that is still closed by merging; default 5.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxGapMm"/> is negative.</exception>
    public CollinearCenterLineMerger(double maxGapMm = 5.0)
    {
        if (maxGapMm < 0)
            throw new ArgumentOutOfRangeException(nameof(maxGapMm));

        _maxGapMm = maxGapMm;
    }

    /// <summary>A merged segment and the wall thickness it carries.</summary>
    /// <param name="Segment">Merged centerline segment.</param>
    /// <param name="ThicknessMm">Wall thickness in millimetres (0 when unknown).</param>
    public readonly record struct MergedSegment(LineSegment2D Segment, double ThicknessMm);

    /// <summary>Tolerance (mm) within which two thicknesses are considered equal when grouping.</summary>
    public const double ThicknessToleranceMm = 0.1;

    /// <summary>
    /// Merges collinear, nearby pieces without considering thickness: every collinear piece
    /// joins the same group.
    /// </summary>
    /// <param name="centerLines">Centerline pieces; <c>null</c> and degenerate pieces are ignored.</param>
    /// <returns>The merged centerlines.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="centerLines"/> is <c>null</c>.</exception>
    public IReadOnlyList<LineSegment2D> Merge(
        IEnumerable<LineSegment2D> centerLines)
    {
        ArgumentNullException.ThrowIfNull(centerLines);

        var items = centerLines
            .Where(s => s is not null && !s.IsDegenerate)
            .Select(s => new MergedSegment(s, 0))
            .ToList();

        return MergeCore(items).Select(m => m.Segment).ToList();
    }

    /// <summary>
    /// Thickness-aware merge: only collinear pieces with the same thickness
    /// (± <see cref="ThicknessToleranceMm"/>) are merged, so a 100 mm and a 200 mm wall on the
    /// same axis remain separate walls.
    /// </summary>
    /// <param name="centerLines">Centerline pieces; <c>null</c> and degenerate pieces are ignored.</param>
    /// <param name="thicknessMm">Thickness of each piece (same order and count).</param>
    /// <returns>The merged segments with their thickness.</returns>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The two lists have different lengths.</exception>
    public IReadOnlyList<MergedSegment> Merge(
        IReadOnlyList<LineSegment2D> centerLines,
        IReadOnlyList<double> thicknessMm)
    {
        ArgumentNullException.ThrowIfNull(centerLines);
        ArgumentNullException.ThrowIfNull(thicknessMm);

        if (centerLines.Count != thicknessMm.Count)
            throw new ArgumentException("centerLines and thicknessMm must have the same length.");

        var items = new List<MergedSegment>(centerLines.Count);
        for (int i = 0; i < centerLines.Count; i++)
        {
            LineSegment2D s = centerLines[i];
            if (s is null || s.IsDegenerate)
                continue;
            items.Add(new MergedSegment(s, thicknessMm[i]));
        }

        return MergeCore(items);
    }

    /// <summary>Groups the pieces and merges each group; every output keeps its group's thickness.</summary>
    private List<MergedSegment> MergeCore(List<MergedSegment> items)
    {
        var result = new List<MergedSegment>();

        foreach (var group in BuildCollinearGroups(items))
        {
            double thickness = group[0].ThicknessMm;
            foreach (LineSegment2D segment in MergeGroup(group.Select(g => g.Segment).ToList()))
                result.Add(new MergedSegment(segment, thickness));
        }

        return result;
    }

    /// <summary>
    /// Groups pieces that are collinear and of equal thickness. When a piece matches several
    /// existing groups, those groups are combined (transitive grouping).
    /// </summary>
    private static List<List<MergedSegment>> BuildCollinearGroups(
        List<MergedSegment> items)
    {
        var groups = new List<List<MergedSegment>>();

        foreach (MergedSegment item in items)
        {
            var matching = groups
                .Where(g => g.Any(existing =>
                    Math.Abs(existing.ThicknessMm - item.ThicknessMm) <= ThicknessToleranceMm
                    && AreCollinear(item.Segment, existing.Segment)))
                .ToList();

            if (matching.Count == 0)
            {
                groups.Add(new List<MergedSegment> { item });
                continue;
            }

            matching[0].Add(item);

            foreach (var other in matching.Skip(1))
            {
                matching[0].AddRange(other);
                groups.Remove(other);
            }
        }

        return groups;
    }

    /// <summary>Parallel and on (almost) the same supporting line.</summary>
    private static bool AreCollinear(
        LineSegment2D a,
        LineSegment2D b)
    {
        if (!a.IsParallelTo(b))
            return false;

        return a.GetDistanceToParallelLine(b)
               <= GeometryTolerance.PointDistanceMm;
    }

    /// <summary>
    /// Merges one collinear group: projects every piece onto the axis of the longest piece,
    /// merges intervals that overlap or are separated by at most the maximum gap, and converts the
    /// merged intervals back to segments (pieces shorter than the point tolerance are dropped).
    /// </summary>
    private List<LineSegment2D> MergeGroup(
        List<LineSegment2D> group)
    {
        if (group.Count == 1)
            return new List<LineSegment2D> { group[0] };

        // Build a stable axis from the longest segment.
        LineSegment2D reference = group
            .OrderByDescending(s => s.Length)
            .First();

        Vector2 direction = reference.Direction;
        double dirLen = direction.Modulus();
        Vector2 unit = direction / dirLen;

        // Prefer a consistent orientation (+X, or +Y for vertical lines).
        if (unit.X < 0 ||
            (Math.Abs(unit.X) < 1e-10 && unit.Y < 0))
        {
            unit = new Vector2(-unit.X, -unit.Y);
        }

        Vector2 origin = reference.Start;

        // Collect projected intervals [min, max] along the axis.
        var intervals = new List<(double Min, double Max)>();

        foreach (LineSegment2D segment in group)
        {
            double p0 = Project(segment.Start, origin, unit);
            double p1 = Project(segment.End, origin, unit);
            double min = Math.Min(p0, p1);
            double max = Math.Max(p0, p1);
            intervals.Add((min, max));
        }

        intervals.Sort((a, b) => a.Min.CompareTo(b.Min));

        // Merge intervals that overlap or whose gap is at most maxGap.
        var merged = new List<(double Min, double Max)>();
        double curMin = intervals[0].Min;
        double curMax = intervals[0].Max;

        for (int i = 1; i < intervals.Count; i++)
        {
            double nextMin = intervals[i].Min;
            double nextMax = intervals[i].Max;

            if (nextMin <= curMax + _maxGapMm)
            {
                curMax = Math.Max(curMax, nextMax);
            }
            else
            {
                merged.Add((curMin, curMax));
                curMin = nextMin;
                curMax = nextMax;
            }
        }

        merged.Add((curMin, curMax));

        var result = new List<LineSegment2D>(merged.Count);
        foreach (var (min, max) in merged)
        {
            if (max - min < GeometryTolerance.PointDistanceMm)
                continue;

            Vector2 start = origin + unit * min;
            Vector2 end = origin + unit * max;
            result.Add(new LineSegment2D(start, end));
        }

        return result;
    }

    /// <summary>Signed distance of <paramref name="point"/> along the axis through <paramref name="origin"/>.</summary>
    private static double Project(
        Vector2 point,
        Vector2 origin,
        Vector2 unitDirection)
    {
        return (point.X - origin.X) * unitDirection.X
             + (point.Y - origin.Y) * unitDirection.Y;
    }
}
