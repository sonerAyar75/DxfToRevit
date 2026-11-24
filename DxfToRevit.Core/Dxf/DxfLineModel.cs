using DxfToRevit.Core.Geometry;

namespace DxfToRevit.Core.Dxf;

/// <summary>
/// A raw line read from a DXF file, before layer classification.
/// </summary>
public sealed class DxfLineModel
{
    /// <summary>Line geometry in millimetres.</summary>
    public LineSegment2D Geometry { get; }

    /// <summary>DXF layer name.</summary>
    public string SourceLayer { get; }

    /// <summary>DXF entity handle.</summary>
    public string Handle { get; }

    /// <summary>Creates a raw line model.</summary>
    /// <param name="geometry">Line segment.</param>
    /// <param name="sourceLayer">Layer name.</param>
    /// <param name="handle">DXF entity handle.</param>
    /// <exception cref="ArgumentNullException"><paramref name="geometry"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The layer or handle is empty.</exception>
    public DxfLineModel(
        LineSegment2D geometry,
        string sourceLayer,
        string handle)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        if (string.IsNullOrWhiteSpace(sourceLayer))
            throw new ArgumentException("Source layer cannot be empty.", nameof(sourceLayer));

        if (string.IsNullOrWhiteSpace(handle))
            throw new ArgumentException("DXF handle cannot be empty.", nameof(handle));

        Geometry = geometry;
        SourceLayer = sourceLayer;
        Handle = handle;
    }
}
