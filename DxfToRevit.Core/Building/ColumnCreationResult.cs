namespace DxfToRevit.Core.Building;

/// <summary>
/// Outcome of creating a single column in a host model.
/// </summary>
/// <remarks>
/// A successful result may still carry a <see cref="Message"/>; this is used as an informational
/// note, for example when a column had to be created as a DirectShape instead of a family instance.
/// </remarks>
public sealed class ColumnCreationResult
{
    /// <summary>Identifier of the <see cref="ColumnCreationRequest"/> this result belongs to.</summary>
    public string RequestId { get; }

    /// <summary><c>true</c> when the host created the column.</summary>
    public bool Success { get; }

    /// <summary>Host element identifier (for Revit, the element id as text); <c>null</c> on failure.</summary>
    public string? HostElementId { get; }

    /// <summary>Error message on failure, or an optional informational note on success.</summary>
    public string? Message { get; }

    /// <summary>Creates a result.</summary>
    /// <param name="requestId">Identifier of the originating request.</param>
    /// <param name="success">Whether the column was created.</param>
    /// <param name="hostElementId">Host element identifier, when created.</param>
    /// <param name="message">Error message or informational note.</param>
    /// <exception cref="ArgumentException"><paramref name="requestId"/> is empty.</exception>
    public ColumnCreationResult(
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

    /// <summary>Creates a successful result without a note.</summary>
    /// <param name="requestId">Identifier of the originating request.</param>
    /// <param name="hostElementId">Identifier of the created host element.</param>
    public static ColumnCreationResult Ok(string requestId, string hostElementId) =>
        new(requestId, true, hostElementId);

    /// <summary>Creates a failed result.</summary>
    /// <param name="requestId">Identifier of the originating request.</param>
    /// <param name="message">Reason for the failure.</param>
    public static ColumnCreationResult Fail(string requestId, string message) =>
        new(requestId, false, message: message);
}
