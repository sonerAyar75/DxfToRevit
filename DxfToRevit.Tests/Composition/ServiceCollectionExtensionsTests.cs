using DxfToRevit.Core.Composition;
using DxfToRevit.Core.Dxf;
using DxfToRevit.Core.InMemory;
using DxfToRevit.Core.Walls;
using Microsoft.Extensions.DependencyInjection;
using netDxf;

namespace DxfToRevit.Tests.Composition;

/// <summary>
/// DI registrations: the pipeline resolves and the in-memory building host works.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    /// <summary>AddDxfToRevitCore → DxfCenterLinePipeline can be resolved.</summary>
    [Fact]
    public void AddDxfToRevitCore_ResolvesPipeline()
    {
        var services = new ServiceCollection();
        services.AddDxfToRevitCore();
        using var sp = services.BuildServiceProvider();

        var pipeline = sp.GetRequiredService<DxfCenterLinePipeline>();
        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// InMemory host: IWallCreator resolve → CreateWall model'e yazar.
    /// </summary>
    [Fact]
    public void AddBuildingInMemory_WallCreator_WritesToSharedModel()
    {
        var services = new ServiceCollection();
        services.AddDxfToRevitCore();
        services.AddDxfToRevitBuildingInMemory();
        using var sp = services.BuildServiceProvider();

        var model = sp.GetRequiredService<InMemoryBuildingModel>();
        var walls = sp.GetRequiredService<IWallCreator>();
        var result = walls.CreateWall(
            new WallCreationRequest("W1", new Vector2(0, 0), new Vector2(100, 0), 200));

        Assert.True(result.Success);
        Assert.Single(model.Walls);
    }
}
