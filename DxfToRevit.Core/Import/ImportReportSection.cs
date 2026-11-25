namespace DxfToRevit.Core.Import;

/// <summary>A titled list of lines a module adds to the import summary (for example marker warnings).</summary>
/// <param name="Title">Section title, without the count.</param>
/// <param name="Count">Number shown after the title.</param>
/// <param name="Lines">Lines of the section.</param>
/// <param name="IsError">Whether the section means that something was not imported.</param>
public sealed record ImportReportSection(string Title, int Count, IReadOnlyList<string> Lines, bool IsError);
