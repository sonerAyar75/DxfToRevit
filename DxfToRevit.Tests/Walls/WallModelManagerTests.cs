
using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Models;
using DxfToRevit.Core.Services;
using netDxf;
using Xunit;

namespace DxfToRevit.Tests.Walls;

/// <summary>
/// Creating WallModels from face pairs and centerlines.
/// </summary>
public class WallModelManagerTests
{
    private readonly WallModelManager _manager = new();

    private static LineSegment2D CreateCenterLine()
    {
        return new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(5000, 0));
    }

    /// <summary>
    /// CreateFromCenterLine_KnownLayer_UsesLayerThickness: Layer/thickness resolution.
    /// </summary>
    [Fact]
    public void CreateFromCenterLine_KnownLayer_UsesLayerThickness()
    {
        WallModel wall = _manager.CreateFromCenterLine(
            "W001",
            "WALL-20",
            CreateCenterLine());

        Assert.Equal(200, wall.ThicknessMm);
        Assert.Equal("WALL-20", wall.SourceLayer);
        Assert.NotNull(wall.CenterLine);
    }

    /// <summary>
    /// CreateFromCenterLine_UnknownLayerWithUserThickness_UsesUserThickness: Layer/thickness resolution.
    /// </summary>
    [Fact]
    public void CreateFromCenterLine_UnknownLayerWithUserThickness_UsesUserThickness()
    {
        WallModel wall = _manager.CreateFromCenterLine(
            "W002",
            "wall",
            CreateCenterLine(),
            userThicknessMm: 150);

        Assert.Equal(150, wall.ThicknessMm);
    }

    /// <summary>
    /// CreateFromCenterLine_UnknownLayerWithoutUserThickness_Throws: Layer/thickness resolution.
    /// </summary>
    [Fact]
    public void CreateFromCenterLine_UnknownLayerWithoutUserThickness_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _manager.CreateFromCenterLine(
                "W003",
                "wall",
                CreateCenterLine()));
    }

    /// <summary>
    /// CreateFromCenterLine_KnownLayerAndDifferentUserThickness_UsesLayerThickness: Layer/thickness resolution.
    /// </summary>
    [Fact]
    public void CreateFromCenterLine_KnownLayerAndDifferentUserThickness_UsesLayerThickness()
    {
        WallModel wall = _manager.CreateFromCenterLine(
            "W004",
            "WALL-20",
            CreateCenterLine(),
            userThicknessMm: 150);

        Assert.Equal(200, wall.ThicknessMm);
    }

    /// <summary>
    /// CreateFromCenterLine_InvalidUserThickness_Throws: Layer/thickness resolution.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void CreateFromCenterLine_InvalidUserThickness_Throws(
        double invalidThicknessMm)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _manager.CreateFromCenterLine(
                "W005",
                "wall",
                CreateCenterLine(),
                userThicknessMm: invalidThicknessMm));
    }

    /// <summary>
    /// CreateFromFacePair_DifferentLengthFaces_CreatesCorrectWall: Model creation.
    /// </summary>
    [Fact]
    public void CreateFromFacePair_DifferentLengthFaces_CreatesCorrectWall()
    {
        // Arrange
        var faceA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        var faceB = new LineSegment2D(
            new Vector2(200, 200),
            new Vector2(800, 200));

        var facePair = new WallFacePair(
            faceA,
            faceB,
            "WALL-20",
            200,
            "A1",
            "B1");

        var manager = new WallModelManager();

        // Act
        WallModel wall = manager.CreateFromFacePair(
            "W001",
            facePair);

        // Assert
        Assert.Equal("W001", wall.Id);
        Assert.Equal("WALL-20", wall.SourceLayer);
        Assert.Equal(200, wall.ThicknessMm);

        Assert.NotNull(wall.CenterLine);
        Assert.NotNull(wall.FaceA);
        Assert.NotNull(wall.FaceB);

        Assert.Equal(
            new Vector2(200, 100),
            wall.CenterLine!.Start);

        Assert.Equal(
            new Vector2(800, 100),
            wall.CenterLine.End);

        Assert.Equal(600, wall.CenterLine.Length, 6);
    }

    /// <summary>
    /// CreateFromFacePair_ReversedFaceDirection_CreatesCorrectWall: Model creation.
    /// </summary>
    [Fact]
    public void CreateFromFacePair_ReversedFaceDirection_CreatesCorrectWall()
    {
        // Arrange
        var faceA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        var faceB = new LineSegment2D(
            new Vector2(800, 200),
            new Vector2(200, 200));

        var facePair = new WallFacePair(
            faceA,
            faceB,
            "WALL-20",
            200,
            "A1",
            "B1");

        var manager = new WallModelManager();

        // Act
        WallModel wall = manager.CreateFromFacePair(
            "W002",
            facePair);

        // Assert
        Assert.NotNull(wall.CenterLine);

        Assert.Equal(
            new Vector2(200, 100),
            wall.CenterLine!.Start);

        Assert.Equal(
            new Vector2(800, 100),
            wall.CenterLine.End);

        Assert.Equal(600, wall.CenterLine.Length, 6);
    }


    /// <summary>
    /// CreateFromFacePair_PreservesWallFacesAndMetadata: Model creation.
    /// </summary>
    [Fact]
    public void CreateFromFacePair_PreservesWallFacesAndMetadata()
    {
        var faceA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        var faceB = new LineSegment2D(
            new Vector2(200, 200),
            new Vector2(800, 200));

        var facePair = new WallFacePair(
            faceA,
            faceB,
            "WALL-20",
            200,
            "A1",
            "B1");

        var manager = new WallModelManager();

        WallModel wall = manager.CreateFromFacePair(
            "W003",
            facePair);

        Assert.NotNull(wall.FaceA);
        Assert.NotNull(wall.FaceB);

        Assert.Equal(200, wall.FaceA!.Start.X, 6);
        Assert.Equal(800, wall.FaceA.End.X, 6);

        Assert.Equal(200, wall.FaceB!.Start.X, 6);
        Assert.Equal(800, wall.FaceB.End.X, 6);
        Assert.NotNull(wall.FaceB);

        Assert.Equal(200, wall.FaceB!.Start.X, 6);
        Assert.Equal(200, wall.FaceB.Start.Y, 6);

        Assert.Equal(800, wall.FaceB.End.X, 6);
        Assert.Equal(200, wall.FaceB.End.Y, 6);

        Assert.Equal("W003", wall.Id);
        Assert.Equal("WALL-20", wall.SourceLayer);
        Assert.Equal(200, wall.ThicknessMm);

        Assert.NotNull(wall.FaceA);
        Assert.NotNull(wall.FaceB);
        Assert.NotNull(wall.CenterLine);

        Assert.Equal(600, wall.FaceA!.Length, 6);
        Assert.Equal(600, wall.FaceB!.Length, 6);
        Assert.Equal(600, wall.CenterLine!.Length, 6);
    }

}

