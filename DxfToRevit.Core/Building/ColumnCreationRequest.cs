using netDxf;

namespace DxfToRevit.Core.Building;

/// <summary>
/// Request to create one column: the section outline in millimetres plus the type, placement
/// and plan rotation the host should use.
/// </summary>
public sealed class ColumnCreationRequest
{
    /// <summary>Request identifier (the source column id, e.g. <c>C001</c>).</summary>
    public string Id { get; }

    /// <summary>Section vertices in millimetres (world coordinates); the closing vertex is not repeated.</summary>
    public IReadOnlyList<Vector2> OutlineMm { get; }

    /// <summary>Area centroid of the section in millimetres; the host places the column here.</summary>
    public Vector2 CentroidMm { get; }

    /// <summary>Type description (family key, dimensions, structural flag, section kind).</summary>
    public ColumnTypeProperties Type { get; }

    /// <summary>Level and height placement.</summary>
    public ElementPlacement Placement { get; }

    /// <summary>Source DXF layer, kept for traceability.</summary>
    public string? SourceLayer { get; }

    /// <summary>Source DXF entity handle, kept for traceability.</summary>
    public string? SourceHandle { get; }

    /// <summary>
    /// Plan rotation in radians, counter-clockwise from the world X axis. For rectangular sections
    /// the width (<see cref="ColumnTypeProperties.WidthMm"/>) runs along this direction; axis-aligned
    /// columns use 0.
    /// </summary>
    public double RotationRadians { get; }

    /// <summary>Creates a column request.</summary>
    /// <param name="id">Request identifier.</param>
    /// <param name="outlineMm">Section vertices in millimetres (at least three).</param>
    /// <param name="centroidMm">Placement point (section centroid) in millimetres.</param>
    /// <param name="type">Type description; defaults to a rectangular column type.</param>
    /// <param name="placement">Placement; defaults to the host default level and height.</param>
    /// <param name="sourceLayer">Source DXF layer.</param>
    /// <param name="sourceHandle">Source DXF entity handle.</param>
    /// <param name="rotationRadians">Plan rotation in radians; must be finite.</param>
    /// <exception cref="ArgumentException">The id is empty or the outline has fewer than three vertices.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="outlineMm"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="rotationRadians"/> is not finite.</exception>
    public ColumnCreationRequest(
        string id,
        IReadOnlyList<Vector2> outlineMm,
        Vector2 centroidMm,
        ColumnTypeProperties? type = null,
        ElementPlacement? placement = null,
        string? sourceLayer = null,
        string? sourceHandle = null,
        double rotationRadians = 0)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be empty.", nameof(id));
        ArgumentNullException.ThrowIfNull(outlineMm);
        if (outlineMm.Count < 3)
            throw new ArgumentException("Outline needs at least 3 vertices.", nameof(outlineMm));
        if (!double.IsFinite(rotationRadians))
            throw new ArgumentOutOfRangeException(nameof(rotationRadians));

        Id = id;
        OutlineMm = outlineMm.ToList().AsReadOnly();
        CentroidMm = centroidMm;
        Type = type ?? new ColumnTypeProperties();
        Placement = placement ?? new ElementPlacement();
        SourceLayer = sourceLayer;
        SourceHandle = sourceHandle;
        RotationRadians = rotationRadians;
    }
}
