using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Dxf;

namespace DxfToRevit.Core.Import;

/// <summary>Everything a module needs to decide what to create.</summary>
public sealed class ImportPlanContext
{
    private readonly IReadOnlySet<string> _included;

    /// <summary>Creates the context.</summary>
    /// <param name="drawing">The DXF drawing: the loaded document and its analysis.</param>
    /// <param name="session">Level, height and type options of the import.</param>
    /// <param name="existing">Elements already in the model.</param>
    /// <param name="includedCategoryKeys">Keys of the categories chosen for import.</param>
    /// <param name="skipExisting">Whether requests already in the model are not created again.</param>
    /// <param name="options">Module options set by the host.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public ImportPlanContext(
        DxfDrawing drawing,
        ImportSessionOptions session,
        ExistingElementMatcher existing,
        IEnumerable<string> includedCategoryKeys,
        bool skipExisting,
        ImportBag? options = null)
    {
        Drawing = drawing ?? throw new ArgumentNullException(nameof(drawing));
        Session = session ?? throw new ArgumentNullException(nameof(session));
        Existing = existing ?? throw new ArgumentNullException(nameof(existing));
        ArgumentNullException.ThrowIfNull(includedCategoryKeys);
        _included = includedCategoryKeys.ToHashSet(StringComparer.Ordinal);
        SkipExisting = skipExisting;
        Options = options ?? new ImportBag();
    }

    /// <summary>The DXF drawing: the loaded document and its analysis.</summary>
    public DxfDrawing Drawing { get; }

    /// <summary>Level, height and type options of the import.</summary>
    public ImportSessionOptions Session { get; }

    /// <summary>Finds the elements already in the model.</summary>
    public ExistingElementMatcher Existing { get; }

    /// <summary>Whether requests already in the model are not created again.</summary>
    public bool SkipExisting { get; }

    /// <summary>Module options set by the host (for example <see cref="ColumnImportOptions"/>).</summary>
    public ImportBag Options { get; }

    /// <summary>Whether a category was chosen for import.</summary>
    /// <param name="category">Category.</param>
    public bool IsIncluded(ImportCategory category) => _included.Contains(category.Key);
}
