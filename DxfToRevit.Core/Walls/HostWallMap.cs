namespace DxfToRevit.Core.Walls;

/// <summary>
/// Wall request id → element id of the wall that hosts the openings of that wall: the wall created
/// in this import, otherwise the matching wall already in the model. Provided by
/// <see cref="WallImportModule"/> for the modules that place elements in walls.
/// </summary>
public sealed class HostWallMap
{
    private readonly Dictionary<string, string> _ids = new(StringComparer.Ordinal);

    /// <summary>Wall request id → host element id.</summary>
    public IReadOnlyDictionary<string, string> Ids => _ids;

    /// <summary>Sets the element of a wall request.</summary>
    /// <param name="wallRequestId">Wall request id.</param>
    /// <param name="elementId">Element id.</param>
    public void Set(string wallRequestId, string elementId) => _ids[wallRequestId] = elementId;
}
