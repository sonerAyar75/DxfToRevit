using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Import;
using DxfToRevit.Core.Openings;

namespace DxfToRevit.Addin.Settings;

/// <summary>
/// The import choices of the user: which categories, the column options and the door and window
/// options. One instance (<see cref="Session"/>) is kept for the Revit session and passed to the
/// dialogs and commands, which therefore do not depend on global state.
/// </summary>
internal sealed class ImportSettings
{
    /// <summary>The choices kept for the current Revit session.</summary>
    public static ImportSettings Session { get; } = new();

    /// <summary>Which categories are imported and whether elements already in the model are skipped.</summary>
    public ImportSelectionSettings Selection { get; } = new();

    /// <summary>Column options.</summary>
    public ColumnImportSettings Columns { get; } = new();

    /// <summary>Door, window and opening options.</summary>
    public OpeningImportSettings Openings { get; } = new();
}
