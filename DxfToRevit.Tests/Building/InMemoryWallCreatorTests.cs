using DxfToRevit.Core.Building;
using DxfToRevit.Core.Building.InMemory;
using netDxf;
using Xunit;

namespace DxfToRevit.Tests.Building;

/// <summary>
/// InMemoryWallCreator: unit tests of the Basic Wall flow without Revit.
/// </summary>
public sealed class InMemoryWallCreatorTests
{
    /// <summary>
    /// Two valid requests → two records, Success = true, model.Walls.Count = 2.
    /// </summary>
    [Fact]
    public void CreateWalls_TwoValidRequests_AddsTwoRecords()
    {
        var model = new InMemoryBuildingModel();
        var creator = new InMemoryWallCreator(model);

        var requests = new[]
        {
            new WallCreationRequest("W001", new Vector2(0, 0), new Vector2(1000, 0), 200),
            new WallCreationRequest("W002", new Vector2(0, 0), new Vector2(0, 500), 150)
        };

        IReadOnlyList<WallCreationResult> results = creator.CreateWalls(requests);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
        Assert.Equal(2, model.Walls.Count);
        Assert.Equal(200, model.Walls[0].Request.ThicknessMm);
        Assert.Equal(150, model.Walls[1].Request.ThicknessMm);
        Assert.Equal("W-0001", model.Walls[0].HostElementId);
        Assert.Equal("W-0002", model.Walls[1].HostElementId);
    }

    /// <summary>Zero length → fail; nothing is added to the model.</summary>
    [Fact]
    public void CreateWall_Degenerate_ReturnsFailAndDoesNotAdd()
    {
        var model = new InMemoryBuildingModel();
        var creator = new InMemoryWallCreator(model);

        var req = new WallCreationRequest("W001", new Vector2(0, 0), new Vector2(0, 0), 200);
        WallCreationResult result = creator.CreateWall(req);

        Assert.False(result.Success);
        Assert.Empty(model.Walls);
    }
}
