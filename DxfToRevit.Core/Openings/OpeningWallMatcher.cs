using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Models;
using netDxf;

namespace DxfToRevit.Core.Openings;

/// <summary>
/// Attaches an opening marker to the wall footprint it sits on. The marker must lie on exactly one
/// wall, be parallel to it and stay within its length; otherwise an error is returned instead of a
/// guess.
/// </summary>
/// <remarks>
/// A wall is a candidate when one side of the marker rectangle is parallel to the wall centerline
/// (within <see cref="ParallelToleranceDegrees"/>), the rectangle centre lies inside the wall strip
/// (half the thickness plus the tolerance from the centerline) and the rectangle overlaps the
/// centerline extent. The opening stretch is the rectangle projected onto the centerline.
/// </remarks>
public sealed class OpeningWallMatcher
{
    /// <summary>Maximum angle (degrees) between the marker and the wall.</summary>
    public double ParallelToleranceDegrees { get; init; } = 1.0;

    /// <summary>Positional tolerance in millimetres (wall strip and wall ends).</summary>
    public double ToleranceMm { get; init; } = GeometryTolerance.PointDistanceMm;

    /// <summary>Allowed difference (mm) between marker depth and wall thickness before a warning.</summary>
    public double DepthToleranceMm { get; init; } = 5.0;

    /// <summary>Finds the host wall of a marker.</summary>
    /// <param name="marker">Opening marker.</param>
    /// <param name="walls">Wall footprints produced by the pipeline.</param>
    /// <param name="placement">The placement when exactly one wall matches and the marker fits on it.</param>
    /// <param name="issues">Receives the error (no or several walls, outside the wall) or the depth warning.</param>
    /// <returns><c>true</c> when a placement was found.</returns>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public bool TryMatch(
        OpeningMarker marker,
        IReadOnlyList<WallSolid2D> walls,
        out OpeningPlacement? placement,
        ICollection<OpeningIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(marker);
        ArgumentNullException.ThrowIfNull(walls);
        ArgumentNullException.ThrowIfNull(issues);

        placement = null;
        var candidates = new List<(int Index, Projection P)>();
        for (int i = 0; i < walls.Count; i++)
        {
            if (TryProject(marker, walls[i], out Projection p))
                candidates.Add((i, p));
        }

        string name = marker.Definition.Name;
        if (candidates.Count == 0)
        {
            issues.Add(OpeningIssue.Error(OpeningIssueCode.NoHostWall, marker.Handle, marker.Center,
                $"{name}: the marker does not lie on a wall parallel to it."));
            return false;
        }

        if (candidates.Count > 1)
        {
            issues.Add(OpeningIssue.Error(OpeningIssueCode.MultipleWalls, marker.Handle, marker.Center,
                $"{name}: the marker lies on {candidates.Count} walls; place it on a single wall."));
            return false;
        }

        (int index, Projection proj) = candidates[0];
        WallSolid2D wall = walls[index];
        double length = wall.CenterLine.Length;

        if (proj.SMin < -ToleranceMm || proj.SMax > length + ToleranceMm)
        {
            issues.Add(OpeningIssue.Error(OpeningIssueCode.OutsideWall, marker.Handle, marker.Center,
                $"{name}: the marker extends beyond the end of its wall."));
            return false;
        }

        double sMin = Math.Max(0, proj.SMin);
        double sMax = Math.Min(length, proj.SMax);
        double depth = proj.TMax - proj.TMin;
        if (Math.Abs(depth - wall.ThicknessMm) > DepthToleranceMm)
        {
            issues.Add(OpeningIssue.Warning(OpeningIssueCode.DepthMismatch, marker.Handle, marker.Center,
                $"{name}: marker depth {depth:0} mm differs from the wall thickness {wall.ThicknessMm:0} mm; " +
                "the wall thickness is used."));
        }

        Vector2 center = wall.CenterLine.Start + proj.Direction * ((sMin + sMax) / 2.0);
        placement = new OpeningPlacement(index, center, proj.Direction, sMin, sMax, wall.ThicknessMm, depth);
        return true;
    }

    /// <summary>Marker rectangle in the wall frame: along-wall (s) and across-wall (t) extents.</summary>
    private readonly record struct Projection(Vector2 Direction, double SMin, double SMax, double TMin, double TMax);

    /// <summary>Projects the marker onto a wall; <c>false</c> when the wall is not a candidate.</summary>
    private bool TryProject(OpeningMarker marker, WallSolid2D wall, out Projection projection)
    {
        projection = default;
        LineSegment2D cl = wall.CenterLine;
        if (cl.IsDegenerate)
            return false;

        Vector2 u = cl.Direction / cl.Length;
        Vector2 n = new(-u.Y, u.X);

        if (!HasSideParallelTo(marker.Outline, u))
            return false;

        double sMin = double.MaxValue, sMax = double.MinValue, tMin = double.MaxValue, tMax = double.MinValue;
        foreach (Vector2 p in marker.Outline)
        {
            Vector2 d = p - cl.Start;
            double s = Vector2.DotProduct(d, u);
            double t = Vector2.DotProduct(d, n);
            sMin = Math.Min(sMin, s);
            sMax = Math.Max(sMax, s);
            tMin = Math.Min(tMin, t);
            tMax = Math.Max(tMax, t);
        }

        double centerT = (tMin + tMax) / 2.0;
        bool insideStrip = Math.Abs(centerT) <= wall.ThicknessMm / 2.0 + ToleranceMm;
        bool overlapsLength = sMax > ToleranceMm && sMin < cl.Length - ToleranceMm;
        if (!insideStrip || !overlapsLength)
            return false;

        projection = new Projection(u, sMin, sMax, tMin, tMax);
        return true;
    }

    /// <summary>Whether one of the first two rectangle sides is parallel to <paramref name="unit"/>.</summary>
    private bool HasSideParallelTo(IReadOnlyList<Vector2> outline, Vector2 unit)
    {
        double maxSin = Math.Sin(ParallelToleranceDegrees * Math.PI / 180.0);
        for (int i = 0; i < 2; i++)
        {
            Vector2 side = outline[i + 1] - outline[i];
            double length = side.Modulus();
            if (length < 1e-9)
                continue;
            if (Math.Abs(Vector2.CrossProduct(side / length, unit)) <= maxSin)
                return true;
        }

        return false;
    }
}
