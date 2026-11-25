namespace DxfToRevit.Core.Columns;

/// <summary>
/// The column choices of the user, kept for the session (circular mode, chosen types, freeform
/// options). <see cref="ToOptions"/> turns them into the options of <see cref="ColumnImportModule"/>.
/// </summary>
public sealed class ColumnImportSettings
{
    /// <summary>Type key of freeform columns created as DirectShapes (when not built as families).</summary>
    public const string FreeformSolidTypeKey = "Freeform Column";

    /// <summary>How circular columns are imported.</summary>
    public CircularColumnMode CircularMode { get; set; } = CircularColumnMode.CircularFamily;

    /// <summary>
    /// Circular column type (<c>Family::Type</c>) or <see cref="ColumnTypeKeys.AutoRoundFamily"/>;
    /// <c>null</c> for automatic.
    /// </summary>
    public string? CircularTypeKey { get; set; }

    /// <summary>Square column type (<c>Family::Type</c>) that replaces circular columns.</summary>
    public string? SquareTypeKey { get; set; }

    /// <summary>Whether freeform columns are imported.</summary>
    public bool IncludeFreeform { get; set; } = true;

    /// <summary>Whether freeform columns are built as column families bound to the levels (otherwise solids).</summary>
    public bool FreeformAsFamily { get; set; } = true;

    /// <summary>The options of <see cref="ColumnImportModule"/> for these choices.</summary>
    public ColumnImportOptions ToOptions() => new()
    {
        CircularTypeKey = CircularMode == CircularColumnMode.RoundSolid
            ? ColumnTypeKeys.RoundDirectShape
            : CircularTypeKey ?? ColumnTypeKeys.AutoRoundFamily,
        ConvertCircularToSquare = CircularMode == CircularColumnMode.SquareFallback,
        SquareTypeKey = SquareTypeKey,
        FreeformTypeKey = FreeformAsFamily ? ColumnTypeKeys.FreeformFamily : FreeformSolidTypeKey,
        IncludeCircular = CircularMode != CircularColumnMode.Skip,
        IncludeFreeform = IncludeFreeform
    };
}
