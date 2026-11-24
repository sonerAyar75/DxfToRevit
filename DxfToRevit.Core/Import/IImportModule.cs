using DxfToRevit.Core.Building;
using DxfToRevit.Core.Services;

namespace DxfToRevit.Core.Import;

/// <summary>Everything a module needs to decide what to create.</summary>
public sealed class ImportPlanContext
{
    private readonly IReadOnlySet<string> _included;

    /// <summary>Creates the context.</summary>
    /// <param name="drawing">Analysed DXF drawing.</param>
    /// <param name="session">Level, height and type options of the import.</param>
    /// <param name="existing">Elements already in the model.</param>
    /// <param name="includedCategoryKeys">Keys of the categories chosen for import.</param>
    /// <param name="skipExisting">Whether requests already in the model are not created again.</param>
    /// <param name="options">Module options set by the host.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public ImportPlanContext(
        DxfCenterLinePipeline.Result drawing,
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

    /// <summary>Analysed DXF drawing.</summary>
    public DxfCenterLinePipeline.Result Drawing { get; }

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

/// <summary>What a module plans to create, and what it skips because it is already in the model.</summary>
/// <param name="ToCreate">Number of elements the module will try to create.</param>
/// <param name="Skipped">Elements already in the model, per category.</param>
public sealed record ImportModulePlan(int ToCreate, IReadOnlyDictionary<ImportCategory, int> Skipped)
{
    /// <summary>A plan that creates nothing and skips nothing.</summary>
    public static ImportModulePlan Nothing { get; } = new(0, new Dictionary<ImportCategory, int>());
}

/// <summary>
/// One kind of building element imported from a DXF drawing (walls, columns, openings, ...). A new
/// kind of element is a new module registered with the host; the import flow, the settings check
/// boxes and the summary work with every module the same way.
/// </summary>
/// <remarks>
/// A module instance serves one import. <see cref="ImportCoordinator"/> calls <see cref="Plan"/> on
/// every module in <see cref="Order"/>, then, inside the host transaction, <see cref="Create"/> in
/// the same order. Modules pass data to later modules through <see cref="ImportBag"/> values (for
/// example the walls that host the doors). Creation goes through the host-independent creators,
/// so a module runs in tests with the in-memory creators.
/// </remarks>
public interface IImportModule
{
    /// <summary>Plural name used in the summary, for example <c>Walls</c>.</summary>
    string Name { get; }

    /// <summary>Title of the summary section for notes on created elements, for example <c>Wall notes</c>.</summary>
    string NoteTitle { get; }

    /// <summary>The categories the user can choose, in display order.</summary>
    IReadOnlyList<ImportCategory> Categories { get; }

    /// <summary>Position in the import; lower runs first (walls before what they host).</summary>
    int Order { get; }

    /// <summary>Whether the model must be regenerated after this module, before the next one creates.</summary>
    bool RegenerateAfter { get; }

    /// <summary>How many elements of a category the drawing contains.</summary>
    /// <param name="category">One of <see cref="Categories"/>.</param>
    /// <param name="drawing">Analysed drawing.</param>
    int CountInDrawing(ImportCategory category, DxfCenterLinePipeline.Result drawing);

    /// <summary>
    /// Builds the creation requests of the chosen categories and sets aside those already in the
    /// model. Called for every module, also when none of its categories is chosen, so that it can
    /// still provide data to other modules.
    /// </summary>
    /// <param name="context">Plan context.</param>
    /// <param name="shared">Values shared with the other modules.</param>
    ImportModulePlan Plan(ImportPlanContext context, ImportBag shared);

    /// <summary>Creates the planned elements.</summary>
    /// <param name="shared">Values shared with the other modules.</param>
    ImportModuleResult Create(ImportBag shared);
}
