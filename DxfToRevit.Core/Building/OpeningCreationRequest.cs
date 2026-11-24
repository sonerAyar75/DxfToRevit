using DxfToRevit.Core.Openings;
using netDxf;

namespace DxfToRevit.Core.Building;

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

    /// <summary>Swing of hinged openings; <c>null</c> otherwise.</summary>
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
    /// <param name="handing">Swing, for hinged openings.</param>
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

/// <summary>Outcome of creating a single opening in a host model.</summary>
public sealed class OpeningCreationResult
{
    /// <summary>Identifier of the originating request.</summary>
    public string RequestId { get; }

    /// <summary><c>true</c> when the host created the opening.</summary>
    public bool Success { get; }

    /// <summary>Host element identifier; <c>null</c> on failure.</summary>
    public string? HostElementId { get; }

    /// <summary>Error message on failure, or an optional note on success.</summary>
    public string? Message { get; }

    /// <summary>Creates a result.</summary>
    /// <param name="requestId">Originating request id.</param>
    /// <param name="success">Whether the opening was created.</param>
    /// <param name="hostElementId">Host element id, when created.</param>
    /// <param name="message">Error message or note.</param>
    /// <exception cref="ArgumentException"><paramref name="requestId"/> is empty.</exception>
    public OpeningCreationResult(string requestId, bool success, string? hostElementId = null, string? message = null)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request id cannot be empty.", nameof(requestId));

        RequestId = requestId;
        Success = success;
        HostElementId = hostElementId;
        Message = message;
    }

    /// <summary>Creates a successful result.</summary>
    /// <param name="requestId">Originating request id.</param>
    /// <param name="hostElementId">Created element id.</param>
    public static OpeningCreationResult Ok(string requestId, string hostElementId) => new(requestId, true, hostElementId);

    /// <summary>Creates a failed result.</summary>
    /// <param name="requestId">Originating request id.</param>
    /// <param name="message">Reason.</param>
    public static OpeningCreationResult Fail(string requestId, string message) => new(requestId, false, message: message);
}

/// <summary>
/// Host-independent contract for creating openings in existing walls.
/// </summary>
/// <remarks>
/// Implementations: <see cref="InMemory.InMemoryOpeningCreator"/> (tests) and
/// <c>DxfToRevit.Revit.RevitOpeningCreator</c> (Revit doors, windows and wall openings).
/// </remarks>
public interface IOpeningCreator
{
    /// <summary>Creates one opening in the given host wall.</summary>
    /// <param name="request">Opening request.</param>
    /// <param name="hostWallElementId">Host element id of the wall created for <see cref="OpeningCreationRequest.HostWallRequestId"/>.</param>
    /// <returns>Success with the element id, or a failure message.</returns>
    OpeningCreationResult CreateOpening(OpeningCreationRequest request, string hostWallElementId);
}

/// <summary>
/// Business rules an opening request must satisfy before it is sent to a host: minimum width and
/// height, a non-negative sill, and a Revit type for hosted families.
/// </summary>
public sealed class OpeningCreationValidator
{
    /// <summary>Minimum opening width in millimetres.</summary>
    public double MinWidthMm { get; }

    /// <summary>Minimum opening height in millimetres.</summary>
    public double MinHeightMm { get; }

    /// <summary>Creates the validator.</summary>
    /// <param name="minWidthMm">Minimum width (mm).</param>
    /// <param name="minHeightMm">Minimum height (mm).</param>
    /// <exception cref="ArgumentOutOfRangeException">A limit is negative.</exception>
    public OpeningCreationValidator(double minWidthMm = 100, double minHeightMm = 100)
    {
        if (minWidthMm < 0) throw new ArgumentOutOfRangeException(nameof(minWidthMm));
        if (minHeightMm < 0) throw new ArgumentOutOfRangeException(nameof(minHeightMm));
        MinWidthMm = minWidthMm;
        MinHeightMm = minHeightMm;
    }

    /// <summary>Checks a request.</summary>
    /// <param name="request">Request to check.</param>
    /// <returns><c>null</c> when valid; otherwise an error message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    public string? Validate(OpeningCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.WidthMm < MinWidthMm)
            return $"Opening width must be at least {MinWidthMm} mm (got {request.WidthMm:F0}).";
        if (request.HeightMm < MinHeightMm)
            return $"Opening height must be at least {MinHeightMm} mm (got {request.HeightMm:F0}).";
        if (request.SillHeightMm < 0)
            return "Sill height cannot be negative.";
        if (request.CreationMode == OpeningCreationMode.HostedFamily && request.TypeKey is null)
        {
            string code = string.IsNullOrEmpty(request.TypeCode) ? "(none)" : request.TypeCode;
            return $"No Revit type is mapped for {request.Category} type code {code}. " +
                   "Add it to the opening type table.";
        }

        return null;
    }
}
