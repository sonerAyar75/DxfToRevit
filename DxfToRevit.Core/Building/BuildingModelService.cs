namespace DxfToRevit.Core.Building;

/// <summary>
/// Host-independent entry point for creating walls and columns: validates each request and
/// forwards the valid ones to the configured <see cref="IWallCreator"/> / <see cref="IColumnCreator"/>.
/// </summary>
/// <remarks>
/// The same service is used by unit tests (with the in-memory creators) and by the Revit add-in
/// (with the Revit creators), so validation rules are identical in both environments.
/// Results for requests rejected by validation are returned first, followed by the host results
/// for the accepted requests.
/// </remarks>
public sealed class BuildingModelService
{
    private readonly IWallCreator _wallCreator;
    private readonly IColumnCreator? _columnCreator;
    private readonly WallCreationValidator _wallValidator;
    private readonly ColumnCreationValidator _columnValidator;
    private readonly IOpeningCreator? _openingCreator;
    private readonly OpeningCreationValidator _openingValidator;

    /// <summary>Creates the service.</summary>
    /// <param name="wallCreator">Host wall creator (required).</param>
    /// <param name="columnCreator">Host column creator; when <c>null</c>, every column request fails.</param>
    /// <param name="wallValidator">Wall validator; a default validator is used when <c>null</c>.</param>
    /// <param name="columnValidator">Column validator; a default validator is used when <c>null</c>.</param>
    /// <param name="openingCreator">Host opening creator; when <c>null</c>, every opening request fails.</param>
    /// <param name="openingValidator">Opening validator; a default validator is used when <c>null</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="wallCreator"/> is <c>null</c>.</exception>
    public BuildingModelService(
        IWallCreator wallCreator,
        IColumnCreator? columnCreator = null,
        WallCreationValidator? wallValidator = null,
        ColumnCreationValidator? columnValidator = null,
        IOpeningCreator? openingCreator = null,
        OpeningCreationValidator? openingValidator = null)
    {
        _wallCreator = wallCreator ?? throw new ArgumentNullException(nameof(wallCreator));
        _columnCreator = columnCreator;
        _wallValidator = wallValidator ?? new WallCreationValidator();
        _columnValidator = columnValidator ?? new ColumnCreationValidator();
        _openingCreator = openingCreator;
        _openingValidator = openingValidator ?? new OpeningCreationValidator();
    }

    /// <summary>
    /// Validates the opening requests and creates them in their host walls. Must be called after
    /// <see cref="CreateWalls"/>: <paramref name="hostWallIds"/> maps each wall request id to the
    /// element the host created for it.
    /// </summary>
    /// <param name="requests">Opening requests.</param>
    /// <param name="hostWallIds">Wall request id → host element id of the created walls.</param>
    /// <returns>One result per request, in request order.</returns>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public IReadOnlyList<OpeningCreationResult> CreateOpenings(
        IEnumerable<OpeningCreationRequest> requests,
        IReadOnlyDictionary<string, string> hostWallIds)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(hostWallIds);

        var results = new List<OpeningCreationResult>();
        foreach (OpeningCreationRequest request in requests)
        {
            if (_openingCreator is null)
            {
                results.Add(OpeningCreationResult.Fail(request.Id, "No IOpeningCreator configured."));
                continue;
            }

            string? error = _openingValidator.Validate(request);
            if (error is not null)
            {
                results.Add(OpeningCreationResult.Fail(request.Id, error));
                continue;
            }

            if (!hostWallIds.TryGetValue(request.HostWallRequestId, out string? hostWallId))
            {
                results.Add(OpeningCreationResult.Fail(request.Id,
                    $"Host wall {request.HostWallRequestId} was not created, so the opening has no host."));
                continue;
            }

            results.Add(_openingCreator.CreateOpening(request, hostWallId));
        }

        return results;
    }

    /// <summary>
    /// Validates the wall requests and asks the host to create the valid ones.
    /// </summary>
    /// <param name="requests">Wall requests to create.</param>
    /// <returns>One result per request: validation failures first, then the host results.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="requests"/> is <c>null</c>.</exception>
    public IReadOnlyList<WallCreationResult> CreateWalls(IEnumerable<WallCreationRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        var results = new List<WallCreationResult>();
        var accepted = new List<WallCreationRequest>();

        foreach (WallCreationRequest request in requests)
        {
            string? error = _wallValidator.Validate(request);
            if (error is not null)
            {
                results.Add(WallCreationResult.Fail(request.Id, error));
                continue;
            }

            accepted.Add(request);
        }

        if (accepted.Count > 0)
            results.AddRange(_wallCreator.CreateWalls(accepted));

        return results;
    }

    /// <summary>
    /// Validates the column requests and asks the host to create the valid ones.
    /// When no <see cref="IColumnCreator"/> was configured, every request fails.
    /// </summary>
    /// <param name="requests">Column requests to create.</param>
    /// <returns>One result per request: validation failures first, then the host results.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="requests"/> is <c>null</c>.</exception>
    public IReadOnlyList<ColumnCreationResult> CreateColumns(IEnumerable<ColumnCreationRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        var results = new List<ColumnCreationResult>();
        var accepted = new List<ColumnCreationRequest>();

        foreach (ColumnCreationRequest request in requests)
        {
            if (_columnCreator is null)
            {
                results.Add(ColumnCreationResult.Fail(request.Id, "No IColumnCreator configured."));
                continue;
            }

            string? error = _columnValidator.Validate(request);
            if (error is not null)
            {
                results.Add(ColumnCreationResult.Fail(request.Id, error));
                continue;
            }

            accepted.Add(request);
        }

        if (accepted.Count > 0 && _columnCreator is not null)
            results.AddRange(_columnCreator.CreateColumns(accepted));

        return results;
    }
}
