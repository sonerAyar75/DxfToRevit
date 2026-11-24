using DxfToRevit.Core.Dxf;
using DxfToRevit.Core.Import;
using DxfToRevit.Core.Walls;

namespace DxfToRevit.Core.Openings;

/// <summary>
/// Doors, windows and empty openings from the marker blocks of the drawing, placed in their host
/// walls (<see cref="HostWallMap"/> of <see cref="WallImportModule"/>). Reports the markers that
/// could not be used and their warnings.
/// </summary>
public sealed class OpeningImportModule : IImportModule
{
    /// <summary>Most issue groups listed per summary section.</summary>
    public const int MaxListedIssueGroups = 8;

    private readonly BuildingModelService _service;
    private IReadOnlyList<OpeningCreationRequest> _toCreate = Array.Empty<OpeningCreationRequest>();
    private IReadOnlyList<ImportReportSection> _sections = Array.Empty<ImportReportSection>();
    private string? _countSuffix;
    private bool _included;

    /// <summary>Creates the module.</summary>
    /// <param name="service">Validates the requests and calls the host creators.</param>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <c>null</c>.</exception>
    public OpeningImportModule(BuildingModelService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <inheritdoc />
    public string Name => "Openings";

    /// <inheritdoc />
    public string NoteTitle => "Opening notes";

    /// <inheritdoc />
    public IReadOnlyList<ImportCategory> Categories { get; } =
        new[] { ImportCategories.Doors, ImportCategories.Windows, ImportCategories.EmptyOpenings };

    /// <inheritdoc />
    public int Order => 300;

    /// <inheritdoc />
    public bool RegenerateAfter => false;

    /// <inheritdoc />
    public int CountInDrawing(ImportCategory category, DxfCenterLinePipeline.Result drawing) =>
        drawing.Openings.Count(o => CategoryOf(o.Definition.Category) == category);

    /// <inheritdoc />
    public ImportModulePlan Plan(ImportPlanContext context, ImportBag shared)
    {
        ArgumentNullException.ThrowIfNull(context);

        _included = Categories.Any(context.IsIncluded);
        _toCreate = Array.Empty<OpeningCreationRequest>();
        _sections = Array.Empty<ImportReportSection>();
        _countSuffix = null;
        if (!_included)
            return ImportModulePlan.Nothing;

        _countSuffix = $"(markers in DXF: {context.Drawing.OpeningMarkerCount})";
        _sections = IssueSections(context.Drawing.OpeningIssues);

        OpeningTypeMap typeMap = context.Options.Get<OpeningImportOptions>()?.TypeMap ?? OpeningTypeMap.Empty;
        List<OpeningCreationRequest> requests = new OpeningCreationRequestFactory(context.Session, typeMap)
            .FromOpeningModels(context.Drawing.Openings)
            .Where(r => context.IsIncluded(CategoryOf(r.Category)))
            .ToList();

        if (!context.SkipExisting)
        {
            _toCreate = requests;
            return new ImportModulePlan(requests.Count, new Dictionary<ImportCategory, int>());
        }

        ExistingSplit<OpeningCreationRequest> split =
            ExistingElementMatcher.Split(requests, r => r.Id, context.Existing.FindOpening);
        _toCreate = split.New;
        Dictionary<ImportCategory, int> skipped = requests
            .Where(r => split.Existing.ContainsKey(r.Id))
            .GroupBy(r => CategoryOf(r.Category))
            .ToDictionary(g => g.Key, g => g.Count());
        return new ImportModulePlan(_toCreate.Count, skipped);
    }

    /// <inheritdoc />
    public ImportModuleResult Create(ImportBag shared)
    {
        ArgumentNullException.ThrowIfNull(shared);

        IReadOnlyDictionary<string, string> hosts = shared.Get<HostWallMap>()?.Ids
                                                    ?? new Dictionary<string, string>();
        IReadOnlyList<OpeningCreationResult> results = _service.CreateOpenings(_toCreate, hosts);
        return new ImportModuleResult(Name, NoteTitle, _included,
            results.Select(r => new ImportEntry(r.RequestId, r.Success, r.HostElementId, r.Message)).ToList(),
            _sections, _countSuffix);
    }

    /// <summary>The import category of an opening category.</summary>
    /// <param name="category">Opening category.</param>
    public static ImportCategory CategoryOf(OpeningCategory category) => category switch
    {
        OpeningCategory.Door => ImportCategories.Doors,
        OpeningCategory.Window => ImportCategories.Windows,
        _ => ImportCategories.EmptyOpenings
    };

    /// <summary>The marker errors and warnings as summary sections, grouped by issue code with up to five DXF locations each.</summary>
    private static IReadOnlyList<ImportReportSection> IssueSections(IReadOnlyList<OpeningIssue> issues)
    {
        var sections = new List<ImportReportSection>();
        AddSection(sections, "Opening markers not imported", issues.Where(i => i.Severity == OpeningIssueSeverity.Error).ToList(), isError: true);
        AddSection(sections, "Opening warnings", issues.Where(i => i.Severity == OpeningIssueSeverity.Warning).ToList(), isError: false);
        return sections;
    }

    private static void AddSection(List<ImportReportSection> sections, string title, IReadOnlyList<OpeningIssue> issues, bool isError)
    {
        if (issues.Count == 0)
            return;

        List<string> lines = issues.GroupBy(i => i.Code).Take(MaxListedIssueGroups).Select(group =>
        {
            string where = string.Join(", ", group.Take(5).Select(i => $"({i.LocationMm.X:F0}, {i.LocationMm.Y:F0})"));
            string more = group.Count() > 5 ? $", +{group.Count() - 5} more" : string.Empty;
            return $"{group.Key} at {where}{more}: {group.First().Message}";
        }).ToList();

        sections.Add(new ImportReportSection(title, issues.Count, lines, isError));
    }
}
