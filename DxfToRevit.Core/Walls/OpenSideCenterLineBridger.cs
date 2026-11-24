using DxfToRevit.Core.Geometry;
using netDxf;

namespace DxfToRevit.Core.Walls;

/// <summary>
/// Closes centerline gaps of walls that are open on one side only.
/// </summary>
/// <remarks>
/// Face pairing only produces centerlines where both faces overlap, so a wall with a niche or a
/// door frame on one side gets a broken centerline. Collinear centerlines of the same layer and
/// thickness are grouped; a gap between two pieces is bridged when at least one original
/// (untrimmed) face runs continuously across it. When both faces are interrupted the gap is a real
/// opening and is preserved — unless an opening marker covers it: a marked door, window or opening
/// sits in a continuous wall, so the gap is bridged, and a wall end inside a marker is extended to
/// the far side of the marker (for openings next to a column or a corner).
/// </remarks>
public sealed class OpenSideCenterLineBridger
{
    /// <summary>
    /// Applies the open-side bridge using the wall models and their original face pairs.
    /// <paramref name="walls"/> and <paramref name="pairs"/> must be aligned
    /// (same order and count) so each wall's original face geometry is available.
    /// </summary>
    /// <param name="walls">Wall models; walls without a centerline are ignored.</param>
    /// <param name="pairs">Face pair of each wall, in the same order.</param>
    /// <returns>Bridged centerlines.</returns>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The two lists have different lengths.</exception>
    public IReadOnlyList<LineSegment2D> Bridge(
        IReadOnlyList<WallModel> walls,
        IReadOnlyList<WallFacePair> pairs) =>
        Bridge(walls, pairs, Array.Empty<IReadOnlyList<Vector2>>(), out _);

    /// <summary>
    /// Applies the open-side bridge and also bridges every gap covered by an opening marker.
    /// </summary>
    /// <param name="walls">Wall models; walls without a centerline are ignored.</param>
    /// <param name="pairs">Face pair of each wall, in the same order.</param>
    /// <param name="openingOutlines">Opening marker rectangles: gaps inside them are bridged and wall ends inside them are extended through them.</param>
    /// <param name="openGaps">Gaps between collinear pieces that were left open (centerline segment across each gap).</param>
    /// <returns>Bridged centerlines.</returns>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The wall and pair lists have different lengths.</exception>
    public IReadOnlyList<LineSegment2D> Bridge(
        IReadOnlyList<WallModel> walls,
        IReadOnlyList<WallFacePair> pairs,
        IReadOnlyList<IReadOnlyList<Vector2>> openingOutlines,
        out IReadOnlyList<LineSegment2D> openGaps)
    {
        ArgumentNullException.ThrowIfNull(walls);
        ArgumentNullException.ThrowIfNull(pairs);
        ArgumentNullException.ThrowIfNull(openingOutlines);

        if (walls.Count != pairs.Count)
            throw new ArgumentException("walls and pairs must have the same count.");

        var items = new List<WallItem>();
        for (int i = 0; i < walls.Count; i++)
        {
            if (walls[i].CenterLine is null)
                continue;
            items.Add(new WallItem(walls[i], pairs[i]));
        }

        var gaps = new List<LineSegment2D>();
        openGaps = gaps;
        if (items.Count == 0)
            return Array.Empty<LineSegment2D>();

        var groups = BuildCollinearGroups(items);
        var result = new List<LineSegment2D>();

        foreach (var group in groups)
            result.AddRange(BridgeGroup(group, openingOutlines, gaps));

        return result;
    }

