using DxfToRevit.Core.Import;
using DxfToRevit.Core.InMemory;
using DxfToRevit.Core.Walls;
using netDxf;

namespace DxfToRevit.Tests.Import;

/// <summary>
/// BuildingModelService: validation and creation.
/// </summary>
public sealed class BuildingModelServiceTests
{
    /// <summary>
    /// One valid and one degenerate request → 1 success, 1 failure; the model holds 1 wall.
    /// </summary>
    [Fact]
    public void CreateWalls_MixedValidAndInvalid_PartialSuccess()
    {
        var model = new InMemoryBuildingModel();
        var service = new BuildingModelService(new InMemoryWallCreator(model));

        var requests = new[]
        {
            new WallCreationRequest("OK", new Vector2(0, 0), new Vector2(500, 0), 200),
            new WallCreationRequest("BAD", new Vector2(1, 1), new Vector2(1, 1), 200)
        };

        IReadOnlyList<WallCreationResult> results = service.CreateWalls(requests);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.RequestId == "OK" && r.Success);
        Assert.Contains(results, r => r.RequestId == "BAD" && !r.Success);
        Assert.Single(model.Walls);
    }
}
