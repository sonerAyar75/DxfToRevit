using DxfToRevit.Core.Import;
using DxfToRevit.Core.Walls;
using netDxf;

namespace DxfToRevit.Core.Openings;

/// <summary>
/// Request to create one opening (door, window or empty opening) in a wall that has already been
/// created. Lengths are in millimetres.
/// </summary>
public sealed class OpeningCreationRequest
{
    /// <summary>Request id (the opening id, e.g. <c>D001</c>).</summary>
    public string Id { get; }

    /// <summary>Host category.</summary>
    public OpeningCategory Category { get; }

    /// <summary>How the host creates the opening.</summary>
    public OpeningCreationMode CreationMode { get; }

    /// <summary>
    /// Revit type key (<c>Family::Type</c> or a type/family name) for hosted families; <c>null</c>
    /// for wall openings, or when no type is mapped (then validation fails).
    /// </summary>
    public string? TypeKey { get; }

    /// <summary>Type code from the marker attribute (for messages).</summary>
    public string TypeCode { get; }

    /// <summary>Request id of the host wall (see <see cref="WallCreationRequestFactory.WallIdForIndex"/>).</summary>
    public string HostWallRequestId { get; }

    /// <summary>Opening centre on the host wall centerline.</summary>
    public Vector2 LocationMm { get; }

    /// <summary>Unit direction of the host wall centerline.</summary>
    public Vector2 WallDirection { get; }

    /// <summary>Opening width along the wall.</summary>
    public double WidthMm { get; }

    /// <summary>Opening height.</summary>
    public double HeightMm { get; }

    /// <summary>Sill height above the level.</summary>
    public double SillHeightMm { get; }

    /// <summary>Orientation of doors and windows read from the marker circles; <c>null</c> when unknown or not applicable.</summary>
    public OpeningHanding? Handing { get; }

    /// <summary>Level placement (the base level of the import).</summary>
    public ElementPlacement Placement { get; }

    /// <summary>DXF handle of the marker, for traceability.</summary>
    public string? SourceHandle { get; }

    /// <summary>Creates a request.</summary>
    /// <param name="id">Request id.</param>
    /// <param name="category">Host category.</param>
    /// <param name="creationMode">Creation mode.</param>
    /// <param name="typeKey">Revit type key, or <c>null</c>.</param>
    /// <param name="typeCode">Marker type code.</param>
    /// <param name="hostWallRequestId">Host wall request id.</param>
    /// <param name="locationMm">Centre on the wall centerline.</param>
    /// <param name="wallDirection">Unit wall direction.</param>
    /// <param name="widthMm">Width.</param>
    /// <param name="heightMm">Height.</param>
    /// <param name="sillHeightMm">Sill height.</param>
    /// <param name="handing">Orientation, for doors and windows.</param>
    /// <param name="placement">Level placement; host default when <c>null</c>.</param>
    /// <param name="sourceHandle">Marker handle.</param>
    /// <exception cref="ArgumentException">The id or host wall id is empty.</exception>
    public OpeningCreationRequest(
        string id,
        OpeningCategory category,
        OpeningCreationMode creationMode,
        string? typeKey,
        string typeCode,
        string hostWallRequestId,
        Vector2 locationMm,
        Vector2 wallDirection,
        double widthMm,
        double heightMm,
        double sillHeightMm,
        OpeningHanding? handing = null,
        ElementPlacement? placement = null,
        string? sourceHandle = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(hostWallRequestId))
            throw new ArgumentException("Host wall id cannot be empty.", nameof(hostWallRequestId));

        Id = id;
        Category = category;
        CreationMode = creationMode;
        TypeKey = string.IsNullOrWhiteSpace(typeKey) ? null : typeKey.Trim();
        TypeCode = typeCode ?? string.Empty;
        HostWallRequestId = hostWallRequestId;
        LocationMm = locationMm;
        WallDirection = wallDirection;
        WidthMm = widthMm;
        HeightMm = heightMm;
        SillHeightMm = sillHeightMm;
        Handing = handing;
        Placement = placement ?? new ElementPlacement();
        SourceHandle = sourceHandle;
    }
}
