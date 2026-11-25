namespace DxfToRevit.Core.InMemory;

/// <summary>
/// In-memory stand-in for a host document (such as a Revit project). Stores created walls and
/// columns and hands out sequential host ids. Used by unit tests and simulations.
/// </summary>
public sealed class InMemoryBuildingModel
{
    private readonly List<InMemoryWallRecord> _walls = new();
    private readonly List<InMemoryColumnRecord> _columns = new();
    private readonly List<InMemoryOpeningRecord> _openings = new();
    private int _nextWallId = 1;
    private int _nextColumnId = 1;
    private int _nextOpeningId = 1;

    /// <summary>Walls created so far, in creation order.</summary>
    public IReadOnlyList<InMemoryWallRecord> Walls => _walls;

    /// <summary>Columns created so far, in creation order.</summary>
    public IReadOnlyList<InMemoryColumnRecord> Columns => _columns;

    /// <summary>Openings created so far, in creation order.</summary>
    public IReadOnlyList<InMemoryOpeningRecord> Openings => _openings;

    /// <summary>Returns the next wall host id (<c>W-0001</c>, <c>W-0002</c>, ...).</summary>
    public string AllocateWallId() => $"W-{_nextWallId++:D4}";

    /// <summary>Returns the next column host id (<c>C-0001</c>, <c>C-0002</c>, ...).</summary>
    public string AllocateColumnId() => $"C-{_nextColumnId++:D4}";

    /// <summary>Returns the next opening host id (<c>O-0001</c>, <c>O-0002</c>, ...).</summary>
    public string AllocateOpeningId() => $"O-{_nextOpeningId++:D4}";

    /// <summary>Stores a created wall.</summary>
    /// <param name="record">Wall record to store.</param>
    /// <exception cref="ArgumentNullException"><paramref name="record"/> is <c>null</c>.</exception>
    public void AddWall(InMemoryWallRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _walls.Add(record);
    }

    /// <summary>Stores a created column.</summary>
    /// <param name="record">Column record to store.</param>
    /// <exception cref="ArgumentNullException"><paramref name="record"/> is <c>null</c>.</exception>
    public void AddColumn(InMemoryColumnRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _columns.Add(record);
    }

    /// <summary>Stores a created opening.</summary>
    /// <param name="record">Opening record to store.</param>
    /// <exception cref="ArgumentNullException"><paramref name="record"/> is <c>null</c>.</exception>
    public void AddOpening(InMemoryOpeningRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _openings.Add(record);
    }

    /// <summary>Removes every element and restarts id numbering.</summary>
    public void Clear()
    {
        _walls.Clear();
        _columns.Clear();
        _openings.Clear();
        _nextWallId = 1;
        _nextColumnId = 1;
        _nextOpeningId = 1;
    }
}
