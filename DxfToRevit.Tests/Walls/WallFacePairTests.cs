using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Models;
using netDxf;
using Xunit;

namespace DxfToRevit.Tests.Walls;

/// <summary>
/// WallFacePair creation and validation.
/// </summary>
public sealed class WallFacePairTests
{
    /// <summary>
    /// Constructor_SingleHandles_CreatesHandleLists: Model creation.
    /// </summary>
    [Fact]
    public void Constructor_SingleHandles_CreatesHandleLists()
    {
        // Arrange
        var faceA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        var faceB = new LineSegment2D(
            new Vector2(0, 200),
            new Vector2(1000, 200));

        // Act
        var pair = new WallFacePair(
            faceA,
            faceB,
            "WALL-20",
            200,
            "MAIN-A",
            "MAIN-B");

        // Assert
        Assert.Equal(
            new[] { "MAIN-A" },
            pair.FaceAHandles);

        Assert.Equal(
            new[] { "MAIN-B" },
            pair.FaceBHandles);
    }

    /// <summary>
    /// Constructor_MultipleHandles_StoresAllHandles: Model creation.
    /// </summary>
    [Fact]
    public void Constructor_MultipleHandles_StoresAllHandles()
    {
        // Arrange
        var faceA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        var faceB = new LineSegment2D(
            new Vector2(0, 200),
            new Vector2(1000, 200));

        var faceBHandles = new[]
        {
        "MAIN-B1",
        "MAIN-B2"
    };

        // Act
        var pair = new WallFacePair(
            faceA,
            faceB,
            "WALL-20",
            200,
            "MAIN-A",
            "MAIN-B1",
            faceBHandles: faceBHandles);

        // Assert
        Assert.Equal(
            new[] { "MAIN-A" },
            pair.FaceAHandles);

        Assert.Equal(
            new[] { "MAIN-B1", "MAIN-B2" },
            pair.FaceBHandles);
    }
}
