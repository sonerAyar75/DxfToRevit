namespace DxfToRevit.Addin;

/// <summary>How many elements of each category the DXF contains, shown next to the import check boxes.</summary>
/// <param name="Walls">Walls.</param>
/// <param name="Columns">Columns.</param>
/// <param name="Doors">Doors.</param>
/// <param name="Windows">Windows.</param>
/// <param name="EmptyOpenings">Empty wall openings.</param>
internal sealed record ImportCounts(int Walls, int Columns, int Doors, int Windows, int EmptyOpenings);

/// <summary>How many elements of each category were not created because they are already in the model.</summary>
/// <param name="Walls">Walls.</param>
/// <param name="Columns">Columns.</param>
/// <param name="Doors">Doors.</param>
/// <param name="Windows">Windows.</param>
/// <param name="EmptyOpenings">Empty wall openings.</param>
internal sealed record SkippedCounts(int Walls, int Columns, int Doors, int Windows, int EmptyOpenings)
{
    /// <summary>All skipped elements.</summary>
    public int Total => Walls + Columns + Doors + Windows + EmptyOpenings;

    /// <summary>The non-zero counts, for example <c>12 walls, 3 doors</c>.</summary>
    public string Describe() =>
        string.Join(", ", new[]
            {
                (Walls, "wall(s)"), (Columns, "column(s)"), (Doors, "door(s)"), (Windows, "window(s)"),
                (EmptyOpenings, "empty opening(s)")
            }
            .Where(c => c.Item1 > 0)
            .Select(c => $"{c.Item1} {c.Item2}"));
}

/// <summary>
/// Which categories are imported, and whether elements already in the model are skipped;
/// remembered for the current Revit session.
/// </summary>
internal static class ImportSelectionPreferences
{
    /// <summary>Whether walls are imported.</summary>
    public static bool IncludeWalls { get; set; } = true;

    /// <summary>Whether columns are imported.</summary>
    public static bool IncludeColumns { get; set; } = true;

    /// <summary>Whether doors are imported.</summary>
    public static bool IncludeDoors { get; set; } = true;

    /// <summary>Whether windows are imported.</summary>
    public static bool IncludeWindows { get; set; } = true;

    /// <summary>Whether empty wall openings are imported.</summary>
    public static bool IncludeEmptyOpenings { get; set; } = true;

    /// <summary>
    /// Whether elements that are already in the model (same kind, same place, same level) are
    /// skipped, so importing an updated drawing only adds what is new.
    /// </summary>
    public static bool SkipExisting { get; set; } = true;
}
