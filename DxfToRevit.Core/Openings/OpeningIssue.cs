using netDxf;

namespace DxfToRevit.Core.Openings;

/// <summary>Severity of an <see cref="OpeningIssue"/>.</summary>
public enum OpeningIssueSeverity
{
    /// <summary>The opening is created; the user should check it.</summary>
    Warning,

    /// <summary>The opening is not created.</summary>
    Error
}

/// <summary>Kind of problem found while reading or interpreting an opening.</summary>
public enum OpeningIssueCode
{
    /// <summary>The marker block has no closed 4-vertex rectangle, or an unsupported orientation.</summary>
    InvalidMarker,

    /// <summary>The marker does not lie on any wall.</summary>
    NoHostWall,

    /// <summary>The marker lies on more than one wall.</summary>
    MultipleWalls,

    /// <summary>The marker extends beyond the end of its wall.</summary>
    OutsideWall,

    /// <summary>Two markers on the same wall overlap.</summary>
    Overlapping,

    /// <summary>An expected attribute is missing; a default was used.</summary>
    MissingAttribute,

    /// <summary>An attribute is not a valid number; a default was used.</summary>
    InvalidAttribute,

    /// <summary>The marker depth differs from the wall thickness.</summary>
    DepthMismatch,

    /// <summary>
    /// The swing of a door or window marker is unclear: no circle or more than one (left as
    /// Revit places it), a circle on the wall axis, or a circle in the middle of the opening.
    /// </summary>
    MissingHinge,

    /// <summary>A gap in a wall has no marker; it was left open.</summary>
    UnmarkedGap
}

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
