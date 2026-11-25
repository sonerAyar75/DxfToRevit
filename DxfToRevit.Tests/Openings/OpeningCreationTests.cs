using DxfToRevit.Core.Import;
using DxfToRevit.Core.InMemory;
using DxfToRevit.Core.Openings;
using DxfToRevit.Core.Walls;
using netDxf;
using static DxfToRevit.Tests.Openings.OpeningTestFixtures;

namespace DxfToRevit.Tests.Openings;

/// <summary>Type map, map file, opening requests and creation through <see cref="BuildingModelService"/>.</summary>
public sealed class OpeningCreationTests
{
    private static readonly OpeningTypeMap Map = new(new[]
    {
        new OpeningTypeMapEntry(OpeningCategory.Door, "1", "M_Single-Flush::0915 x 2134mm"),
        new OpeningTypeMapEntry(OpeningCategory.Door, "*", "M_Single-Flush::0864 x 2032mm"),
        new OpeningTypeMapEntry(OpeningCategory.Window, "1", "M_Fixed::0915 x 1220mm")
    });

    /// <summary>Exact codes win, <c>*</c> covers the other codes, a category without entries resolves to nothing.</summary>
    [Fact]
    public void Resolve_UsesExactCodeThenDefault()
    {
        Assert.Equal("M_Single-Flush::0915 x 2134mm", Map.Resolve(OpeningCategory.Door, "1"));
        Assert.Equal("M_Single-Flush::0864 x 2032mm", Map.Resolve(OpeningCategory.Door, "7"));
        Assert.Equal("M_Fixed::0915 x 1220mm", Map.Resolve(OpeningCategory.Window, "1"));
        Assert.Null(Map.Resolve(OpeningCategory.Window, "2"));
    }

    /// <summary>Invalid lines are reported with their line numbers; valid lines are kept.</summary>
    [Fact]
    public void Parse_ReportsInvalidLinesWithLineNumbers()
    {
        const string text = "Category;Code;RevitType\n" +
                            "# comment\n" +
                            "Door;1;M_Single-Flush::0915 x 2134mm\n" +
                            "Roof;1;Something\n" +
                            "Window;2\n" +
                            "Window\t3\tM_Fixed::0915 x 1220mm\n";

        OpeningTypeMapReadResult result = OpeningTypeMapFile.Parse(new StringReader(text));

        Assert.Equal(2, result.Map.Entries.Count);
        Assert.Equal(2, result.Errors.Count);
        Assert.StartsWith("Line 4:", result.Errors[0]);
        Assert.StartsWith("Line 5:", result.Errors[1]);
    }

    /// <summary>A written map file reads back to the same entries.</summary>
    [Fact]
    public void WriteThenRead_RoundTrips()
    {
        string path = Path.Combine(Path.GetTempPath(), $"DxfToRevitTypeMap_{Guid.NewGuid():N}.txt");
        try
        {
            OpeningTypeMapFile.Write(path, Map);
            OpeningTypeMapReadResult result = OpeningTypeMapFile.Read(path);

            Assert.Empty(result.Errors);
            Assert.Equal(Map.Entries, result.Map.Entries);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// End to end in memory: walls are created, then doors (mapped type), windows without a mapping
    /// fail validation, and empty openings need no type.
    /// </summary>
    [Fact]
    public void CreateOpenings_InMemory_CreatesMappedOpeningsInTheirWalls()
    {
        WallSolid2D wall = HorizontalWall();
        var markers = new[]
        {
            Marker("door", 1000, 0, 900, 200, hinge: new Vector2(560, 90),
                attributes: Attributes(("TYPE", "1"), ("HEIGHT", "210")), handle: "D"),
            Marker("window", 2500, 0, 1200, 200,
                attributes: Attributes(("TYPE", "9"), ("HEIGHT", "150"), ("SILL_HEIGHT", "90")), handle: "W"),
            Marker("opening", 4000, 0, 800, 200,
                attributes: Attributes(("TYPE", "-"), ("HEIGHT", "210"), ("SILL_HEIGHT", "0")), handle: "O")
        };
        OpeningInterpretation interpretation = new OpeningInterpreter().Interpret(markers, new[] { wall });

        var session = new ImportSessionOptions(baseLevelName: "Level 1");
        var model = new InMemoryBuildingModel();
        var service = new BuildingModelService(
            new InMemoryWallCreator(model),
            openingCreator: new InMemoryOpeningCreator(model));

        IReadOnlyList<WallCreationResult> walls =
            service.CreateWalls(new WallCreationRequestFactory(session).FromWallSolids(new[] { wall }));
        var hostIds = walls.Where(r => r.Success).ToDictionary(r => r.RequestId, r => r.HostElementId!);

        IReadOnlyList<OpeningCreationRequest> requests =
            new OpeningCreationRequestFactory(session, Map).FromOpeningModels(interpretation.Openings);
        IReadOnlyList<OpeningCreationResult> results = service.CreateOpenings(requests, hostIds);

        Assert.Equal(3, requests.Count);
        Assert.All(requests, r => Assert.Equal("W001", r.HostWallRequestId));
        Assert.Equal("M_Single-Flush::0915 x 2134mm", requests[0].TypeKey);
        Assert.Null(requests[1].TypeKey);
        Assert.Null(requests[2].TypeKey);
        Assert.Equal(0, requests[2].SillHeightMm, 3);
        Assert.Equal(900, requests[1].SillHeightMm, 3);

        Assert.True(results[0].Success);
        Assert.False(results[1].Success);
        Assert.Contains("No Revit type is mapped", results[1].Message);
        Assert.True(results[2].Success);
        Assert.Equal(2, model.Openings.Count);
        Assert.All(model.Openings, o => Assert.Equal(walls[0].HostElementId, o.HostWallElementId));
    }

    /// <summary>An opening whose host wall was not created fails with an explanatory message.</summary>
    [Fact]
    public void CreateOpenings_HostWallMissing_Fails()
    {
        var marker = Marker("opening", 1000, 0, 800, 200,
            attributes: Attributes(("TYPE", "-"), ("HEIGHT", "210"), ("SILL_HEIGHT", "0")));
        OpeningInterpretation interpretation = new OpeningInterpreter().Interpret(new[] { marker }, new[] { HorizontalWall() });
        var model = new InMemoryBuildingModel();
        var service = new BuildingModelService(new InMemoryWallCreator(model), openingCreator: new InMemoryOpeningCreator(model));

        IReadOnlyList<OpeningCreationResult> results = service.CreateOpenings(
            new OpeningCreationRequestFactory().FromOpeningModels(interpretation.Openings),
            new Dictionary<string, string>());

        OpeningCreationResult result = Assert.Single(results);
        Assert.False(result.Success);
        Assert.Contains("was not created", result.Message);
    }

    /// <summary>Without an opening creator every request fails.</summary>
    [Fact]
    public void CreateOpenings_NoCreator_Fails()
    {
        var marker = Marker("opening", 1000, 0, 800, 200,
            attributes: Attributes(("TYPE", "-"), ("HEIGHT", "210"), ("SILL_HEIGHT", "0")));
        OpeningInterpretation interpretation = new OpeningInterpreter().Interpret(new[] { marker }, new[] { HorizontalWall() });
        var service = new BuildingModelService(new InMemoryWallCreator(new InMemoryBuildingModel()));

        IReadOnlyList<OpeningCreationResult> results = service.CreateOpenings(
            new OpeningCreationRequestFactory().FromOpeningModels(interpretation.Openings),
            new Dictionary<string, string> { ["W001"] = "W-0001" });

        Assert.False(Assert.Single(results).Success);
    }
}
