namespace DxfToRevit.Core.Import;

/// <summary>
/// A kind of element the user can choose to import, shown as one check box in the import settings
/// (for example walls or doors). A module imports one or more categories.
/// </summary>
/// <param name="Key">Stable identifier, used to remember the choice.</param>
/// <param name="Name">Plural display name, for example <c>Walls</c>.</param>
public sealed record ImportCategory(string Key, string Name);

/// <summary>The categories of the standard modules.</summary>
public static class ImportCategories
{
    /// <summary>Walls.</summary>
    public static ImportCategory Walls { get; } = new("walls", "Walls");

    /// <summary>Columns.</summary>
    public static ImportCategory Columns { get; } = new("columns", "Columns");

    /// <summary>Doors.</summary>
    public static ImportCategory Doors { get; } = new("doors", "Doors");

    /// <summary>Windows.</summary>
    public static ImportCategory Windows { get; } = new("windows", "Windows");

    /// <summary>Empty wall openings.</summary>
    public static ImportCategory EmptyOpenings { get; } = new("emptyOpenings", "Empty openings");
}
