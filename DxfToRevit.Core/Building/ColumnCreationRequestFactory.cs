using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Models;

namespace DxfToRevit.Core.Building;

/// <summary>
/// Converts <see cref="ColumnModel"/> instances read from the DXF into
/// <see cref="ColumnCreationRequest"/>s, classifying each section as rectangular, circular or freeform.
/// </summary>
/// <remarks>
/// Classification order:
/// <list type="number">
/// <item><description>Circle (the model carries a radius) → <see cref="ColumnSectionKind.Circular"/>,
/// or a square of the same outside size when <see cref="ConvertCircularToSquare"/> is set.</description></item>
/// <item><description>Axis-aligned rectangle → <see cref="ColumnSectionKind.Rectangular"/>, rotation 0.</description></item>
/// <item><description>Rotated rectangle (when <see cref="RecognizeRotatedRectangles"/> is set) →
/// <see cref="ColumnSectionKind.Rectangular"/> with the true side lengths and a plan rotation.</description></item>
/// <item><description>Anything else → <see cref="ColumnSectionKind.Freeform"/>.</description></item>
/// </list>
/// </remarks>
public sealed class ColumnCreationRequestFactory
{
    private readonly ImportSessionOptions _session;

    /// <summary>Type key assigned to freeform sections (default <c>"Freeform Column"</c>, or <see cref="ColumnTypeKeys.FreeformFamily"/>).</summary>
    public string FreeformColumnTypeKey { get; init; } = "Freeform Column";

    /// <summary>Type key assigned to circular sections (a family/type key or a <see cref="ColumnTypeKeys"/> value).</summary>
    public string CircularColumnTypeKey { get; init; } = "Circular Column";

    /// <summary>When <c>true</c>, circular DXF columns are represented by square/rectangular columns.</summary>
    public bool ConvertCircularToSquare { get; init; }

    /// <summary>Family/type key used when circular columns are converted to squares; falls back to the session default.</summary>
    public string? SquareColumnTypeKey { get; init; }

    /// <summary>
    /// When <c>true</c> (default), rotated rectangles become rectangular columns with a plan rotation;
    /// when <c>false</c>, they are treated as freeform sections.
    /// </summary>
    public bool RecognizeRotatedRectangles { get; init; } = true;

    /// <summary>Creates the factory.</summary>
    /// <param name="session">Import policy; default options are used when <c>null</c>.</param>
    public ColumnCreationRequestFactory(ImportSessionOptions? session = null)
    {
        _session = session ?? new ImportSessionOptions();
    }

    /// <summary>Import policy used for type keys, structural flag and placement.</summary>
    public ImportSessionOptions Session => _session;

    /// <summary>Creates one request per column model.</summary>
    /// <param name="columns">Columns read from the DXF.</param>
    /// <returns>Column requests in the same order as <paramref name="columns"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="columns"/> is <c>null</c>.</exception>
    public IReadOnlyList<ColumnCreationRequest> FromColumnModels(IEnumerable<ColumnModel> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);

        var list = columns is ICollection<ColumnModel> collection
            ? new List<ColumnCreationRequest>(collection.Count)
            : new List<ColumnCreationRequest>();

        foreach (ColumnModel col in columns)
        {
            double? width = null;
            double? depth = null;
            if (PolygonMetrics2D.TryGetWidthDepth(col.Outline, out double w, out double d))
            {
                width = w;
                depth = d;
            }

            ColumnSectionKind kind;
            string typeKey;
            double? radius = col.RadiusMm;
            double rotation = 0;

            if (radius is double r && r > 0)
            {
                // Preserve the DXF diameter. In square-fallback mode, use it as both
                // width and depth so the square's outside dimension matches the circle.
                kind = ConvertCircularToSquare
                    ? ColumnSectionKind.Rectangular
                    : ColumnSectionKind.Circular;
                typeKey = ConvertCircularToSquare
                    ? (SquareColumnTypeKey ?? _session.DefaultColumnTypeKey)
                    : CircularColumnTypeKey;
                width = 2 * r;
                depth = 2 * r;
                if (ConvertCircularToSquare)
                    radius = null;
            }
            else if (PolygonMetrics2D.IsApproximatelyRectangular(col.Outline))
            {
                kind = ColumnSectionKind.Rectangular;
                typeKey = _session.DefaultColumnTypeKey;
            }
            else if (RecognizeRotatedRectangles &&
                     PolygonMetrics2D.TryGetOrientedRectangle(
                         col.Outline, out _, out double rw, out double rd, out double angle))
            {
                // Rotated rectangle: use the true side lengths and a plan rotation so the host
                // can place a rectangular family instead of a freeform solid.
                kind = ColumnSectionKind.Rectangular;
                typeKey = _session.DefaultColumnTypeKey;
                width = rw;
                depth = rd;
                rotation = angle;
            }
            else
            {
                kind = ColumnSectionKind.Freeform;
                typeKey = FreeformColumnTypeKey;
            }

            var type = new ColumnTypeProperties(
                typeKey: typeKey,
                widthMm: width,
                depthMm: depth,
                isStructural: _session.ColumnsAreStructural,
                sectionKind: kind,
                radiusMm: radius);

            list.Add(new ColumnCreationRequest(
                id: col.Id,
                outlineMm: col.Outline,
                centroidMm: col.Centroid,
                type: type,
                placement: _session.CreatePlacement(),
                sourceLayer: col.SourceLayer,
                sourceHandle: col.Handle,
                rotationRadians: rotation));
        }

        return list;
    }
}
