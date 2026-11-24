using DxfToRevit.Core.Dxf;

namespace DxfToRevit.Core.Import;

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
    /// <param name="drawing">The DXF drawing: the loaded document and its analysis.</param>
    int CountInDrawing(ImportCategory category, DxfDrawing drawing);

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
