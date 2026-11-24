using netDxf;

namespace DxfToRevit.Core.Openings;

/// <summary>
/// A problem found with an opening marker or a wall gap, shown in the import summary.
/// Errors prevent the opening from being created; warnings do not.
/// </summary>
public sealed class OpeningIssue
{
    /// <summary>Severity.</summary>
    public OpeningIssueSeverity Severity { get; }

    /// <summary>Problem kind.</summary>
    public OpeningIssueCode Code { get; }

    /// <summary>DXF handle of the marker block reference; <c>null</c> for wall gaps.</summary>
    public string? MarkerHandle { get; }

    /// <summary>Location in millimetres (marker centre or gap middle), for finding it in the drawing.</summary>
    public Vector2 LocationMm { get; }

    /// <summary>Human-readable description.</summary>
    public string Message { get; }

    /// <summary>Creates an issue.</summary>
    /// <param name="severity">Severity.</param>
    /// <param name="code">Problem kind.</param>
    /// <param name="markerHandle">Marker handle, if any.</param>
    /// <param name="locationMm">Location in millimetres.</param>
    /// <param name="message">Description.</param>
    /// <exception cref="ArgumentException"><paramref name="message"/> is empty.</exception>
    public OpeningIssue(
        OpeningIssueSeverity severity,
        OpeningIssueCode code,
        string? markerHandle,
        Vector2 locationMm,
        string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty.", nameof(message));

        Severity = severity;
        Code = code;
        MarkerHandle = markerHandle;
        LocationMm = locationMm;
        Message = message;
    }

    /// <summary>Creates an error.</summary>
    public static OpeningIssue Error(OpeningIssueCode code, string? handle, Vector2 location, string message) =>
        new(OpeningIssueSeverity.Error, code, handle, location, message);

    /// <summary>Creates a warning.</summary>
    public static OpeningIssue Warning(OpeningIssueCode code, string? handle, Vector2 location, string message) =>
        new(OpeningIssueSeverity.Warning, code, handle, location, message);

    /// <inheritdoc />
    public override string ToString() =>
        $"{Severity} {Code} at ({LocationMm.X:0}, {LocationMm.Y:0}): {Message}";
}
