
using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Walls;
using netDxf;

namespace DxfToRevit.Tests.Walls;

/// <summary>
/// ClassifiedDxfLine validation and constraints (thickness, layer, handle).
/// </summary>
public class ClassifiedDxfLineTests
{
    private static LineSegment2D CreateGeometry()
    {
        return new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(5000, 0));
    }

    /// <summary>
    /// Constructor_ValidWallLine_StoresClassification: Model creation.
    /// </summary>
    [Fact]
    public void Constructor_ValidWallLine_StoresClassification()
    {
        var line = new ClassifiedDxfLine(
            CreateGeometry(),
            "WALL-20",
            isWallLayer: true,
            wallThicknessMm: 200, "TEST001");

        Assert.Equal("WALL-20", line.SourceLayer);
        Assert.True(line.IsWallLayer);
        Assert.Equal(200, line.WallThicknessMm);
    }

    /// <summary>
    /// Constructor_NonWallLineWithoutThickness_IsValid: Layer/thickness resolution.
    /// </summary>
    [Fact]
    public void Constructor_NonWallLineWithoutThickness_IsValid()
    {
        var line = new ClassifiedDxfLine(
            CreateGeometry(),
            "COLUMN",
            isWallLayer: false,
            wallThicknessMm: null, "TEST001");

        Assert.False(line.IsWallLayer);
        Assert.Null(line.WallThicknessMm);
    }

    /// <summary>
    /// Constructor_NullGeometry_ThrowsArgumentNullException: Null argument → exception.
    /// </summary>
    [Fact]
    public void Constructor_NullGeometry_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ClassifiedDxfLine(
                null!,
                "WALL-20",
                true,
                200, "TEST001"));
    }

    /// <summary>
    /// Constructor_EmptyLayer_ThrowsArgumentException: Empty input scenario.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_EmptyLayer_ThrowsArgumentException(
        string layerName)
    {
        Assert.Throws<ArgumentException>(() =>
            new ClassifiedDxfLine(
                CreateGeometry(),
                layerName,
                true,
                200, "TEST001"));
    }

    /// <summary>
    /// Constructor_NonPositiveThickness_Throws: Layer/thickness resolution.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Constructor_NonPositiveThickness_Throws(
        double thicknessMm)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ClassifiedDxfLine(
                CreateGeometry(),
                "WALL-20",
                true,
                thicknessMm, "TEST001"));
    }

    /// <summary>
    /// Constructor_NonWallWithThickness_ThrowsArgumentException: Layer/thickness resolution.
    /// </summary>
    [Fact]
    public void Constructor_NonWallWithThickness_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new ClassifiedDxfLine(
                CreateGeometry(),
                "COLUMN",
                false,
                200, "TEST001"));
    }
}

