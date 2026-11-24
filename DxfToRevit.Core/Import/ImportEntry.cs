namespace DxfToRevit.Core.Import;

/// <summary>Outcome of one creation request.</summary>
/// <param name="RequestId">Request id, for example <c>W001</c>.</param>
/// <param name="Success">Whether the element was created.</param>
/// <param name="ElementId">Id of the created element; <c>null</c> when it was not created.</param>
/// <param name="Message">Failure reason, or a note for a created element.</param>
public sealed record ImportEntry(string RequestId, bool Success, string? ElementId, string? Message);
