using DxfToRevit.Core.Import;

namespace DxfToRevit.Addin;

/// <summary>A category check box of the import settings: the category and how many the DXF contains.</summary>
/// <param name="Category">Import category of a module.</param>
/// <param name="Count">Elements in the DXF; <c>null</c> when no DXF is loaded.</param>
internal sealed record ImportCategoryChoice(ImportCategory Category, int? Count);

/// <summary>
/// Which categories are imported, and whether elements already in the model are skipped;
/// remembered for the current Revit session. Categories are kept by key, so the categories of a
/// new module are imported unless the user turns them off.
/// </summary>
internal static class ImportSelectionPreferences
{
    private static readonly HashSet<string> Excluded = new(StringComparer.Ordinal);

    /// <summary>Whether a category is imported.</summary>
    /// <param name="category">Import category.</param>
    public static bool IsIncluded(ImportCategory category) => !Excluded.Contains(category.Key);

    /// <summary>Remembers whether a category is imported.</summary>
    /// <param name="category">Import category.</param>
    /// <param name="included">Whether it is imported.</param>
    public static void SetIncluded(ImportCategory category, bool included)
    {
        if (included)
            Excluded.Remove(category.Key);
        else
            Excluded.Add(category.Key);
    }

    /// <summary>
    /// Whether elements that are already in the model (same kind, same place, same level) are
    /// skipped, so importing an updated drawing only adds what is new.
    /// </summary>
    public static bool SkipExisting { get; set; } = true;
}
