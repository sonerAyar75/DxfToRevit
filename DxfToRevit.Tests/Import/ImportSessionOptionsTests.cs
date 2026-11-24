using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Import;
using DxfToRevit.Core.Walls;
using netDxf;

namespace DxfToRevit.Tests.Import;

/// <summary>
/// ImportSessionOptions: Level, height, thickness→type map → request factory.
/// </summary>
public sealed class ImportSessionOptionsTests
{
    [Fact]
    public void ResolveWallTypeKey_MatchesThicknessMap()
    {
        var session = new ImportSessionOptions(
            wallTypeByThicknessMm: new Dictionary<double, string>
            {
                [150] = "Basic Wall: Generic - 150mm",
                [200] = "Basic Wall: Generic - 200mm"
            },
            defaultWallTypeKey: "Basic Wall");

        Assert.Equal("Basic Wall: Generic - 200mm", session.ResolveWallTypeKey(200));
        Assert.Equal("Basic Wall: Generic - 150mm", session.ResolveWallTypeKey(150.2));
        Assert.Equal("Basic Wall", session.ResolveWallTypeKey(300));
    }

    [Fact]
    public void ForLevel_SetsPlacementOnWallRequests()
    {
        var session = ImportSessionOptions.ForLevel("Level 1", heightMm: 3200);
        var factory = new WallCreationRequestFactory(session);
        var cl = new LineSegment2D(new Vector2(0, 0), new Vector2(1000, 0));

        var requests = factory.FromCenterLines(new[] { cl }, new[] { 200.0 });

        Assert.Single(requests);
        Assert.Equal("Level 1", requests[0].LevelName);
        Assert.Equal(3200, requests[0].HeightMm);
        Assert.Equal(200, requests[0].ThicknessMm);
    }

    [Fact]
    public void WallFactory_UsesMappedTypeKey()
    {
        var session = new ImportSessionOptions(
            baseLevelName: "Level 2",
            wallTypeByThicknessMm: new Dictionary<double, string> { [200] = "Wall-200" });
        var factory = new WallCreationRequestFactory(session);
        var solid = new WallFootprintBuilder().Build(
            new LineSegment2D(new Vector2(0, 0), new Vector2(500, 0)), 200);

        var requests = factory.FromWallSolids(new[] { solid });

        Assert.Equal("Wall-200", requests[0].WallTypeKey);
        Assert.Equal("Level 2", requests[0].LevelName);
    }

    [Fact]
    public void ColumnFactory_UsesSessionLevelAndColumnType()
    {
        var session = ImportSessionOptions.ForLevel("Level 1", 3000);
        session = new ImportSessionOptions(
            baseLevelName: "Level 1",
            defaultHeightMm: 3000,
            defaultColumnTypeKey: "M_Concrete-Rectangular-Column");

        var factory = new ColumnCreationRequestFactory(session);
        var model = new ColumnModel(
            "C001", "COLUMN", "H1",
            new[]
            {
                new Vector2(0, 0), new Vector2(300, 0),
                new Vector2(300, 300), new Vector2(0, 300)
            });

        var requests = factory.FromColumnModels(new[] { model });

        Assert.Equal("Level 1", requests[0].Placement.LevelName);
        Assert.Equal(3000, requests[0].Placement.HeightMm);
        Assert.Equal("M_Concrete-Rectangular-Column", requests[0].Type.TypeKey);
        Assert.True(requests[0].Type.IsStructural);
    }


    [Fact]
    public void CreatePlacement_WithTopLevel_CopiesTopLevelName()
    {
        var session = new ImportSessionOptions(
            baseLevelName: "Level 1",
            defaultHeightMm: 3000,
            topLevelName: "Level 3");

        ElementPlacement placement = session.CreatePlacement();

        Assert.Equal("Level 1", placement.LevelName);
        Assert.Equal("Level 3", placement.TopLevelName);
        Assert.Equal(3000, placement.HeightMm);
    }

    [Fact]
    public void CreatePlacement_WithoutTopLevel_UsesUnconnected()
    {
        var session = new ImportSessionOptions(
            baseLevelName: "Level 1",
            defaultHeightMm: 3500);

        ElementPlacement placement = session.CreatePlacement();

        Assert.Null(placement.TopLevelName);
        Assert.Equal(3500, placement.HeightMm);
    }

}
