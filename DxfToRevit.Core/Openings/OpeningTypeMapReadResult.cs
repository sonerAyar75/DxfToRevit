namespace DxfToRevit.Core.Openings;

/// <summary>Result of reading an opening type map file.</summary>
/// <param name="Map">Rows that could be read.</param>
/// <param name="Errors">One message per line that could not be read (with its line number).</param>
public sealed record OpeningTypeMapReadResult(OpeningTypeMap Map, IReadOnlyList<string> Errors);
