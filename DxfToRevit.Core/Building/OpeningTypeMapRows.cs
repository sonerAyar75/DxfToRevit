using DxfToRevit.Core.Openings;

namespace DxfToRevit.Core.Building;

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

/// <summary>
/// Builds the rows of the opening type map editor from the codes found in a DXF and the current
/// map, and reports codes that would get no Revit type.
/// </summary>
public static class OpeningTypeMapRows
{
    /// <summary>
    /// One row per door/window code found in the DXF (with its mapped type, if any), followed by
    /// the map entries whose codes are not in the DXF (for example <c>*</c>), in map order.
    /// </summary>
    /// <param name="map">Current type map.</param>
    /// <param name="usages">Codes found in the DXF; empty when no DXF is loaded.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static IReadOnlyList<OpeningTypeMapRow> Build(OpeningTypeMap map, IEnumerable<OpeningCodeUsage> usages)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(usages);

        var rows = new List<OpeningTypeMapRow>();
        foreach (OpeningCodeUsage usage in usages.Where(IsMappable))
        {
            string? typeKey = map.Entries.LastOrDefault(e =>
                e.Category == usage.Category &&
                string.Equals(e.Code, usage.Code, StringComparison.OrdinalIgnoreCase))?.TypeKey;
            rows.Add(new OpeningTypeMapRow(usage.Category, usage.Code, typeKey, usage.Count, string.Join(", ", usage.Sizes)));
        }

        foreach (OpeningTypeMapEntry entry in map.Entries)
        {
            bool shown = rows.Any(r => r.Category == entry.Category &&
                                       string.Equals(r.Code, entry.Code, StringComparison.OrdinalIgnoreCase));
            if (!shown)
                rows.Add(new OpeningTypeMapRow(entry.Category, entry.Code, entry.TypeKey, 0, string.Empty));
        }

        return rows;
    }

    /// <summary>Door/window codes of the DXF that resolve to no Revit type (neither their own row nor <c>*</c>).</summary>
    /// <param name="map">Type map.</param>
    /// <param name="usages">Codes found in the DXF.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static IReadOnlyList<OpeningCodeUsage> Unmapped(OpeningTypeMap map, IEnumerable<OpeningCodeUsage> usages)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(usages);
        return usages.Where(u => IsMappable(u) && map.Resolve(u.Category, u.Code) is null).ToList();
    }

    /// <summary>Only doors and windows are mapped to Revit types; empty openings need none.</summary>
    private static bool IsMappable(OpeningCodeUsage usage) =>
        usage.Category is OpeningCategory.Door or OpeningCategory.Window;
}
