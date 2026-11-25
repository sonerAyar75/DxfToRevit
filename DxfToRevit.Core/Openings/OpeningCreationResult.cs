namespace DxfToRevit.Core.Openings;

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
