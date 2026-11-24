namespace DxfToRevit.Core.Walls;

/// <summary>
/// Outcome of creating a single wall in a host model.
/// </summary>
public sealed class WallCreationResult
{
    /// <summary>Identifier of the <see cref="WallCreationRequest"/> this result belongs to.</summary>
    public string RequestId { get; }

    /// <summary><c>true</c> when the host created the wall.</summary>
    public bool Success { get; }

    /// <summary>Host element identifier (Revit element id or in-memory id); <c>null</c> on failure.</summary>
    public string? HostElementId { get; }

    /// <summary>Error message on failure, or an optional informational note on success.</summary>
    public string? Message { get; }

    /// <summary>Creates a result.</summary>
    /// <param name="requestId">Identifier of the originating request.</param>
    /// <param name="success">Whether the wall was created.</param>
    /// <param name="hostElementId">Host element identifier, when created.</param>
    /// <param name="message">Error message or informational note.</param>
    /// <exception cref="ArgumentException"><paramref name="requestId"/> is empty.</exception>
    public WallCreationResult(
        string requestId,
        bool success,
        string? hostElementId = null,
        string? message = null)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request id cannot be empty.", nameof(requestId));

        RequestId = requestId;
        Success = success;
        HostElementId = hostElementId;
        Message = message;
    }

    /// <summary>Creates a successful result.</summary>
    /// <param name="requestId">Identifier of the originating request.</param>
    /// <param name="hostElementId">Identifier of the created host element.</param>
    public static WallCreationResult Ok(string requestId, string hostElementId) =>
        new(requestId, true, hostElementId);

    /// <summary>Creates a failed result.</summary>
    /// <param name="requestId">Identifier of the originating request.</param>
    /// <param name="message">Reason for the failure.</param>
    public static WallCreationResult Fail(string requestId, string message) =>
        new(requestId, false, message: message);
}
