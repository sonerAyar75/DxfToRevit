using DxfToRevit.Core.Geometry;
using netDxf.Entities;
using netDxf;

namespace DxfToRevit.Core.Columns;

/// <summary>
/// Reads column sections from COLUMN* layers (see <see cref="ColumnLayerRule"/>):
/// closed 2D polylines become polygon columns and circles become circular columns.
/// </summary>
/// <remarks>
/// A polyline counts as closed when its closed flag is set or when its first and last vertices are
/// within <see cref="ClosureToleranceMm"/>. Sections smaller than <see cref="MinAreaMm2"/> (or circles
/// smaller than <see cref="MinRadiusMm"/>) are ignored as drawing noise. Columns are numbered
/// <c>C001</c>, <c>C002</c>, ... — polylines first, then circles.
/// </remarks>
public sealed class DxfColumnReader
{
    private readonly ColumnLayerRule _layerRule;

    /// <summary>Creates the reader.</summary>
    /// <param name="layerRule">Column layer rule; the default rule when <c>null</c>.</param>
    public DxfColumnReader(ColumnLayerRule? layerRule = null)
    {
        _layerRule = layerRule ?? new ColumnLayerRule();
    }

    /// <summary>Maximum distance (mm) between the first and last vertex for an open polyline to count as closed.</summary>
    public double ClosureToleranceMm { get; init; } = 1.0;

    /// <summary>Minimum section area (mm²) for a column to be read.</summary>
    public double MinAreaMm2 { get; init; } = 100.0;

    /// <summary>Number of vertices used to approximate a circle's outline.</summary>
    public int CircleSegmentCount { get; init; } = 32;

    /// <summary>Minimum circle radius (mm) for a circular column to be read.</summary>
    public double MinRadiusMm { get; init; } = 10.0;

    /// <summary>Reads every column on COLUMN* layers of the document.</summary>
    /// <param name="document">Loaded DXF document.</param>
    /// <returns>Columns in reading order (polylines, then circles).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public IReadOnlyList<ColumnModel> ReadColumns(DxfDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var result = new List<ColumnModel>();
        int index = 0;

        foreach (Polyline2D poly in document.Entities.Polylines2D)
        {
            if (!_layerRule.IsColumnLayer(poly.Layer?.Name))
                continue;

            var raw = ExtractRawVertices(poly);
            if (!TryNormalizeClosedOutline(raw, poly.IsClosed, out List<Vector2> verts))
                continue;

            if (PolygonMetrics2D.AbsoluteArea(verts) < MinAreaMm2)
                continue;

            result.Add(new ColumnModel(
                $"C{++index:D3}",
                poly.Layer!.Name,
                poly.Handle,
                verts));
        }

        foreach (Circle circle in document.Entities.Circles)
        {
            if (!_layerRule.IsColumnLayer(circle.Layer?.Name))
                continue;

            double radius = circle.Radius;
            if (radius < MinRadiusMm)
                continue;

            double area = Math.PI * radius * radius;
            if (area < MinAreaMm2)
                continue;

            var center = new Vector2(circle.Center.X, circle.Center.Y);
            result.Add(ColumnModel.FromCircle(
                $"C{++index:D3}",
                circle.Layer!.Name,
                circle.Handle,
                center,
                radius,
                CircleSegmentCount));
        }

        return result;
    }

    /// <summary>Returns the polyline vertex positions (bulges are ignored).</summary>
    private static List<Vector2> ExtractRawVertices(Polyline2D poly)
    {
        var list = new List<Vector2>();
        foreach (Polyline2DVertex v in poly.Vertexes)
            list.Add(new Vector2(v.Position.X, v.Position.Y));
        return list;
    }

    /// <summary>
    /// Accepts the outline when it is flagged or geometrically closed, and removes trailing
    /// vertices that duplicate the first one.
    /// </summary>
    /// <param name="raw">Vertices as read from the polyline.</param>
    /// <param name="flaggedClosed">The polyline's closed flag.</param>
    /// <param name="normalized">Closed outline without a repeated closing vertex.</param>
    /// <returns><c>true</c> when a closed outline with at least three vertices remains.</returns>
    private bool TryNormalizeClosedOutline(
        List<Vector2> raw,
        bool flaggedClosed,
        out List<Vector2> normalized)
    {
        normalized = new List<Vector2>();
        if (raw.Count < 3)
            return false;

        bool geometricallyClosed = Vector2.Distance(raw[0], raw[^1]) <= ClosureToleranceMm;
        if (!flaggedClosed && !geometricallyClosed)
            return false;

        normalized.AddRange(raw);
        while (normalized.Count > 3 &&
               Vector2.Distance(normalized[0], normalized[^1]) <= ClosureToleranceMm)
        {
            normalized.RemoveAt(normalized.Count - 1);
        }

        return normalized.Count >= 3;
    }
}
