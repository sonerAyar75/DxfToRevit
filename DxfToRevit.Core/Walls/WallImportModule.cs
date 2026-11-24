using DxfToRevit.Core.Dxf;
using DxfToRevit.Core.Import;

namespace DxfToRevit.Core.Walls;

/// <summary>
/// Walls from the wall footprints of the drawing. Provides a <see cref="HostWallMap"/>: walls that
/// are already in the model host the doors and windows even when walls are not imported.
/// </summary>
public sealed class WallImportModule : IImportModule
{
    private readonly BuildingModelService _service;
    private IReadOnlyList<WallCreationRequest> _toCreate = Array.Empty<WallCreationRequest>();
    private bool _included;

    /// <summary>Creates the module.</summary>
    /// <param name="service">Validates the requests and calls the host creators.</param>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <c>null</c>.</exception>
    public WallImportModule(BuildingModelService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <inheritdoc />
    public string Name => "Walls";

    /// <inheritdoc />
    public string NoteTitle => "Wall notes";

    /// <inheritdoc />
    public IReadOnlyList<ImportCategory> Categories { get; } = new[] { ImportCategories.Walls };

    /// <inheritdoc />
    public int Order => 100;

    /// <inheritdoc />
    /// <remarks>Columns join walls and openings need their host walls, so the model is regenerated.</remarks>
    public bool RegenerateAfter => true;

    /// <inheritdoc />
    public int CountInDrawing(ImportCategory category, DxfCenterLinePipeline.Result drawing) =>
        drawing.WallSolids.Count;

    /// <inheritdoc />
    public ImportModulePlan Plan(ImportPlanContext context, ImportBag shared)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(shared);

        IReadOnlyList<WallCreationRequest> requests =
            new WallCreationRequestFactory(context.Session).FromWallSolids(context.Drawing.WallSolids);
        ExistingSplit<WallCreationRequest> split =
            ExistingElementMatcher.Split(requests, r => r.Id, context.Existing.FindWall);

        // Walls already in the model always host openings, whether or not walls are imported.
        var hosts = new HostWallMap();
        foreach (KeyValuePair<string, string> existing in split.Existing)
            hosts.Set(existing.Key, existing.Value);
        shared.Set(hosts);

        _included = context.IsIncluded(ImportCategories.Walls);
        if (!_included)
        {
            _toCreate = Array.Empty<WallCreationRequest>();
            return ImportModulePlan.Nothing;
        }

        _toCreate = context.SkipExisting ? split.New : requests;
        int skipped = context.SkipExisting ? split.Existing.Count : 0;
        return new ImportModulePlan(_toCreate.Count, new Dictionary<ImportCategory, int> { [ImportCategories.Walls] = skipped });
    }

    /// <inheritdoc />
    public ImportModuleResult Create(ImportBag shared)
    {
        ArgumentNullException.ThrowIfNull(shared);

        IReadOnlyList<WallCreationResult> results = _service.CreateWalls(_toCreate);
        HostWallMap hosts = shared.GetOrDefault(new HostWallMap());
        foreach (WallCreationResult r in results.Where(r => r.Success && r.HostElementId is not null))
            hosts.Set(r.RequestId, r.HostElementId!);
        shared.Set(hosts);

        return new ImportModuleResult(Name, NoteTitle, _included,
            results.Select(r => new ImportEntry(r.RequestId, r.Success, r.HostElementId, r.Message)).ToList());
    }
}
