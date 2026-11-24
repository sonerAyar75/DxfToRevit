using DxfToRevit.Core.Openings;

namespace DxfToRevit.Core.InMemory;

/// <summary>
/// In-memory <see cref="IOpeningCreator"/>: validates each request and records it in an
/// <see cref="InMemoryBuildingModel"/>, checking that the host wall exists in the model.
/// </summary>
public sealed class InMemoryOpeningCreator : IOpeningCreator
{
    private readonly InMemoryBuildingModel _model;
    private readonly OpeningCreationValidator _validator;

    /// <summary>Creates the creator.</summary>
    /// <param name="model">Model that holds the walls and receives the openings.</param>
    /// <param name="validator">Validator; a default validator is used when <c>null</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> is <c>null</c>.</exception>
    public InMemoryOpeningCreator(InMemoryBuildingModel model, OpeningCreationValidator? validator = null)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _validator = validator ?? new OpeningCreationValidator();
    }

    /// <inheritdoc />
    public OpeningCreationResult CreateOpening(OpeningCreationRequest request, string hostWallElementId)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? error = _validator.Validate(request);
        if (error is not null)
            return OpeningCreationResult.Fail(request.Id, error);

        if (!_model.Walls.Any(w => w.HostElementId == hostWallElementId))
            return OpeningCreationResult.Fail(request.Id, $"Host wall element {hostWallElementId} does not exist.");

        string hostId = _model.AllocateOpeningId();
        _model.AddOpening(new InMemoryOpeningRecord(hostId, hostWallElementId, request));
        return OpeningCreationResult.Ok(request.Id, hostId);
    }
}
