using netDxf;

namespace DxfToRevit.Core.Geometry;

/// <summary>
/// Metrics for closed 2D polygons: bounding box, area, area-weighted centroid and rectangle
/// detection (axis-aligned and rotated). Polygons are given without a repeated closing vertex.
/// </summary>
public static class PolygonMetrics2D
{
    /// <summary>Computes the axis-aligned bounding box of the outline.</summary>
    /// <param name="outline">Polygon vertices.</param>
    /// <param name="minX">Smallest X.</param>
    /// <param name="minY">Smallest Y.</param>
    /// <param name="maxX">Largest X.</param>
    /// <param name="maxY">Largest Y.</param>
    /// <returns><c>false</c> when the outline is <c>null</c> or empty.</returns>
    public static bool TryGetAxisAlignedBounds(
        IReadOnlyList<Vector2> outline,
        out double minX,
        out double minY,
        out double maxX,
        out double maxY)
    {
        minX = minY = maxX = maxY = 0;
        if (outline is null || outline.Count < 1)
            return false;

        minX = maxX = outline[0].X;
        minY = maxY = outline[0].Y;
        for (int i = 1; i < outline.Count; i++)
        {
            Vector2 p = outline[i];
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;
        }

        return true;
    }

    /// <summary>
    /// Width (X extent) and depth (Y extent) of the axis-aligned bounding box.
    /// </summary>
    /// <param name="outline">Polygon vertices.</param>
    /// <param name="widthMm">Bounding box width.</param>
    /// <param name="depthMm">Bounding box depth.</param>
    /// <returns><c>true</c> when both extents are positive.</returns>
    public static bool TryGetWidthDepth(
        IReadOnlyList<Vector2> outline,
        out double widthMm,
        out double depthMm)
    {
        widthMm = depthMm = 0;
        if (!TryGetAxisAlignedBounds(outline, out double minX, out double minY, out double maxX, out double maxY))
            return false;

        widthMm = maxX - minX;
        depthMm = maxY - minY;
        return widthMm > 0 && depthMm > 0;
    }

    /// <summary>
    /// Signed area by the shoelace formula: positive for counter-clockwise vertex order,
    /// negative for clockwise. Returns 0 for fewer than three vertices.
    /// </summary>
    /// <param name="outline">Polygon vertices.</param>
    public static double SignedArea(IReadOnlyList<Vector2> outline)
    {
        if (outline is null || outline.Count < 3)
            return 0;

        double sum = 0;
        int n = outline.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = outline[i];
            Vector2 b = outline[(i + 1) % n];
            sum += a.X * b.Y - b.X * a.Y;
        }

