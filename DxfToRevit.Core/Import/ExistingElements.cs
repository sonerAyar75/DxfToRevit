namespace DxfToRevit.Core.Import;

/// <summary>Snapshot of the walls, columns and openings of a model, read before an import.</summary>
public sealed class ExistingElements
{
    /// <summary>A model without elements.</summary>
    public static ExistingElements Empty { get; } = new(
        Array.Empty<ExistingWall>(), Array.Empty<ExistingColumn>(), Array.Empty<ExistingOpening>());

    /// <summary>Straight walls.</summary>
    public IReadOnlyList<ExistingWall> Walls { get; }

    /// <summary>Columns.</summary>
    public IReadOnlyList<ExistingColumn> Columns { get; }

    /// <summary>Doors, windows and wall openings.</summary>
    public IReadOnlyList<ExistingOpening> Openings { get; }

    /// <summary>Creates the snapshot.</summary>
    /// <exception cref="ArgumentNullException">A list is <c>null</c>.</exception>
    public ExistingElements(
        IReadOnlyList<ExistingWall> walls, IReadOnlyList<ExistingColumn> columns, IReadOnlyList<ExistingOpening> openings)
    {
        Walls = walls ?? throw new ArgumentNullException(nameof(walls));
        Columns = columns ?? throw new ArgumentNullException(nameof(columns));
        Openings = openings ?? throw new ArgumentNullException(nameof(openings));
    }
}
