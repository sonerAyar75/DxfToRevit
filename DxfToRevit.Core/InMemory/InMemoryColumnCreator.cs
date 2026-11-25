using DxfToRevit.Core.Columns;

namespace DxfToRevit.Core.InMemory;

/// <summary>
/// In-memory <see cref="IColumnCreator"/>: validates each request and records it in an
/// <see cref="InMemoryBuildingModel"/>. Lets the COLUMN-layer → column flow be tested without Revit.
/// </summary>
public sealed class InMemoryColumnCreator : IColumnCreator
{
    private readonly InMemoryBuildingModel _model;
    private readonly ColumnCreationValidator _validator;

    /// <summary>Creates the creator.</summary>
    /// <param name="model">Model that receives the created columns.</param>
    /// <param name="validator">Validator; a default validator is used when <c>null</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> is <c>null</c>.</exception>
    public InMemoryColumnCreator(
        InMemoryBuildingModel model,
        ColumnCreationValidator? validator = null)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _validator = validator ?? new ColumnCreationValidator();
    }

    /// <inheritdoc />
    public ColumnCreationResult CreateColumn(ColumnCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? error = _validator.Validate(request);
        if (error is not null)
            return ColumnCreationResult.Fail(request.Id, error);

        string hostId = _model.AllocateColumnId();
        _model.AddColumn(new InMemoryColumnRecord(hostId, request));
        return ColumnCreationResult.Ok(request.Id, hostId);
    }

    /// <inheritdoc />
    public IReadOnlyList<ColumnCreationResult> CreateColumns(IEnumerable<ColumnCreationRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        return requests.Select(CreateColumn).ToList();
    }
}
