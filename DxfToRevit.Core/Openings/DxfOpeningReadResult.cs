namespace DxfToRevit.Core.Openings;

/// <summary>Result of <see cref="DxfOpeningReader.ReadMarkers"/>.</summary>
/// <param name="Markers">Usable opening markers.</param>
/// <param name="Issues">Markers that could not be read (invalid block content or orientation).</param>
public sealed record DxfOpeningReadResult(IReadOnlyList<OpeningMarker> Markers, IReadOnlyList<OpeningIssue> Issues);
