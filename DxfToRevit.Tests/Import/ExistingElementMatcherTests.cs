using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Import;
using DxfToRevit.Core.Openings;
using DxfToRevit.Core.Walls;
using netDxf;

namespace DxfToRevit.Tests.Import;

/// <summary>Finding the model elements that an import would duplicate.</summary>
public sealed class ExistingElementMatcherTests
{
    private static WallCreationRequest Wall(double x1, double x2, double thickness = 200, string level = "Level 1") =>
        new("W001", new Vector2(x1, 0), new Vector2(x2, 0), thickness, levelName: level);

    private static ColumnCreationRequest Column(double x, double y, string level = "Level 1")
    {
        var outline = new[] { new Vector2(x - 200, y - 200), new Vector2(x + 200, y - 200), new Vector2(x + 200, y + 200), new Vector2(x - 200, y + 200) };
        return new ColumnCreationRequest("C001", outline, new Vector2(x, y), placement: new ElementPlacement(levelName: level));
    }

    private static OpeningCreationRequest Opening(OpeningCategory category, double x, string level = "Level 1") =>
        new(
            id: "D001",
            category: category,
            creationMode: category == OpeningCategory.EmptyOpening ? OpeningCreationMode.WallOpening : OpeningCreationMode.HostedFamily,
            typeKey: category == OpeningCategory.EmptyOpening ? null : "F::T",
            typeCode: "1",
            hostWallRequestId: "W001",
            locationMm: new Vector2(x, 0),
            wallDirection: new Vector2(1, 0),
            widthMm: 900,
            heightMm: 2100,
            sillHeightMm: 0,
            handing: null,
            placement: new ElementPlacement(levelName: level));

    private static ExistingElementMatcher Matcher(
        ExistingWall[]? walls = null, ExistingColumn[]? columns = null, ExistingOpening[]? openings = null) =>
        new(new ExistingElements(
            walls ?? Array.Empty<ExistingWall>(), columns ?? Array.Empty<ExistingColumn>(), openings ?? Array.Empty<ExistingOpening>()));

    [Fact]
    public void FindWall_ExistingWallCoversRequest_ReturnsItsId()
    {
        var matcher = Matcher(walls: new[] { new ExistingWall("11", "Level 1", new Vector2(0, 0), new Vector2(5000, 3), 200) });

        Assert.Equal("11", matcher.FindWall(Wall(1000, 4000)));
        Assert.Equal("11", matcher.FindWall(Wall(0, 5000)));
    }

    [Theory]
    [InlineData(0, 6000, 200, "Level 1")]   // lengthened in the drawing
    [InlineData(0, 5000, 150, "Level 1")]   // other thickness
    [InlineData(0, 5000, 200, "Level 2")]   // other level
    public void FindWall_NoCoveringWall_ReturnsNull(double x1, double x2, double thickness, string level)
    {
        var matcher = Matcher(walls: new[] { new ExistingWall("11", "Level 1", new Vector2(0, 0), new Vector2(5000, 0), 200) });

        Assert.Null(matcher.FindWall(Wall(x1, x2, thickness, level)));
    }

    [Fact]
    public void FindColumn_SameLocationAndLevel()
    {
        var matcher = Matcher(columns: new[] { new ExistingColumn("21", "level 1", new Vector2(1005, 2000)) });

        Assert.Equal("21", matcher.FindColumn(Column(1000, 2000)));
        Assert.Null(matcher.FindColumn(Column(1100, 2000)));
        Assert.Null(matcher.FindColumn(Column(1000, 2000, "Level 2")));
    }

    [Fact]
    public void FindOpening_RequiresSameCategory()
    {
        var matcher = Matcher(openings: new[] { new ExistingOpening("31", OpeningCategory.Door, "Level 1", new Vector2(1500, 0)) });

        Assert.Equal("31", matcher.FindOpening(Opening(OpeningCategory.Door, 1500)));
        Assert.Null(matcher.FindOpening(Opening(OpeningCategory.Window, 1500)));
        Assert.Null(matcher.FindOpening(Opening(OpeningCategory.Door, 2500)));
    }

    [Fact]
    public void Split_SeparatesNewAndExisting()
    {
        var matcher = Matcher(columns: new[] { new ExistingColumn("21", "Level 1", new Vector2(0, 0)) });
        var requests = new[] { Column(0, 0), Column(3000, 0) };

        ExistingSplit<ColumnCreationRequest> split =
            ExistingElementMatcher.Split(requests, r => r.Id + r.CentroidMm.X, matcher.FindColumn);

        Assert.Equal(3000, Assert.Single(split.New).CentroidMm.X);
        Assert.Equal("21", split.Existing["C0010"]);
    }
}
