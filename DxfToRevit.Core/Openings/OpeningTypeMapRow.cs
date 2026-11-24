namespace DxfToRevit.Core.Openings;

/// <summary>One row of the opening type map editor.</summary>
/// <param name="Category">Door or window.</param>
/// <param name="Code">Type code; empty for blocks without a <c>TYPE</c> attribute.</param>
/// <param name="TypeKey">Revit type mapped to exactly this code; <c>null</c> when not mapped.</param>
/// <param name="CountInDxf">Openings with this code in the loaded DXF; 0 for rows that only come from the map.</param>
/// <param name="Sizes">Sizes found in the DXF (<c>&lt;w&gt;x&lt;h&gt;</c>, comma-separated); empty for map-only rows.</param>
public sealed record OpeningTypeMapRow(
    OpeningCategory Category, string Code, string? TypeKey, int CountInDxf, string Sizes)
{
    /// <summary>Whether the row was produced by the loaded DXF.</summary>
    public bool FromDxf => CountInDxf > 0;
}
