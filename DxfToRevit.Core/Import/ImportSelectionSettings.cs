namespace DxfToRevit.Core.Import;

/// <summary>
/// Which categories are imported and whether elements already in the model are skipped, kept for
/// the session. Categories are kept by key and only the excluded ones are stored, so the categories
/// of a new module are imported unless the user turns them off.
/// </summary>
public sealed class ImportSelectionSettings
{
    private readonly HashSet<string> _excluded = new(StringComparer.Ordinal);

    /// <summary>
    /// Whether elements that are already in the model (same kind, same place, same level) are
    /// skipped, so importing an updated drawing only adds what is new.
    /// </summary>
    public bool SkipExisting { get; set; } = true;

    /// <summary>Whether a category is imported.</summary>
    /// <param name="category">Import category.</param>
    /// <exception cref="ArgumentNullException"><paramref name="category"/> is <c>null</c>.</exception>
    public bool IsIncluded(ImportCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);
        return !_excluded.Contains(category.Key);
    }

    /// <summary>Sets whether a category is imported.</summary>
    /// <param name="category">Import category.</param>
    /// <param name="included">Whether it is imported.</param>
    /// <exception cref="ArgumentNullException"><paramref name="category"/> is <c>null</c>.</exception>
    public void SetIncluded(ImportCategory category, bool included)
    {
        ArgumentNullException.ThrowIfNull(category);
        if (included)
            _excluded.Remove(category.Key);
        else
            _excluded.Add(category.Key);
    }
}
