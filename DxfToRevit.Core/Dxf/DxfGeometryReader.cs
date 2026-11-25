using DxfToRevit.Core.Geometry;
using netDxf.Entities;
using netDxf;

namespace DxfToRevit.Core.Dxf;

/// <summary>
/// Converts the LINE entities of a DXF document into <see cref="DxfLineModel"/>s, keeping the
/// layer name and entity handle of each line. Z coordinates are dropped (plan view).
/// </summary>
public sealed class DxfGeometryReader
{
    /// <summary>Reads every LINE entity of the document.</summary>
    /// <param name="document">Loaded DXF document.</param>
    /// <returns>Raw, unclassified line models.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public IReadOnlyList<DxfLineModel> ReadLines(DxfDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var result = new List<DxfLineModel>();

        foreach (Line line in document.Entities.Lines)
        {
            var start = new Vector2(line.StartPoint.X, line.StartPoint.Y);
            var end = new Vector2(line.EndPoint.X, line.EndPoint.Y);
            var geometry = new LineSegment2D(start, end);

            result.Add(new DxfLineModel(
                geometry,
                line.Layer.Name,
                line.Handle));
        }

        return result;
    }
}