    /// <summary>Groups walls that share layer and thickness and whose centerlines are collinear (transitively).</summary>
    private static List<List<WallItem>> BuildCollinearGroups(List<WallItem> items)
    {
        var groups = new List<List<WallItem>>();

        foreach (WallItem item in items)
        {
            var matching = groups
                .Where(g => g.Any(existing =>
                    string.Equals(existing.Layer, item.Layer, StringComparison.OrdinalIgnoreCase)
                    && Math.Abs(existing.Thickness - item.Thickness) < 0.1
                    && AreCollinear(existing.CenterLine, item.CenterLine)))
                .ToList();

            if (matching.Count == 0)
            {
                groups.Add(new List<WallItem> { item });
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
    private static bool AreCollinear(LineSegment2D a, LineSegment2D b)
    {
        if (!a.IsParallelTo(b))
            return false;
        return a.GetDistanceToParallelLine(b) <= GeometryTolerance.PointDistanceMm;
    }

    /// <summary>
    /// Merges one collinear group: centerline intervals are joined when they touch, when an
    /// original face covers the gap between them, or when an opening marker covers it. Ends lying
    /// inside a marker are extended through it. Gaps left open are added to <paramref name="openGaps"/>.
    /// </summary>
    private static List<LineSegment2D> BridgeGroup(
        List<WallItem> group,
        IReadOnlyList<IReadOnlyList<Vector2>> openingOutlines,
        List<LineSegment2D> openGaps)
    {
        if (group.Count == 1 && openingOutlines.Count == 0)
            return new List<LineSegment2D> { group[0].CenterLine };

        LineSegment2D reference = group
            .OrderByDescending(g => g.CenterLine.Length)
            .First()
            .CenterLine;

        Vector2 direction = reference.Direction;
        double dirLen = direction.Modulus();
        Vector2 unit = direction / dirLen;
        if (unit.X < 0 || (Math.Abs(unit.X) < 1e-10 && unit.Y < 0))
            unit = new Vector2(-unit.X, -unit.Y);

        Vector2 origin = reference.Start;

        var centerIntervals = group
            .Select(g => ProjectInterval(g.CenterLine, origin, unit))
            .OrderBy(iv => iv.Min)
            .ToList();

        // Full original faces from pairs (not overlap-trimmed).
        var faceIntervals = new List<(double Min, double Max)>();
        foreach (WallItem item in group)
        {
            foreach (LineSegment2D face in new[] { item.FullFaceA, item.FullFaceB })
            {
                if (face.IsDegenerate || !face.IsParallelTo(reference))
                    continue;

                double dist = reference.GetDistanceToParallelLine(face);
                if (dist > item.Thickness + 1.0)
                    continue;

                faceIntervals.Add(ProjectInterval(face, origin, unit));
            }
        }

        var merged = new List<(double Min, double Max)>();
        double curMin = centerIntervals[0].Min;
        double curMax = centerIntervals[0].Max;

        for (int i = 1; i < centerIntervals.Count; i++)
        {
            double nextMin = centerIntervals[i].Min;
            double nextMax = centerIntervals[i].Max;

            if (nextMin <= curMax + GeometryTolerance.PointDistanceMm)
            {
                curMax = Math.Max(curMax, nextMax);
                continue;
            }

            Vector2 gapStart = origin + unit * curMax;
            Vector2 gapEnd = origin + unit * nextMin;
            if (FaceCoversGap(faceIntervals, curMax, nextMin) || OpeningCoversGap(openingOutlines, gapStart, gapEnd))
                curMax = Math.Max(curMax, nextMax);
            else
            {
                openGaps.Add(new LineSegment2D(gapStart, gapEnd));
                merged.Add((curMin, curMax));
                curMin = nextMin;
                curMax = nextMax;
            }
        }

        merged.Add((curMin, curMax));

        var result = new List<LineSegment2D>();
        foreach (var (rawMin, rawMax) in merged)
        {
            (double min, double max) = ExtendEndsThroughOpenings(
                openingOutlines, origin, unit, group[0].Thickness, rawMin, rawMax);
            if (max - min < GeometryTolerance.PointDistanceMm)
                continue;
            result.Add(new LineSegment2D(
                origin + unit * min,
                origin + unit * max));
        }

        return result;
    }

    /// <summary>Whether one opening marker contains both ends of the gap (within the point tolerance).</summary>
    private static bool OpeningCoversGap(
        IReadOnlyList<IReadOnlyList<Vector2>> openingOutlines, Vector2 gapStart, Vector2 gapEnd)
    {
        const double tol = GeometryTolerance.PointDistanceMm;
        return openingOutlines.Any(outline =>
            PolygonMetrics2D.ContainsPoint(outline, gapStart, tol) &&
            PolygonMetrics2D.ContainsPoint(outline, gapEnd, tol));
    }

    /// <summary>
    /// Extends an interval end that lies inside an opening marker to the far side of that marker
    /// along the axis, so a wall ending at an opening (next to a column or corner) runs through it.
    /// Only markers centred on this wall (within half its thickness) are considered, so a wall that
    /// merely touches another wall's opening is not extended.
    /// </summary>
    private static (double Min, double Max) ExtendEndsThroughOpenings(
        IReadOnlyList<IReadOnlyList<Vector2>> openingOutlines, Vector2 origin, Vector2 unit,
        double thickness, double min, double max)
    {
        const double tol = GeometryTolerance.PointDistanceMm;
        var normal = new Vector2(-unit.Y, unit.X);
        foreach (IReadOnlyList<Vector2> outline in openingOutlines)
        {
            var center = new Vector2(outline.Average(p => p.X), outline.Average(p => p.Y));
            if (Math.Abs(Vector2.DotProduct(center - origin, normal)) > thickness / 2.0 + tol)
                continue;

            double markerMin = outline.Min(p => Project(p, origin, unit));
            double markerMax = outline.Max(p => Project(p, origin, unit));

            if (PolygonMetrics2D.ContainsPoint(outline, origin + unit * max, tol))
                max = Math.Max(max, markerMax);
            if (PolygonMetrics2D.ContainsPoint(outline, origin + unit * min, tol))
                min = Math.Min(min, markerMin);
        }

        return (min, max);
    }

    /// <summary>
    /// Whether a single face interval, or a chain of touching face intervals, spans the whole gap
    /// [<paramref name="gapStart"/>, <paramref name="gapEnd"/>] within the point tolerance.
    /// </summary>
    private static bool FaceCoversGap(
        List<(double Min, double Max)> faceIntervals,
        double gapStart,
        double gapEnd)
    {
        const double tol = GeometryTolerance.PointDistanceMm;

        foreach (var (min, max) in faceIntervals)
        {
            if (min <= gapStart + tol && max >= gapEnd - tol)
                return true;
        }

        var covering = faceIntervals
            .Where(f => f.Max >= gapStart - tol && f.Min <= gapEnd + tol)
            .OrderBy(f => f.Min)
            .ToList();

        if (covering.Count == 0)
            return false;

        if (covering[0].Min > gapStart + tol)
            return false;

        double coveredTo = covering[0].Max;
        for (int i = 1; i < covering.Count; i++)
        {
            if (covering[i].Min > coveredTo + tol)
                break;
            coveredTo = Math.Max(coveredTo, covering[i].Max);
        }

        return coveredTo >= gapEnd - tol;
    }

    /// <summary>Projects a segment onto the axis and returns its [min, max] interval.</summary>
    private static (double Min, double Max) ProjectInterval(
        LineSegment2D segment,
        Vector2 origin,
        Vector2 unit)
    {
        double p0 = Project(segment.Start, origin, unit);
        double p1 = Project(segment.End, origin, unit);
        return (Math.Min(p0, p1), Math.Max(p0, p1));
    }

    /// <summary>Signed distance of <paramref name="point"/> along the axis through <paramref name="origin"/>.</summary>
    private static double Project(Vector2 point, Vector2 origin, Vector2 unit) =>
        (point.X - origin.X) * unit.X + (point.Y - origin.Y) * unit.Y;

    /// <summary>A wall's layer, thickness, centerline and full (untrimmed) faces.</summary>
    private sealed class WallItem
    {
        public string Layer { get; }
        public double Thickness { get; }
        public LineSegment2D CenterLine { get; }
        public LineSegment2D FullFaceA { get; }
        public LineSegment2D FullFaceB { get; }

        public WallItem(WallModel wall, WallFacePair pair)
        {
            Layer = wall.SourceLayer;
            Thickness = wall.ThicknessMm;
            CenterLine = wall.CenterLine!;
            FullFaceA = pair.FaceA;
            FullFaceB = pair.FaceB;
        }
    }
}
