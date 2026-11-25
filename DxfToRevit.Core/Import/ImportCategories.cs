namespace DxfToRevit.Core.Import;

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
