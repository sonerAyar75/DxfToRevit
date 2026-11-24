using DxfToRevit.Core.Geometry;
using netDxf;

namespace DxfToRevit.Core.Columns;

/// <summary>
/// Column section read from a COLUMN* layer: a closed polyline or a circle, in millimetres.
/// </summary>
public sealed class ColumnModel
{
    /// <summary>Column identifier (e.g. <c>C001</c>).</summary>
    public string Id { get; }

    /// <summary>Source DXF layer name.</summary>
    public string SourceLayer { get; }

    /// <summary>Source DXF entity handle.</summary>
    public string Handle { get; }

    /// <summary>Section vertices in millimetres (a sampled polygon for circles); the closing vertex is not repeated.</summary>
    public IReadOnlyList<Vector2> Outline { get; }

    /// <summary>Area-weighted centroid of <see cref="Outline"/> (the circle centre for circular columns).</summary>
    public Vector2 Centroid { get; }

    /// <summary>
    /// Radius in millimetres for columns read from circles; <c>null</c> for polyline columns.
    /// </summary>
    public double? RadiusMm { get; }

    /// <summary>Creates a column model.</summary>
    /// <param name="id">Column identifier.</param>
    /// <param name="sourceLayer">Source layer name.</param>
    /// <param name="handle">Source entity handle.</param>
    /// <param name="outline">Section vertices (at least three).</param>
    /// <param name="radiusMm">Circle radius, for circular columns; must be positive when given.</param>
    /// <exception cref="ArgumentException">A text value is empty, the outline is too short or has no centroid.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="outline"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="radiusMm"/> is not positive.</exception>
    public ColumnModel(
        string id,
        string sourceLayer,
        string handle,
        IReadOnlyList<Vector2> outline,
        double? radiusMm = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(sourceLayer))
            throw new ArgumentException("Source layer cannot be empty.", nameof(sourceLayer));
        if (string.IsNullOrWhiteSpace(handle))
            throw new ArgumentException("Handle cannot be empty.", nameof(handle));

        ArgumentNullException.ThrowIfNull(outline);
        if (outline.Count < 3)
            throw new ArgumentException("Column outline needs at least 3 vertices.", nameof(outline));
        if (radiusMm is <= 0)
            throw new ArgumentOutOfRangeException(nameof(radiusMm));

        Id = id;
        SourceLayer = sourceLayer;
        Handle = handle;
        Outline = outline.ToList().AsReadOnly();
        RadiusMm = radiusMm;

        if (!PolygonMetrics2D.TryGetAreaCentroid(Outline, out Vector2 centroid))
            throw new ArgumentException("Could not compute outline centroid.", nameof(outline));
        Centroid = centroid;
    }

    /// <summary>
    /// Creates a circular column model whose outline is a regular polygon sampled on the circle.
    /// </summary>
    /// <param name="id">Column identifier.</param>
    /// <param name="sourceLayer">Source layer name.</param>
    /// <param name="handle">Source entity handle.</param>
    /// <param name="center">Circle centre in millimetres.</param>
    /// <param name="radiusMm">Circle radius in millimetres.</param>
    /// <param name="segmentCount">Number of outline vertices (at least 8).</param>
    /// <exception cref="ArgumentOutOfRangeException">The radius is not positive or the segment count is below 8.</exception>
    public static ColumnModel FromCircle(
        string id,
        string sourceLayer,
        string handle,
        Vector2 center,
        double radiusMm,
        int segmentCount = 32)
    {
        if (radiusMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(radiusMm));
        if (segmentCount < 8)
            throw new ArgumentOutOfRangeException(nameof(segmentCount));

        var outline = new List<Vector2>(segmentCount);
        for (int i = 0; i < segmentCount; i++)
        {
            double ang = 2.0 * Math.PI * i / segmentCount;
            outline.Add(new Vector2(
                center.X + radiusMm * Math.Cos(ang),
                center.Y + radiusMm * Math.Sin(ang)));
        }

        return new ColumnModel(id, sourceLayer, handle, outline, radiusMm);
    }
}
