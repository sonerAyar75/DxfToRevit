
using DxfToRevit.Core.Dxf;
using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Walls;
using netDxf;

namespace DxfToRevit.Tests.Walls;

/// <summary>
/// Single line layer → wall/thickness classification.
/// </summary>
public sealed class DxfLineClassifierTests
{
    private static DxfLineModel CreateLine(string layerName)
    {
        var geometry = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        return new DxfLineModel(geometry, layerName, "TEST001");
    }

    /// <summary>
    /// Classify_WallLayerWithKnownThickness_ReturnsThickness: Layer/thickness resolution.
    /// </summary>
    [Fact]
    public void Classify_WallLayerWithKnownThickness_ReturnsThickness()
    {
        var classifier = new DxfLineClassifier();
        var line = CreateLine("WALL-20");

        ClassifiedDxfLine result = classifier.Classify(line);

        Assert.True(result.IsWallLayer);
        Assert.Equal(200.0, result.WallThicknessMm);
        Assert.Equal("WALL-20", result.SourceLayer);
    }

    /// <summary>
    /// Classify_WallLayerWithoutThickness_KeepsThicknessUnknown: Layer/thickness resolution.
    /// </summary>
    [Fact]
    public void Classify_WallLayerWithoutThickness_KeepsThicknessUnknown()
    {
        var classifier = new DxfLineClassifier();
        var line = CreateLine("wall");

        ClassifiedDxfLine result = classifier.Classify(line);

        Assert.True(result.IsWallLayer);
        Assert.Null(result.WallThicknessMm);
    }

    /// <summary>
    /// Classify_NonWallLayer_ReturnsNonWall: Layer/thickness resolution.
    /// </summary>
    [Fact]
    public void Classify_NonWallLayer_ReturnsNonWall()
    {
        var classifier = new DxfLineClassifier();
        var line = CreateLine("COLUMN");

        ClassifiedDxfLine result = classifier.Classify(line);

        Assert.False(result.IsWallLayer);
        Assert.Null(result.WallThicknessMm);
    }

    /// <summary>
    /// Classify_NullInput_ThrowsArgumentNullException: Null argument → exception.
    /// </summary>
    [Fact]
    public void Classify_NullInput_ThrowsArgumentNullException()
    {
        var classifier = new DxfLineClassifier();

        Assert.Throws<ArgumentNullException>(
            () => classifier.Classify(null!));
    }

    /// <summary>
    /// Classify_LayerStartingWithWallButNotWallConvention_ReturnsNonWall: Layer/thickness resolution.
    /// </summary>
    [Theory]
    [InlineData("WALLPAPER")]
    [InlineData("WALLS")]
    public void Classify_LayerStartingWithWallButNotWallConvention_ReturnsNonWall(
        string layerName)
    {
        var classifier = new DxfLineClassifier();
        var line = CreateLine(layerName);

        ClassifiedDxfLine result = classifier.Classify(line);

        Assert.False(result.IsWallLayer);
        Assert.Null(result.WallThicknessMm);
    }
}

