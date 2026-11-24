using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Dxf;
using DxfToRevit.Core.Import;
using DxfToRevit.Core.InMemory;
using DxfToRevit.Core.Openings;
using DxfToRevit.Core.Walls;
using static DxfToRevit.Tests.Openings.OpeningTestFixtures;

namespace DxfToRevit.Tests.Import;

/// <summary>
/// The modular import with the in-memory creators, on <c>OpeningsDoors.dxf</c> (walls and three
/// doors): planning, category choice, skipping existing elements and hosting doors.
/// </summary>
public sealed class ImportCoordinatorTests
{
    private static readonly DxfCenterLinePipeline.Result Drawing =
        new DxfCenterLinePipeline().Analyze(TestDataPath("OpeningsDoors.dxf"));

    private static readonly ImportSessionOptions Session = new(baseLevelName: "Level 1");

    private static readonly string[] AllCategories =
    {
        ImportCategories.Walls.Key, ImportCategories.Columns.Key, ImportCategories.Doors.Key,
        ImportCategories.Windows.Key, ImportCategories.EmptyOpenings.Key
    };

    /// <summary>A coordinator writing into <paramref name="model"/> (a new model when <c>null</c>), like a second import into the same document.</summary>
    private static (ImportCoordinator Coordinator, InMemoryBuildingModel Model) Coordinator(InMemoryBuildingModel? model = null)
    {
        model ??= new InMemoryBuildingModel();
        var service = new BuildingModelService(
            new InMemoryWallCreator(model), new InMemoryColumnCreator(model),
            openingCreator: new InMemoryOpeningCreator(model));
        IImportModule[] modules =
        {
            new OpeningImportModule(service), new WallImportModule(service), new ColumnImportModule(service)
        };
        return (new ImportCoordinator(modules), model);
    }

    /// <summary>The elements of an in-memory model as the host would report them.</summary>
    private static ExistingElements Snapshot(InMemoryBuildingModel model, bool withColumns = true, bool withOpenings = true) =>
        new(
            model.Walls.Select(w => new ExistingWall(w.HostElementId, "Level 1", w.Request.StartMm, w.Request.EndMm, w.Request.Type.ThicknessMm)).ToList(),
            withColumns
                ? model.Columns.Select(c => new ExistingColumn(c.HostElementId, "Level 1", c.Request.CentroidMm)).ToList()
                : Array.Empty<ExistingColumn>(),
            withOpenings
                ? model.Openings.Select(o => new ExistingOpening(o.HostElementId, o.Request.Category, "Level 1", o.Request.LocationMm)).ToList()
                : Array.Empty<ExistingOpening>());

    private static ImportPlanContext Context(
        IEnumerable<string> categories, bool skipExisting = true, ExistingElements? existing = null) =>
        new(Drawing, Session, new ExistingElementMatcher(existing ?? ExistingElements.Empty), categories, skipExisting,
            OptionsWithTypes());

    private static ImportBag OptionsWithTypes()
    {
        var options = new ImportBag();
        options.Set(new OpeningImportOptions(new OpeningTypeMap(new[]
        {
            new OpeningTypeMapEntry(OpeningCategory.Door, OpeningTypeMap.DefaultCode, "Door::900")
        })));
        return options;
    }

    [Fact]
    public void Modules_RunInOrder_WallsFirst()
    {
        (ImportCoordinator coordinator, _) = Coordinator();

        Assert.Equal(new[] { "Walls", "Columns", "Openings" }, coordinator.Modules.Select(m => m.Name));
        Assert.Equal(AllCategories, coordinator.Categories.Select(c => c.Key));
    }

    [Fact]
    public void Create_AllCategories_DoorsAreHostedInTheNewWalls()
    {
        (ImportCoordinator coordinator, InMemoryBuildingModel model) = Coordinator();
        int regenerations = 0;

        coordinator.Plan(Context(AllCategories));
        IReadOnlyList<ImportModuleResult> results = coordinator.Create(() => regenerations++);

        Assert.Equal(1, regenerations);
        Assert.NotEmpty(model.Walls);
        Assert.Equal(3, model.Openings.Count);
        HashSet<string> wallIds = model.Walls.Select(w => w.HostElementId).ToHashSet();
        Assert.All(model.Openings, o => Assert.Contains(o.HostWallElementId, wallIds));
        Assert.All(results, r => Assert.True(r.IsComplete, r.Name));
        Assert.Empty(coordinator.Skipped);
    }

