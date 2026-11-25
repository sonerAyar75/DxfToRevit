using DxfToRevit.Core.Dxf;
using DxfToRevit.Core.Openings;
using netDxf;
using static DxfToRevit.Tests.Openings.OpeningTestFixtures;

namespace DxfToRevit.Tests.Openings;

/// <summary>Listing the codes found in a DXF and building the type map editor rows from them.</summary>
public sealed class OpeningCodeSummaryTests
{
    private static IReadOnlyList<OpeningModel> Openings()
    {
        var markers = new[]
        {
            Marker("door", 500, 0, 800, 200, hinge: new Vector2(110, 90), attributes: Attributes(("TYPE", "10"), ("HEIGHT", "210")), handle: "1"),
            Marker("door", 1500, 0, 900, 200, hinge: new Vector2(1060, 90), attributes: Attributes(("TYPE", "2"), ("HEIGHT", "210")), handle: "2"),
            Marker("door", 2500, 0, 900, 200, hinge: new Vector2(2060, 90), attributes: Attributes(("TYPE", "2"), ("HEIGHT", "210")), handle: "3"),
            Marker("window", 3700, 0, 1200, 200, attributes: Attributes(("TYPE", "A"), ("HEIGHT", "150"), ("SILL_HEIGHT", "90")), handle: "4"),
            Marker("opening", 5000, 0, 1000, 200, attributes: Attributes(("HEIGHT", "210"), ("SILL_HEIGHT", "0")), handle: "5")
        };
        return new OpeningInterpreter().Interpret(markers, new[] { HorizontalWall(lengthMm: 6000) }).Openings;
    }

    /// <summary>Codes are grouped per category, ordered numerically, with counts and distinct sizes.</summary>
    [Fact]
    public void Build_GroupsByCategoryAndCode()
    {
        IReadOnlyList<OpeningCodeUsage> usages = OpeningCodeSummary.Build(Openings());

        Assert.Equal(4, usages.Count);
        Assert.Equal((OpeningCategory.Door, "2", 2), (usages[0].Category, usages[0].Code, usages[0].Count));
        Assert.Equal(new[] { "900x2100" }, usages[0].Sizes);
        Assert.Equal((OpeningCategory.Door, "10", 1), (usages[1].Category, usages[1].Code, usages[1].Count));
        Assert.Equal((OpeningCategory.Window, "A", 1), (usages[2].Category, usages[2].Code, usages[2].Count));
        Assert.Equal(new[] { "1200x1500" }, usages[2].Sizes);
        Assert.Equal((OpeningCategory.EmptyOpening, "", 1), (usages[3].Category, usages[3].Code, usages[3].Count));
    }

    /// <summary>
    /// The editor lists only the DXF door/window codes, with their mapped types; map entries of other
    /// codes and empty openings are not listed.
    /// </summary>
    [Fact]
    public void Rows_ListOnlyDxfCodes()
    {
        var map = new OpeningTypeMap(new[]
        {
            new OpeningTypeMapEntry(OpeningCategory.Door, "*", "F::Default"),
            new OpeningTypeMapEntry(OpeningCategory.Door, "2", "F::Two"),
            new OpeningTypeMapEntry(OpeningCategory.Window, "Z", "W::Zed")
        });

        IReadOnlyList<OpeningTypeMapRow> rows = OpeningTypeMapRows.Build(map, OpeningCodeSummary.Build(Openings()));

        Assert.Equal(
            new[] { "Door|2|F::Two|2", "Door|10||1", "Window|A||1" },
            rows.Select(r => $"{r.Category}|{r.Code}|{r.TypeKey}|{r.CountInDxf}"));
        Assert.All(rows, r => Assert.True(r.FromDxf));
        Assert.Equal("900x2100", rows[0].Sizes);
    }

    /// <summary>Without a DXF the editor has no rows.</summary>
    [Fact]
    public void Rows_NoDxf_AreEmpty()
    {
        var map = new OpeningTypeMap(new[] { new OpeningTypeMapEntry(OpeningCategory.Door, "*", "F::Default") });

        Assert.Empty(OpeningTypeMapRows.Build(map, Array.Empty<OpeningCodeUsage>()));
    }

    /// <summary>Blocks without a TYPE attribute are listed with the <c>*</c> entry, which is what resolves them.</summary>
    [Fact]
    public void Rows_NoTypeCode_ShowsDefaultEntry()
    {
        var map = new OpeningTypeMap(new[] { new OpeningTypeMapEntry(OpeningCategory.Window, "*", "W::Default") });
        var usages = new[] { new OpeningCodeUsage(OpeningCategory.Window, "", 2, new[] { "1200x1500" }) };

        OpeningTypeMapRow row = Assert.Single(OpeningTypeMapRows.Build(map, usages));

        Assert.Equal("W::Default", row.TypeKey);
        Assert.Equal("*", OpeningTypeMapRows.MapCode(row.Code));
    }

    /// <summary>
    /// Editing keeps the entries of codes that are not in the DXF and replaces the listed ones; a
    /// listed row left empty removes its entry.
    /// </summary>
    [Fact]
    public void Merge_KeepsUnlistedEntriesAndReplacesListedOnes()
    {
        var previous = new OpeningTypeMap(new[]
        {
            new OpeningTypeMapEntry(OpeningCategory.Door, "2", "F::Old"),
            new OpeningTypeMapEntry(OpeningCategory.Door, "10", "F::Ten"),
            new OpeningTypeMapEntry(OpeningCategory.Window, "Z", "W::Zed")
        });
        var usages = new[]
        {
            new OpeningCodeUsage(OpeningCategory.Door, "2", 1, Array.Empty<string>()),
            new OpeningCodeUsage(OpeningCategory.Door, "10", 1, Array.Empty<string>())
        };

        OpeningTypeMap merged = OpeningTypeMapRows.Merge(previous, usages,
            new[] { new OpeningTypeMapEntry(OpeningCategory.Door, "2", "F::New") });

        Assert.Equal("F::New", merged.Resolve(OpeningCategory.Door, "2"));
        Assert.Null(merged.Resolve(OpeningCategory.Door, "10"));
        Assert.Equal("W::Zed", merged.Resolve(OpeningCategory.Window, "Z"));
    }

    /// <summary>A code is unmapped only when neither its own row nor the <c>*</c> row of its category gives a type.</summary>
    [Fact]
    public void Unmapped_ConsidersDefaultRow()
    {
        IReadOnlyList<OpeningCodeUsage> usages = OpeningCodeSummary.Build(Openings());
        var map = new OpeningTypeMap(new[] { new OpeningTypeMapEntry(OpeningCategory.Door, "*", "F::Default") });

        IReadOnlyList<OpeningCodeUsage> unmapped = OpeningTypeMapRows.Unmapped(map, usages);

        OpeningCodeUsage only = Assert.Single(unmapped);
        Assert.Equal((OpeningCategory.Window, "A"), (only.Category, only.Code));
        Assert.Equal(3, OpeningTypeMapRows.Unmapped(OpeningTypeMap.Empty, usages).Count);
    }

    /// <summary>The sample drawing lists its door codes for mapping.</summary>
    [Fact]
    public void Build_SampleFile_ListsDoorCodes()
    {
        DxfCenterLinePipeline.Result result = new DxfCenterLinePipeline().Analyze(TestDataPath("OpeningsDoors.dxf"));

        IReadOnlyList<OpeningCodeUsage> usages = OpeningCodeSummary.Build(result.Openings);

        Assert.All(usages, u => Assert.Equal(OpeningCategory.Door, u.Category));
        Assert.Equal(3, usages.Sum(u => u.Count));
    }
}
