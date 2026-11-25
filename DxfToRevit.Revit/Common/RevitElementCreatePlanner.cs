using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Walls;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Walls;

namespace DxfToRevit.Revit.Common;

/// <summary>
/// Produces wall and column create arguments without calling the Revit API. The document context
/// uses the same arguments when it writes to the document, so the conversion can be unit-tested.
/// </summary>
public sealed class RevitElementCreatePlanner
{
    private readonly WallCreateArgsMapper _wallMapper;
    private readonly ColumnCreateArgsMapper _columnMapper;

    /// <summary>Creates the planner with mappers that share one placement mapper.</summary>
    /// <param name="defaultHeightMm">Height used when a request has none.</param>
    public RevitElementCreatePlanner(double defaultHeightMm = 3000.0)
    {
        var placement = new PlacementCreateArgsMapper(defaultHeightMm);
        _wallMapper = new WallCreateArgsMapper(placement);
        _columnMapper = new ColumnCreateArgsMapper(placement);
    }

    /// <summary>Creates the planner with explicit mappers.</summary>
    /// <param name="wallMapper">Wall mapper.</param>
    /// <param name="columnMapper">Column mapper.</param>
    /// <exception cref="System.ArgumentNullException">A mapper is <c>null</c>.</exception>
    public RevitElementCreatePlanner(
        WallCreateArgsMapper wallMapper,
        ColumnCreateArgsMapper columnMapper)
    {
        _wallMapper = wallMapper ?? throw new ArgumentNullException(nameof(wallMapper));
        _columnMapper = columnMapper ?? throw new ArgumentNullException(nameof(columnMapper));
    }

    /// <summary>Creates the arguments for one wall.</summary>
    /// <param name="request">Wall request.</param>
    public WallCreateArgs PlanWall(WallCreationRequest request) => _wallMapper.Map(request);

    /// <summary>Creates the arguments for several walls.</summary>
    /// <param name="requests">Wall requests.</param>
    public IReadOnlyList<WallCreateArgs> PlanWalls(IEnumerable<WallCreationRequest> requests) =>
        _wallMapper.MapMany(requests);

    /// <summary>Creates the arguments for one column.</summary>
    /// <param name="request">Column request.</param>
    public ColumnCreateArgs PlanColumn(ColumnCreationRequest request) => _columnMapper.Map(request);

    /// <summary>Creates the arguments for several columns.</summary>
    /// <param name="requests">Column requests.</param>
    public IReadOnlyList<ColumnCreateArgs> PlanColumns(IEnumerable<ColumnCreationRequest> requests) =>
        _columnMapper.MapMany(requests);
}
