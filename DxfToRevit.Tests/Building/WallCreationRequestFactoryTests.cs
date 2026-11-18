using DxfToRevit.Core.Building;
using DxfToRevit.Core.Geometry;
using netDxf;
using Xunit;

namespace DxfToRevit.Tests.Building;

/// <summary>
/// Creating WallCreationRequests from pipeline geometry (with ImportSessionOptions).
/// </summary>
public sealed class WallCreationRequestFactoryTests
{
    [Fact]
    public void FromWallSolids_MapsThicknessAndCenterLine()
    {
        var cl = new LineSegment2D(new Vector2(0, 0), new Vector2(2000, 0));
        var solid = new WallFootprintBuilder().Build(cl, 200);
        var factory = new WallCreationRequestFactory();

        IReadOnlyList<WallCreationRequest> requests = factory.FromWallSolids(new[] { solid });

        Assert.Single(requests);
        Assert.Equal(200, requests[0].ThicknessMm);
        Assert.Equal(2000, requests[0].LengthMm, 6);
        Assert.Equal("Basic Wall", requests[0].WallTypeKey);
    }

    [Fact]
    public void FromCenterLines_SessionLevelAndHeight()
    {
        var lines = new[]
        {
            new LineSegment2D(new Vector2(0, 0), new Vector2(100, 0)),
            new LineSegment2D(new Vector2(0, 0), new Vector2(0, 100))
        };
        var thicknesses = new[] { 200.0, 150.0 };
        var session = ImportSessionOptions.ForLevel("Level 1", heightMm: 3000);
        var factory = new WallCreationRequestFactory(session);

        IReadOnlyList<WallCreationRequest> requests = factory.FromCenterLines(lines, thicknesses);

        Assert.Equal(2, requests.Count);
        Assert.Equal("Level 1", requests[0].LevelName);
        Assert.Equal(3000, requests[0].HeightMm);
        Assert.Equal(150, requests[1].ThicknessMm);
    }
}
