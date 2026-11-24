namespace DxfToRevit.Addin;

/// <summary>
/// Column import choices remembered for the current Revit session, so the settings dialog
/// reopens with the last selection.
/// </summary>
internal static class ColumnImportPreferences
{
    /// <summary>Last chosen circular column mode.</summary>
    public static CircularColumnImportMode CircularMode { get; set; } = CircularColumnImportMode.CircularFamily;

    /// <summary>Whether freeform columns are imported.</summary>
    public static bool IncludeFreeformColumns { get; set; } = true;

    /// <summary>
    /// Whether freeform columns are built as column families (bound to levels);
    /// <c>false</c> creates them as DirectShape solids.
    /// </summary>
    public static bool FreeformAsFamily { get; set; } = true;

    /// <summary>Last selected circular column type key.</summary>
    public static string? CircularTypeKey { get; set; }

    /// <summary>Last selected square/rectangular column type key.</summary>
    public static string? SquareTypeKey { get; set; }
}