        return 0.5 * sum;
    }

    /// <summary>Polygon area regardless of vertex order.</summary>
    /// <param name="outline">Polygon vertices.</param>
    public static double AbsoluteArea(IReadOnlyList<Vector2> outline) =>
        Math.Abs(SignedArea(outline));

    /// <summary>
    /// Area-weighted centroid of the polygon. For a degenerate (zero-area) polygon the vertex
    /// average is returned instead.
    /// </summary>
    /// <param name="outline">Polygon vertices.</param>
    /// <param name="centroid">Computed centroid.</param>
    /// <returns><c>false</c> when the outline has fewer than three vertices.</returns>
    public static bool TryGetAreaCentroid(
        IReadOnlyList<Vector2> outline,
        out Vector2 centroid)
    {
        centroid = default;
        if (outline is null || outline.Count < 3)
            return false;

        double a = SignedArea(outline);
        if (Math.Abs(a) < 1e-12)
        {
            double sx = 0, sy = 0;
            foreach (Vector2 p in outline)
            {
                sx += p.X;
                sy += p.Y;
            }

            centroid = new Vector2(sx / outline.Count, sy / outline.Count);
            return true;
        }

        double cx = 0, cy = 0;
        int n = outline.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 p0 = outline[i];
            Vector2 p1 = outline[(i + 1) % n];
            double cross = p0.X * p1.Y - p1.X * p0.Y;
            cx += (p0.X + p1.X) * cross;
            cy += (p0.Y + p1.Y) * cross;
        }

        double inv = 1.0 / (6.0 * a);
        centroid = new Vector2(cx * inv, cy * inv);
        return true;
    }

    /// <summary>
    /// Detects an axis-aligned rectangle: exactly four vertices, a polygon area close to the
    /// bounding box area, and a vertex near every bounding box corner. Anything else is not
    /// considered rectangular here (see <see cref="TryGetOrientedRectangle"/> for rotated ones).
    /// </summary>
    /// <param name="outline">Polygon vertices.</param>
    /// <param name="areaRatioTolerance">Allowed relative difference between polygon and box area.</param>
    /// <param name="cornerToleranceMm">Maximum distance from a box corner to the nearest vertex.</param>
    public static bool IsApproximatelyRectangular(
        IReadOnlyList<Vector2> outline,
        double areaRatioTolerance = 0.08,
        double cornerToleranceMm = 2.0)
    {
        if (outline is null || outline.Count != 4)
            return false;

        if (!TryGetWidthDepth(outline, out double w, out double d))
            return false;

        double boxArea = w * d;
        if (boxArea < 1e-6)
            return false;

        double polyArea = AbsoluteArea(outline);
        if (Math.Abs(polyArea - boxArea) / boxArea > areaRatioTolerance)
            return false;

        if (!TryGetAxisAlignedBounds(outline, out double minX, out double minY, out double maxX, out double maxY))
            return false;

        // Every bounding box corner must have an outline vertex close to it.
        var corners = new[]
        {
            new Vector2(minX, minY),
            new Vector2(maxX, minY),
            new Vector2(maxX, maxY),
            new Vector2(minX, maxY)
        };

        foreach (Vector2 corner in corners)
        {
            bool hit = false;
            foreach (Vector2 p in outline)
            {
                if (Vector2.Distance(p, corner) <= cornerToleranceMm)
                {
                    hit = true;
                    break;
                }
            }

            if (!hit)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Detects a rectangle at any rotation: four vertices, every corner ≈ 90° and opposite sides
    /// of equal length. <paramref name="widthMm"/> is the side along the rotated local X axis and
    /// <paramref name="depthMm"/> the side along local Y. <paramref name="rotationRadians"/> is
    /// normalised to (−π/4, π/4], so an axis-aligned rectangle gets angle 0 with width = X extent
    /// and depth = Y extent, independent of vertex order or winding.
    /// </summary>
    /// <param name="outline">Polygon vertices.</param>
    /// <param name="center">Rectangle centre (vertex average).</param>
    /// <param name="widthMm">Side length along the local X axis.</param>
    /// <param name="depthMm">Side length along the local Y axis.</param>
    /// <param name="rotationRadians">Rotation of the local X axis from world X, counter-clockwise.</param>
    /// <param name="angleToleranceDegrees">Allowed deviation of each corner from 90°.</param>
    /// <param name="sideToleranceMm">Allowed length difference between opposite sides.</param>
    /// <returns><c>true</c> when the outline is a rectangle within the tolerances.</returns>
    public static bool TryGetOrientedRectangle(
        IReadOnlyList<Vector2> outline,
        out Vector2 center,
        out double widthMm,
        out double depthMm,
        out double rotationRadians,
        double angleToleranceDegrees = 1.0,
        double sideToleranceMm = 2.0)
    {
        center = default;
        widthMm = depthMm = rotationRadians = 0;

        if (outline is null || outline.Count != 4)
            return false;

        var edges = new Vector2[4];
        var lengths = new double[4];
        for (int i = 0; i < 4; i++)
        {
            edges[i] = outline[(i + 1) % 4] - outline[i];
            lengths[i] = edges[i].Modulus();
            if (lengths[i] < 1e-6)
                return false;
        }

        // Consecutive edges must be perpendicular: |cos θ| ≤ sin(tolerance).
        double maxCos = Math.Sin(angleToleranceDegrees * Math.PI / 180.0);
        for (int i = 0; i < 4; i++)
        {
            double cos = Vector2.DotProduct(edges[i], edges[(i + 1) % 4]) / (lengths[i] * lengths[(i + 1) % 4]);
            if (Math.Abs(cos) > maxCos)
                return false;
        }

        if (Math.Abs(lengths[0] - lengths[2]) > sideToleranceMm ||
            Math.Abs(lengths[1] - lengths[3]) > sideToleranceMm)
            return false;

        double w = (lengths[0] + lengths[2]) / 2.0;
        double d = (lengths[1] + lengths[3]) / 2.0;
        double angle = Math.Atan2(edges[0].Y, edges[0].X);

        // Normalise the angle to (−45°, 45°]; every 90° step swaps width and depth.
        const double quarter = Math.PI / 2.0;
        while (angle > quarter / 2.0 + 1e-12)
        {
            angle -= quarter;
            (w, d) = (d, w);
        }

        while (angle <= -quarter / 2.0 + 1e-12)
        {
            angle += quarter;
            (w, d) = (d, w);
        }

        if (Math.Abs(angle) < 1e-9)
            angle = 0;

        center = new Vector2(
            (outline[0].X + outline[1].X + outline[2].X + outline[3].X) / 4.0,
            (outline[0].Y + outline[1].Y + outline[2].Y + outline[3].Y) / 4.0);
        widthMm = w;
        depthMm = d;
        rotationRadians = angle;
        return true;
    }
}
