using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Models;
using netDxf;
using Xunit;

namespace DxfToRevit.Tests.Geometry;

/// <summary>
/// Tests that centerline gaps of walls open on one side are bridged while real openings are preserved.
/// </summary>
public sealed class OpenSideCenterLineBridgerTests
{
    /// <summary>
    /// Lower face continuous 0→1000, upper face broken at 400|600 → the centerline is joined 0→1000.
    /// </summary>
    [Fact]
    public void Bridge_OneFaceContinuousAcrossGap_MergesCenterLines()
    {
        var continuousFace = new LineSegment2D(new Vector2(0, 0), new Vector2(1000, 0));
        var topLeft = new LineSegment2D(new Vector2(0, 200), new Vector2(400, 200));
        var topRight = new LineSegment2D(new Vector2(600, 200), new Vector2(1000, 200));
        var clLeft = new LineSegment2D(new Vector2(0, 100), new Vector2(400, 100));
        var clRight = new LineSegment2D(new Vector2(600, 100), new Vector2(1000, 100));

        var pair1 = new WallFacePair(continuousFace, topLeft, "WALL-20", 200, "A", "B1");
        var pair2 = new WallFacePair(continuousFace, topRight, "WALL-20", 200, "A", "B2");

        var walls = new[]
        {
            new WallModel("W1", "WALL-20", 200, clLeft, continuousFace, topLeft),
            new WallModel("W2", "WALL-20", 200, clRight, continuousFace, topRight)
        };

        var result = new OpenSideCenterLineBridger().Bridge(walls, new[] { pair1, pair2 });

        Assert.Single(result);
        Assert.Equal(0, Math.Min(result[0].Start.X, result[0].End.X), 6);
        Assert.Equal(1000, Math.Max(result[0].Start.X, result[0].End.X), 6);
    }

    /// <summary>
    /// Both faces broken → real opening → 2 centerlines remain.
    /// </summary>
    [Fact]
    public void Bridge_BothSidesOpen_PreservesGap()
    {
        var bottomLeft = new LineSegment2D(new Vector2(0, 0), new Vector2(400, 0));
        var bottomRight = new LineSegment2D(new Vector2(600, 0), new Vector2(1000, 0));
        var topLeft = new LineSegment2D(new Vector2(0, 200), new Vector2(400, 200));
        var topRight = new LineSegment2D(new Vector2(600, 200), new Vector2(1000, 200));
        var clLeft = new LineSegment2D(new Vector2(0, 100), new Vector2(400, 100));
        var clRight = new LineSegment2D(new Vector2(600, 100), new Vector2(1000, 100));

        var pair1 = new WallFacePair(bottomLeft, topLeft, "WALL-20", 200, "A1", "B1");
        var pair2 = new WallFacePair(bottomRight, topRight, "WALL-20", 200, "A2", "B2");

        var walls = new[]
        {
            new WallModel("W1", "WALL-20", 200, clLeft, bottomLeft, topLeft),
            new WallModel("W2", "WALL-20", 200, clRight, bottomRight, topRight)
        };

        var result = new OpenSideCenterLineBridger().Bridge(walls, new[] { pair1, pair2 });

        Assert.Equal(2, result.Count);
    }

    /// <summary>
    /// Single wall: the length does not change (500 mm).
    /// </summary>
    [Fact]
    public void Bridge_SingleWall_ReturnsSameCenterLine()
    {
        var faceA = new LineSegment2D(new Vector2(0, 0), new Vector2(500, 0));
        var faceB = new LineSegment2D(new Vector2(0, 200), new Vector2(500, 200));
        var cl = new LineSegment2D(new Vector2(0, 100), new Vector2(500, 100));
        var pair = new WallFacePair(faceA, faceB, "WALL-20", 200, "A", "B");
        var walls = new[]
        {
            new WallModel("W1", "WALL-20", 200, cl, faceA, faceB)
        };

        var result = new OpenSideCenterLineBridger().Bridge(walls, new[] { pair });

        Assert.Single(result);
        Assert.Equal(500, result[0].Length, 6);
    }
}
