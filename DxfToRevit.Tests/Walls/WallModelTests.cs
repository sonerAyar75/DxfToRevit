
using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Models;
using netDxf;
using Xunit;

namespace DxfToRevit.Tests.Walls;

/// <summary>
/// WallModel creation rules (faces together, thickness > 0).
/// </summary>
public class WallModelTests
{
    /// <summary>
    /// Constructor_ShouldCreateWallWithCenterLine: Model creation.
    /// </summary>
    [Fact]
    public void Constructor_ShouldCreateWallWithCenterLine()
    {
        var centerLine = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(5000, 0));

        var wall = new WallModel(
            id: "W-001",
            sourceLayer: "WALL-20",
            thicknessMm: 200,
            centerLine: centerLine);

        Assert.Equal("W-001", wall.Id);
        Assert.Equal("WALL-20", wall.SourceLayer);
        Assert.Equal(200, wall.ThicknessMm);
        Assert.Same(centerLine, wall.CenterLine);
        Assert.Null(wall.FaceA);
        Assert.Null(wall.FaceB);
    }

    /// <summary>
    /// Constructor_ShouldCreateWallWithTwoFaces: Model creation.
    /// </summary>
    [Fact]
    public void Constructor_ShouldCreateWallWithTwoFaces()
    {
        var faceA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(5000, 0));

        var faceB = new LineSegment2D(
            new Vector2(0, 200),
            new Vector2(5000, 200));

        var wall = new WallModel(
            id: "W-002",
            sourceLayer: "WALL-20",
            thicknessMm: 200,
            faceA: faceA,
            faceB: faceB);

        Assert.Same(faceA, wall.FaceA);
        Assert.Same(faceB, wall.FaceB);
        Assert.Null(wall.CenterLine);
    }

    /// <summary>
    /// Constructor_ShouldThrow_WhenIdIsEmpty: Empty input scenario.
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrow_WhenIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new WallModel(
                id: "",
                sourceLayer: "WALL-20",
                thicknessMm: 200,
                centerLine: CreateCenterLine()));
    }

    /// <summary>
    /// Constructor_ShouldThrow_WhenSourceLayerIsEmpty: Empty input scenario.
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrow_WhenSourceLayerIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new WallModel(
                id: "W-001",
                sourceLayer: " ",
                thicknessMm: 200,
                centerLine: CreateCenterLine()));
    }

    /// <summary>
    /// Constructor_ShouldThrow_WhenThicknessIsNotPositive: Layer/thickness resolution.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Constructor_ShouldThrow_WhenThicknessIsNotPositive(
        double thicknessMm)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WallModel(
                id: "W-001",
                sourceLayer: "WALL-20",
                thicknessMm: thicknessMm,
                centerLine: CreateCenterLine()));
    }

    /// <summary>
    /// Constructor_ShouldThrow_WhenGeometryIsMissing: Model creation.
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrow_WhenGeometryIsMissing()
    {
        Assert.Throws<ArgumentException>(() =>
            new WallModel(
                id: "W-001",
                sourceLayer: "WALL-20",
                thicknessMm: 200));
    }

    /// <summary>
    /// Constructor_ShouldThrow_WhenOnlyOneFaceIsProvided: Model creation.
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrow_WhenOnlyOneFaceIsProvided()
    {
        var faceA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(5000, 0));

        Assert.Throws<ArgumentException>(() =>
            new WallModel(
                id: "W-001",
                sourceLayer: "WALL-20",
                thicknessMm: 200,
                faceA: faceA));
    }

    private static LineSegment2D CreateCenterLine()
    {
        return new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(5000, 0));
    }
}

