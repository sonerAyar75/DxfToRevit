using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Import;
using DxfToRevit.Core.Walls;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Walls;
using netDxf;

namespace DxfToRevit.Revit.Tests.Mapping;

/// <summary>
/// Planner: single entry point for wall and column create plans (before a document is injected).
/// </summary>
public sealed class RevitElementCreatePlannerTests
{
    private readonly RevitElementCreatePlanner _planner = new(3000);

    [Fact]
    public void PlanWallAndColumn_SharedDefaultHeight()
    {
        var wallReq = new WallCreationRequest(
            "W1", new Vector2(0, 0), new Vector2(304.8, 0), 200,
            levelName: "Level 1", heightMm: null);

        var colReq = new ColumnCreationRequest(
            "C1",
            new[]
            {
                new Vector2(0, 0), new Vector2(300, 0),
                new Vector2(300, 300), new Vector2(0, 300)
            },
            new Vector2(150, 150),
            placement: new ElementPlacement("Level 1"));

        WallCreateArgs wall = _planner.PlanWall(wallReq);
        ColumnCreateArgs col = _planner.PlanColumn(colReq);

        Assert.Equal("Level 1", wall.LevelName);
        Assert.Equal("Level 1", col.Placement.LevelName);
        Assert.Equal(wall.HeightFeet, col.Placement.HeightFeet, 10);
        Assert.Equal(1.0, wall.LengthFeet, 10);
    }
}
