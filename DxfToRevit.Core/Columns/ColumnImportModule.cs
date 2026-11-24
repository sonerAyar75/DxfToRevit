using DxfToRevit.Core.Dxf;
using DxfToRevit.Core.Import;

namespace DxfToRevit.Core.Columns;

/// <summary>Columns from the closed polylines and circles of the column layers.</summary>
public sealed class ColumnImportModule : IImportModule
{
    private readonly BuildingModelService _service;
    private IReadOnlyList<ColumnCreationRequest> _toCreate = Array.Empty<ColumnCreationRequest>();
    private bool _included;

    /// <summary>Creates the module.</summary>
    /// <param name="service">Validates the requests and calls the host creators.</param>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <c>null</c>.</exception>
    public ColumnImportModule(BuildingModelService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <inheritdoc />
    public string Name => "Columns";

    /// <inheritdoc />
    public string NoteTitle => "Column notes";

    /// <inheritdoc />
    public IReadOnlyList<ImportCategory> Categories { get; } = new[] { ImportCategories.Columns };

    /// <inheritdoc />
    public int Order => 200;

    /// <inheritdoc />
    public bool RegenerateAfter => false;

    /// <inheritdoc />
    public int CountInDrawing(ImportCategory category, DxfCenterLinePipeline.Result drawing) =>
        drawing.Columns.Count;

    /// <inheritdoc />
    public ImportModulePlan Plan(ImportPlanContext context, ImportBag shared)
    {
        ArgumentNullException.ThrowIfNull(context);

        _included = context.IsIncluded(ImportCategories.Columns);
        _toCreate = Array.Empty<ColumnCreationRequest>();
        if (!_included)
            return ImportModulePlan.Nothing;

        ColumnImportOptions options = context.Options.GetOrDefault(new ColumnImportOptions());
        var factory = new ColumnCreationRequestFactory(context.Session)
        {
            CircularColumnTypeKey = options.CircularTypeKey,
            ConvertCircularToSquare = options.ConvertCircularToSquare,
            SquareColumnTypeKey = options.SquareTypeKey,
            FreeformColumnTypeKey = options.FreeformTypeKey
        };

        List<ColumnCreationRequest> requests = factory.FromColumnModels(context.Drawing.Columns)
            .Where(r => r.Type.SectionKind switch
            {
                ColumnSectionKind.Circular => options.IncludeCircular,
                ColumnSectionKind.Freeform => options.IncludeFreeform,
                _ => true
            })
            .ToList();

        if (!context.SkipExisting)
        {
            _toCreate = requests;
            return new ImportModulePlan(requests.Count, new Dictionary<ImportCategory, int>());
        }

        ExistingSplit<ColumnCreationRequest> split =
            ExistingElementMatcher.Split(requests, r => r.Id, context.Existing.FindColumn);
        _toCreate = split.New;
        return new ImportModulePlan(_toCreate.Count,
            new Dictionary<ImportCategory, int> { [ImportCategories.Columns] = split.Existing.Count });
    }

    /// <inheritdoc />
    public ImportModuleResult Create(ImportBag shared)
    {
        IReadOnlyList<ColumnCreationResult> results = _service.CreateColumns(_toCreate);
        return new ImportModuleResult(Name, NoteTitle, _included,
            results.Select(r => new ImportEntry(r.RequestId, r.Success, r.HostElementId, r.Message)).ToList());
    }
}
