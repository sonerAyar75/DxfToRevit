using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Models;
using netDxf;

namespace DxfToRevit.Core.Openings;

/// <summary>
/// Reports wall gaps that were left open because no opening marker covers them, so a forgotten
/// door or window is noticed. Gaps that are explained by other elements are not reported: a gap
/// whose middle lies inside a column, or inside another (crossing) wall, is part of the structure.
/// </summary>
public sealed class UnmarkedGapReporter
{
    /// <summary>Positional tolerance in millimetres.</summary>
    public double ToleranceMm { get; init; } = GeometryTolerance.PointDistanceMm;

    /// <summary>Creates a warning for every open gap that is not occupied by a column or a crossing wall.</summary>
    /// <param name="openGaps">Open gaps, as centerline segments across each gap.</param>
    /// <param name="walls">Wall models (centerline pieces with thickness).</param>
    /// <param name="columns">Columns read from the DXF.</param>
    /// <returns>One <see cref="OpeningIssueCode.UnmarkedGap"/> warning per unexplained gap.</returns>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public IReadOnlyList<OpeningIssue> Report(
        IReadOnlyList<LineSegment2D> openGaps,
        IReadOnlyList<WallModel> walls,
        IReadOnlyList<ColumnModel> columns)
    {
        ArgumentNullException.ThrowIfNull(openGaps);
        ArgumentNullException.ThrowIfNull(walls);
        ArgumentNullException.ThrowIfNull(columns);

        var issues = new List<OpeningIssue>();
        foreach (LineSegment2D gap in openGaps)
        {
            Vector2 middle = gap.MidPoint;
            bool inColumn = columns.Any(c => PolygonMetrics2D.ContainsPoint(c.Outline, middle, ToleranceMm));
            bool inCrossingWall = walls.Any(w =>
                w.CenterLine is not null &&
                !w.CenterLine.IsParallelTo(gap) &&
                PolygonMetrics2D.DistanceToSegment(middle, w.CenterLine.Start, w.CenterLine.End)
                    <= w.ThicknessMm / 2.0 + ToleranceMm);

            if (inColumn || inCrossingWall)
                continue;

            issues.Add(OpeningIssue.Warning(OpeningIssueCode.UnmarkedGap, null, middle,
                $"A {gap.Length:0} mm gap in the wall has no opening marker; it was left open."));
        }

        return issues;
    }
}
