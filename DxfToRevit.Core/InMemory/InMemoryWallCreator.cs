using DxfToRevit.Core.Walls;

namespace DxfToRevit.Core.InMemory;

/// <summary>
/// In-memory <see cref="IWallCreator"/>: validates each request and records it in an
/// <see cref="InMemoryBuildingModel"/>. Lets the wall creation flow be tested without Revit.
/// </summary>
public sealed class InMemoryWallCreator : IWallCreator
{
    private readonly InMemoryBuildingModel _model;
    private readonly WallCreationValidator _validator;

    /// <summary>Creates the creator.</summary>
    /// <param name="model">Model that receives the created walls.</param>
    /// <param name="validator">Validator; a default validator is used when <c>null</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> is <c>null</c>.</exception>
    public InMemoryWallCreator(
        InMemoryBuildingModel model,
        WallCreationValidator? validator = null)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _validator = validator ?? new WallCreationValidator();
    }

    /// <inheritdoc />
    public WallCreationResult CreateWall(WallCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? error = _validator.Validate(request);
        if (error is not null)
            return WallCreationResult.Fail(request.Id, error);

        string hostId = _model.AllocateWallId();
        _model.AddWall(new InMemoryWallRecord(hostId, request));
        return WallCreationResult.Ok(request.Id, hostId);
    }

    /// <inheritdoc />
    public IReadOnlyList<WallCreationResult> CreateWalls(IEnumerable<WallCreationRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        return requests.Select(CreateWall).ToList();
    }
}
