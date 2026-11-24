using DxfToRevit.Core.Building;
using DxfToRevit.Core.Openings;
using DxfToRevit.Core.Services;
using netDxf;
using Xunit;
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
    /// The editor lists the DXF door/window codes first (with their mapped types), then the other
    /// map rows; empty openings are not listed because they need no type.
    /// </summary>
    [Fact]
    public void Rows_ListDxfCodesFirstThenOtherMapEntries()
    {
        var map = new OpeningTypeMap(new[]
        {
            new OpeningTypeMapEntry(OpeningCategory.Door, "*", "F::Default"),
            new OpeningTypeMapEntry(OpeningCategory.Door, "2", "F::Two"),
            new OpeningTypeMapEntry(OpeningCategory.Window, "Z", "W::Zed")
        });

        IReadOnlyList<OpeningTypeMapRow> rows = OpeningTypeMapRows.Build(map, OpeningCodeSummary.Build(Openings()));

        Assert.Equal(
            new[] { "Door|2|F::Two|2", "Door|10||1", "Window|A||1", "Door|*|F::Default|0", "Window|Z|W::Zed|0", "Window|*||0" },
            rows.Select(r => $"{r.Category}|{r.Code}|{r.TypeKey}|{r.CountInDxf}"));
        Assert.True(rows[0].FromDxf);
        Assert.False(rows[3].FromDxf);
        Assert.Equal("900x2100", rows[0].Sizes);
    }

    /// <summary>Without a map, the editor still offers an empty <c>*</c> row for doors and windows.</summary>
    [Fact]
    public void Rows_EmptyMap_OfferDefaultRowsForDoorsAndWindows()
    {
        IReadOnlyList<OpeningTypeMapRow> rows = OpeningTypeMapRows.Build(OpeningTypeMap.Empty, Array.Empty<OpeningCodeUsage>());

        Assert.Equal(new[] { "Door|*|", "Window|*|" }, rows.Select(r => $"{r.Category}|{r.Code}|{r.TypeKey}"));
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
