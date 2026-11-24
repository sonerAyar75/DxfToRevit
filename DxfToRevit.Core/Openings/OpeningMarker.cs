using netDxf;

namespace DxfToRevit.Core.Openings;

/// <summary>
/// A marker block read from the DXF, before it is attached to a wall: the opening rectangle in
/// world coordinates, the hinge point and the raw attribute values.
/// </summary>
public sealed class OpeningMarker
{
    /// <summary>DXF handle of the block reference.</summary>
    public string Handle { get; }

    /// <summary>Block name; the true name for dynamic blocks (e.g. <c>door</c> for <c>*U26</c>).</summary>
    public string BlockName { get; }

    /// <summary>Layer of the block reference.</summary>
    public string Layer { get; }

    /// <summary>Kind of opening the marker stands for.</summary>
    public OpeningDefinition Definition { get; }

    /// <summary>The opening rectangle in millimetres (world coordinates, 4 vertices).</summary>
    public IReadOnlyList<Vector2> Outline { get; }

    /// <summary>Centre of the rectangle in millimetres.</summary>
    public Vector2 Center { get; }

    /// <summary>Hinge point in millimetres: the hinge circle centre, or the insertion point.</summary>
    public Vector2 HingePoint { get; }

    /// <summary>Whether <see cref="HingePoint"/> comes from a circle in the block.</summary>
    public bool HasHingeCircle { get; }

    /// <summary>Attribute values by tag (case-insensitive), as written in the DXF.</summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }

    /// <summary>Creates a marker.</summary>
    /// <param name="handle">Block reference handle.</param>
    /// <param name="blockName">Block (true) name.</param>
    /// <param name="layer">Layer name.</param>
    /// <param name="definition">Opening kind.</param>
    /// <param name="outline">Rectangle vertices in millimetres (exactly 4).</param>
    /// <param name="hingePoint">Hinge point in millimetres.</param>
    /// <param name="hasHingeCircle">Whether the hinge point comes from a circle.</param>
    /// <param name="attributes">Attribute values by tag.</param>
    /// <exception cref="ArgumentException">The handle is empty or the outline does not have 4 vertices.</exception>
    /// <exception cref="ArgumentNullException">A reference argument is <c>null</c>.</exception>
    public OpeningMarker(
        string handle,
        string blockName,
        string layer,
        OpeningDefinition definition,
        IReadOnlyList<Vector2> outline,
        Vector2 hingePoint,
        bool hasHingeCircle,
        IReadOnlyDictionary<string, string> attributes)
    {
        if (string.IsNullOrWhiteSpace(handle))
            throw new ArgumentException("Handle cannot be empty.", nameof(handle));
        ArgumentNullException.ThrowIfNull(blockName);
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(outline);
        ArgumentNullException.ThrowIfNull(attributes);
        if (outline.Count != 4)
            throw new ArgumentException("A marker outline needs exactly 4 vertices.", nameof(outline));

        Handle = handle;
        BlockName = blockName;
        Layer = layer;
        Definition = definition;
        Outline = outline.ToList().AsReadOnly();
        Center = new Vector2(Outline.Average(p => p.X), Outline.Average(p => p.Y));
        HingePoint = hingePoint;
        HasHingeCircle = hasHingeCircle;
        Attributes = new Dictionary<string, string>(attributes, StringComparer.OrdinalIgnoreCase);
    }
}
