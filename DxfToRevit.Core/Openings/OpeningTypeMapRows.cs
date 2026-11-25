namespace DxfToRevit.Core.Openings;

/// <summary>
/// Builds the rows of the opening type map editor from the codes found in a DXF and the current
/// map, and reports codes that would get no Revit type.
/// </summary>
public static class OpeningTypeMapRows
{
    /// <summary>
    /// One row per door/window code found in the DXF, with its mapped type, if any. Only the codes of
    /// the drawing are listed; blocks without a <c>TYPE</c> attribute show the <c>*</c> entry (see
    /// <see cref="MapCode"/>).
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
            string code = MapCode(usage.Code);
            string? typeKey = map.Entries.LastOrDefault(e =>
                e.Category == usage.Category &&
                string.Equals(e.Code, code, StringComparison.OrdinalIgnoreCase))?.TypeKey;
            rows.Add(new OpeningTypeMapRow(usage.Category, usage.Code, typeKey, usage.Count, string.Join(", ", usage.Sizes)));
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

    /// <summary>
    /// The map after editing the listed rows: the entries of <paramref name="previous"/> for codes
    /// that are not listed (other drawings) are kept, the listed codes take the edited types (a row
    /// left empty removes its entry).
    /// </summary>
    /// <param name="previous">Map the rows were built from.</param>
    /// <param name="usages">Codes listed in the editor.</param>
    /// <param name="edited">Entries read from the listed rows (map codes, see <see cref="MapCode"/>).</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static OpeningTypeMap Merge(
        OpeningTypeMap previous, IEnumerable<OpeningCodeUsage> usages, IEnumerable<OpeningTypeMapEntry> edited)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(usages);
        ArgumentNullException.ThrowIfNull(edited);

        var listed = usages.Where(IsMappable)
            .Select(u => (u.Category, Code: MapCode(u.Code).ToUpperInvariant()))
            .ToHashSet();
        IEnumerable<OpeningTypeMapEntry> kept = previous.Entries
            .Where(e => !listed.Contains((e.Category, e.Code.ToUpperInvariant())));
        return new OpeningTypeMap(kept.Concat(edited));
    }

    /// <summary>
    /// The map code of a DXF code: blocks without a <c>TYPE</c> attribute are mapped by the
    /// <see cref="OpeningTypeMap.DefaultCode"/> entry, which <see cref="OpeningTypeMap.Resolve"/> uses for them.
    /// </summary>
    /// <param name="dxfCode">Code as found in the DXF; empty when the block has no <c>TYPE</c>.</param>
    public static string MapCode(string dxfCode) =>
        string.IsNullOrWhiteSpace(dxfCode) ? OpeningTypeMap.DefaultCode : dxfCode.Trim();

    /// <summary>Only doors and windows are mapped to Revit types; empty openings need none.</summary>
    private static bool IsMappable(OpeningCodeUsage usage) =>
        usage.Category is OpeningCategory.Door or OpeningCategory.Window;
}
