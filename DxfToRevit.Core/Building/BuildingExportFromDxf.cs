using DxfToRevit.Core.Dxf;
using DxfToRevit.Core.Models;
using netDxf;

namespace DxfToRevit.Core.Building;

/// <summary>
/// Convenience facade that turns pipeline output (wall footprints) and the COLUMN* layers of a
/// DXF document into host elements through a <see cref="BuildingModelService"/>.
/// </summary>
/// <remarks>
/// Request creation is delegated to <see cref="WallCreationRequestFactory"/> and
/// <see cref="ColumnCreationRequestFactory"/>, so the host policy (levels, heights, type keys)
/// comes from the <see cref="ImportSessionOptions"/> those factories were built with.
/// </remarks>
public sealed class BuildingExportFromDxf
{
    private readonly BuildingModelService _building;
    private readonly WallCreationRequestFactory _wallFactory;
    private readonly ColumnCreationRequestFactory _columnFactory;
    private readonly DxfColumnReader _columnReader;

    /// <summary>
    /// Creates the exporter with explicit collaborators. Any optional collaborator that is
    /// <c>null</c> is replaced by a default instance (default session options).
    /// </summary>
    /// <param name="building">Service that validates requests and forwards them to the host creators.</param>
    /// <param name="wallFactory">Factory that converts wall footprints into wall requests.</param>
    /// <param name="columnFactory">Factory that converts column models into column requests.</param>
    /// <param name="columnReader">Reader that extracts columns from COLUMN* layers.</param>
    /// <exception cref="ArgumentNullException"><paramref name="building"/> is <c>null</c>.</exception>
    public BuildingExportFromDxf(
        BuildingModelService building,
        WallCreationRequestFactory? wallFactory = null,
        ColumnCreationRequestFactory? columnFactory = null,
        DxfColumnReader? columnReader = null)
    {
        _building = building ?? throw new ArgumentNullException(nameof(building));
        _wallFactory = wallFactory ?? new WallCreationRequestFactory();
        _columnFactory = columnFactory ?? new ColumnCreationRequestFactory();
        _columnReader = columnReader ?? new DxfColumnReader();
    }

    /// <summary>
    /// Creates the exporter with wall and column factories that share the same import session.
    /// </summary>
    /// <param name="building">Service that validates requests and forwards them to the host creators.</param>
    /// <param name="session">Import policy (levels, heights, type keys) used by both factories.</param>
    /// <param name="columnReader">Optional column reader; a default reader is used when <c>null</c>.</param>
    public BuildingExportFromDxf(
        BuildingModelService building,
        ImportSessionOptions session,
        DxfColumnReader? columnReader = null)
        : this(
            building,
            new WallCreationRequestFactory(session),
            new ColumnCreationRequestFactory(session),
            columnReader)
    {
    }

    /// <summary>Aggregated outcome of an <see cref="Export"/> call.</summary>
    public sealed class ExportResult
    {
        /// <summary>One result per wall request, including validation failures.</summary>
        public IReadOnlyList<WallCreationResult> Walls { get; init; } = Array.Empty<WallCreationResult>();

        /// <summary>One result per column request, including validation failures.</summary>
        public IReadOnlyList<ColumnCreationResult> Columns { get; init; } = Array.Empty<ColumnCreationResult>();

        /// <summary>Number of walls the host created successfully.</summary>
        public int SuccessfulWalls => Walls.Count(w => w.Success);

        /// <summary>Number of columns the host created successfully.</summary>
        public int SuccessfulColumns => Columns.Count(c => c.Success);
    }

    /// <summary>
    /// Creates walls from <paramref name="wallSolids"/> and, when a document is supplied,
    /// columns read from its COLUMN* layers.
    /// </summary>
    /// <param name="wallSolids">Wall footprints produced by the centerline pipeline.</param>
    /// <param name="documentForColumns">DXF document to read columns from; <c>null</c> skips columns.</param>
    /// <returns>Per-element wall and column results.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="wallSolids"/> is <c>null</c>.</exception>
    public ExportResult Export(
        IEnumerable<WallSolid2D> wallSolids,
        DxfDocument? documentForColumns)
    {
        ArgumentNullException.ThrowIfNull(wallSolids);

        var wallResults = _building.CreateWalls(_wallFactory.FromWallSolids(wallSolids));

        IReadOnlyList<ColumnCreationResult> columnResults = Array.Empty<ColumnCreationResult>();
        if (documentForColumns is not null)
        {
            var columns = _columnReader.ReadColumns(documentForColumns);
            columnResults = _building.CreateColumns(_columnFactory.FromColumnModels(columns));
        }

        return new ExportResult { Walls = wallResults, Columns = columnResults };
    }
}