    [Fact]
    public void Create_DoorsOnly_AreHostedInTheWallsAlreadyInTheModel()
    {
        // First import: walls and columns. Second import into the same model: doors only.
        (ImportCoordinator first, InMemoryBuildingModel model) = Coordinator();
        first.Plan(Context(new[] { ImportCategories.Walls.Key, ImportCategories.Columns.Key }));
        first.Create(() => { });
        int walls = model.Walls.Count;
        HashSet<string> wallIds = model.Walls.Select(w => w.HostElementId).ToHashSet();

        (ImportCoordinator second, _) = Coordinator(model);
        second.Plan(Context(new[] { ImportCategories.Doors.Key }, existing: Snapshot(model)));
        IReadOnlyList<ImportModuleResult> results = second.Create(() => { });

        Assert.Equal(walls, model.Walls.Count);
        Assert.Equal(3, model.Openings.Count);
        Assert.All(model.Openings, o => Assert.Contains(o.HostWallElementId, wallIds));
        Assert.False(results.Single(r => r.Name == "Walls").Included);
        Assert.True(results.Single(r => r.Name == "Openings").IsComplete);
    }

    [Fact]
    public void Plan_SkipExisting_CountsWhatIsAlreadyInTheModel()
    {
        (ImportCoordinator first, InMemoryBuildingModel model) = Coordinator();
        first.Plan(Context(AllCategories));
        first.Create(() => { });
        int walls = model.Walls.Count, columns = model.Columns.Count;

        (ImportCoordinator second, _) = Coordinator(model);
        second.Plan(Context(AllCategories, existing: Snapshot(model)));
        second.Create(() => { });

        Assert.Equal(0, second.PlannedCount);
        Assert.Equal(walls, second.Skipped[ImportCategories.Walls]);
        Assert.Equal(3, second.Skipped[ImportCategories.Doors]);
        Assert.Equal(walls, model.Walls.Count);
        Assert.Equal(columns, model.Columns.Count);
        Assert.Equal(3, model.Openings.Count);
    }

    [Fact]
    public void Plan_WithoutSkipping_CreatesEverythingAgain()
    {
        var existing = new ExistingElements(
            Array.Empty<ExistingWall>(), Array.Empty<ExistingColumn>(),
            Drawing.Openings.Select((o, i) => new ExistingOpening($"E{i}", o.Definition.Category, "Level 1", o.Placement.CenterOnWallMm)).ToList());
        (ImportCoordinator coordinator, InMemoryBuildingModel model) = Coordinator();

        coordinator.Plan(Context(AllCategories, skipExisting: false, existing: existing));
        coordinator.Create(() => { });

        Assert.Equal(3, model.Openings.Count);
        Assert.Empty(coordinator.Skipped);
    }

    [Fact]
    public void Plan_UnchosenCategory_IsNotCreated()
    {
        (ImportCoordinator coordinator, InMemoryBuildingModel model) = Coordinator();

        coordinator.Plan(Context(new[] { ImportCategories.Walls.Key }));
        IReadOnlyList<ImportModuleResult> results = coordinator.Create(() => { });

        Assert.NotEmpty(model.Walls);
        Assert.Empty(model.Openings);
        Assert.False(results.Single(r => r.Name == "Openings").Included);
        Assert.Empty(results.Single(r => r.Name == "Openings").Sections);
    }

    [Fact]
    public void CountInDrawing_PerCategory()
    {
        (ImportCoordinator coordinator, _) = Coordinator();
        IImportModule openings = coordinator.Modules.Single(m => m.Name == "Openings");

        Assert.Equal(3, openings.CountInDrawing(ImportCategories.Doors, Drawing));
        Assert.Equal(0, openings.CountInDrawing(ImportCategories.Windows, Drawing));
    }

    [Fact]
    public void WithCommitOutcome_RemovedElementFailsAndNoteIsAttached()
    {
        var result = new ImportModuleResult("Openings", "Opening notes", true, new[]
        {
            new ImportEntry("D001", true, "11", null),
            new ImportEntry("D002", true, "12", "Placed.")
        });

        ImportModuleResult after = result.WithCommitOutcome(
            id => id == "11" ? "Removed." : null,
            new Dictionary<string, string> { ["D002"] = "Cannot flip." });

        Assert.Equal(new ImportEntry("D001", false, null, "Removed."), after.Entries[0]);
        Assert.Equal("Placed. Cannot flip.", after.Entries[1].Message);
        Assert.Equal(1, after.Created);
    }

    [Fact]
    public void Constructor_DuplicateCategory_Throws()
    {
        var service = new BuildingModelService(new InMemoryWallCreator(new InMemoryBuildingModel()));

        Assert.Throws<ArgumentException>(() =>
            new ImportCoordinator(new IImportModule[] { new WallImportModule(service), new WallImportModule(service) }));
    }
}
