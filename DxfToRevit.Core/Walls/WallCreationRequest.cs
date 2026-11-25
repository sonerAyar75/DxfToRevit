using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Import;
using netDxf;

namespace DxfToRevit.Core.Walls;

/// <summary>
/// Request to create one straight wall: the centerline in millimetres plus the wall type and
/// vertical placement.
/// </summary>
public sealed class WallCreationRequest
{
    /// <summary>Request identifier (e.g. <c>W001</c>).</summary>
    public string Id { get; }

    /// <summary>Centerline start point in millimetres.</summary>
    public Vector2 StartMm { get; }

    /// <summary>Centerline end point in millimetres.</summary>
    public Vector2 EndMm { get; }

    /// <summary>Wall type description (type key, thickness, structural flag).</summary>
    public WallTypeProperties Type { get; }

    /// <summary>Level and height placement.</summary>
    public ElementPlacement Placement { get; }

    /// <summary>
    /// Convenience constructor that builds the <see cref="WallTypeProperties"/> and
    /// <see cref="ElementPlacement"/> from individual values.
    /// </summary>
    /// <param name="id">Request identifier.</param>
    /// <param name="startMm">Centerline start point in millimetres.</param>
    /// <param name="endMm">Centerline end point in millimetres.</param>
    /// <param name="thicknessMm">Wall thickness in millimetres.</param>
    /// <param name="levelName">Base level name, or <c>null</c> for the host default.</param>
    /// <param name="heightMm">Unconnected height in millimetres, or <c>null</c> for the host default.</param>
    /// <param name="wallTypeKey">Wall type key.</param>
    /// <param name="isStructural">Whether the wall is structural.</param>
    /// <param name="baseOffsetMm">Base offset in millimetres.</param>
    public WallCreationRequest(
        string id,
        Vector2 startMm,
        Vector2 endMm,
        double thicknessMm,
        string? levelName = null,
        double? heightMm = null,
        string wallTypeKey = "Basic Wall",
        bool isStructural = false,
        double baseOffsetMm = 0)
        : this(
            id,
            startMm,
            endMm,
            new WallTypeProperties(thicknessMm, wallTypeKey, isStructural),
            new ElementPlacement(levelName, baseOffsetMm, heightMm))
    {
    }

    /// <summary>Creates a wall request from a type description and placement.</summary>
    /// <param name="id">Request identifier.</param>
    /// <param name="startMm">Centerline start point in millimetres.</param>
    /// <param name="endMm">Centerline end point in millimetres.</param>
    /// <param name="type">Wall type description.</param>
    /// <param name="placement">Placement; defaults to the host default level and height.</param>
    /// <exception cref="ArgumentException"><paramref name="id"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
    public WallCreationRequest(
        string id,
        Vector2 startMm,
        Vector2 endMm,
        WallTypeProperties type,
        ElementPlacement? placement = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be empty.", nameof(id));
        ArgumentNullException.ThrowIfNull(type);

        Id = id;
        StartMm = startMm;
        EndMm = endMm;
        Type = type;
        Placement = placement ?? new ElementPlacement();
    }

    /// <summary>Wall thickness in millimetres (from <see cref="Type"/>).</summary>
    public double ThicknessMm => Type.ThicknessMm;

    /// <summary>Wall type key (from <see cref="Type"/>).</summary>
    public string WallTypeKey => Type.TypeKey;

    /// <summary>Base level name (from <see cref="Placement"/>).</summary>
    public string? LevelName => Placement.LevelName;

    /// <summary>Unconnected height in millimetres (from <see cref="Placement"/>).</summary>
    public double? HeightMm => Placement.HeightMm;

    /// <summary>Centerline as a segment.</summary>
    public LineSegment2D CenterLine => new(StartMm, EndMm);

    /// <summary>Centerline length in millimetres.</summary>
    public double LengthMm => CenterLine.Length;
}
