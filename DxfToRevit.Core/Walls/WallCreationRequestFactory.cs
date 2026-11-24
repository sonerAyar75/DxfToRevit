using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Import;

namespace DxfToRevit.Core.Walls;

/// <summary>
/// Converts pipeline output (centerlines or wall footprints) into <see cref="WallCreationRequest"/>s,
/// applying the type and placement policy of an <see cref="ImportSessionOptions"/>.
/// </summary>
public sealed class WallCreationRequestFactory
{
    private readonly ImportSessionOptions _session;

    /// <summary>Creates the factory.</summary>
    /// <param name="session">Import policy; default options are used when <c>null</c>.</param>
    public WallCreationRequestFactory(ImportSessionOptions? session = null)
    {
        _session = session ?? new ImportSessionOptions();
    }

    /// <summary>Import policy used for type keys, structural flag and placement.</summary>
    public ImportSessionOptions Session => _session;

    /// <summary>
    /// Creates one request per non-degenerate centerline. Ids are <c>W001</c>, <c>W002</c>, ... by
    /// input position, so skipped degenerate lines leave gaps in the numbering.
    /// </summary>
    /// <param name="centerLines">Wall centerlines in millimetres.</param>
    /// <param name="thicknessMm">Thickness of each centerline (same order and count).</param>
    /// <returns>The wall requests.</returns>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The two lists have different lengths.</exception>
    public IReadOnlyList<WallCreationRequest> FromCenterLines(
        IReadOnlyList<LineSegment2D> centerLines,
        IReadOnlyList<double> thicknessMm)
    {
        ArgumentNullException.ThrowIfNull(centerLines);
        ArgumentNullException.ThrowIfNull(thicknessMm);
        if (centerLines.Count != thicknessMm.Count)
            throw new ArgumentException("centerLines and thicknessMm length mismatch.");

        var list = new List<WallCreationRequest>(centerLines.Count);
        for (int i = 0; i < centerLines.Count; i++)
        {
            if (centerLines[i].IsDegenerate)
                continue;
            list.Add(CreateRequest($"W{i + 1:D3}", centerLines[i], thicknessMm[i]));
        }

        return list;
    }

    /// <summary>
    /// Creates one request per wall footprint with a non-degenerate centerline.
    /// Ids follow the input position (<c>W001</c>, <c>W002</c>, ...).
    /// </summary>
    /// <param name="solids">Wall footprints produced by the pipeline.</param>
    /// <returns>The wall requests.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="solids"/> is <c>null</c>.</exception>
    public IReadOnlyList<WallCreationRequest> FromWallSolids(IEnumerable<WallSolid2D> solids)
    {
        ArgumentNullException.ThrowIfNull(solids);

        var list = solids is ICollection<WallSolid2D> collection
            ? new List<WallCreationRequest>(collection.Count)
            : new List<WallCreationRequest>();

        int index = 0;
        foreach (WallSolid2D solid in solids)
        {
            string id = WallIdForIndex(index++);
            if (solid.CenterLine.IsDegenerate)
                continue;
            list.Add(CreateRequest(id, solid.CenterLine, solid.ThicknessMm));
        }

        return list;
    }

    /// <summary>
    /// Request id of the wall created from the footprint at <paramref name="index"/> (0-based) by
    /// <see cref="FromWallSolids"/>: <c>W001</c> for index 0. Openings use it to find their host wall.
    /// </summary>
    /// <param name="index">0-based footprint index.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative.</exception>
    public static string WallIdForIndex(int index)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));
        return $"W{index + 1:D3}";
    }

    /// <summary>Builds a request with the session's wall type, structural flag and placement.</summary>
    private WallCreationRequest CreateRequest(string id, LineSegment2D centerLine, double thicknessMm)
    {
        var type = new WallTypeProperties(
            thicknessMm,
            _session.ResolveWallTypeKey(thicknessMm),
            _session.WallsAreStructural);

        return new WallCreationRequest(id, centerLine.Start, centerLine.End, type, _session.CreatePlacement());
    }
}
