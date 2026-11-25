namespace DxfToRevit.Core.Openings;

/// <summary>
/// Maps opening type codes (the <c>TYPE</c> attribute) to Revit type keys, per category.
/// A row with code <see cref="DefaultCode"/> is used for codes without their own row.
/// </summary>
public sealed class OpeningTypeMap
{
    /// <summary>Code of the per-category default row.</summary>
    public const string DefaultCode = "*";

    /// <summary>Rows in the order they were given; later duplicates override earlier ones.</summary>
    public IReadOnlyList<OpeningTypeMapEntry> Entries { get; }

    /// <summary>An empty map.</summary>
    public static OpeningTypeMap Empty { get; } = new(Array.Empty<OpeningTypeMapEntry>());

    /// <summary>Creates a map.</summary>
    /// <param name="entries">Rows; rows with an empty code or type key are ignored.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <c>null</c>.</exception>
    public OpeningTypeMap(IEnumerable<OpeningTypeMapEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        Entries = entries
            .Where(e => !string.IsNullOrWhiteSpace(e.Code) && !string.IsNullOrWhiteSpace(e.TypeKey))
            .Select(e => e with { Code = e.Code.Trim(), TypeKey = e.TypeKey.Trim() })
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Returns the type key for a category and code: the row with that code, otherwise the default
    /// row of the category, otherwise <c>null</c>. Codes are compared case-insensitively.
    /// </summary>
    /// <param name="category">Opening category.</param>
    /// <param name="code">Type code (may be empty).</param>
    public string? Resolve(OpeningCategory category, string? code)
    {
        string wanted = code?.Trim() ?? string.Empty;
        OpeningTypeMapEntry? exact = Entries.LastOrDefault(e =>
            e.Category == category && string.Equals(e.Code, wanted, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return exact.TypeKey;

        return Entries.LastOrDefault(e => e.Category == category && e.Code == DefaultCode)?.TypeKey;
    }
}
