
using DxfToRevit.Core.Dxf;
using DxfToRevit.Core.Geometry;
using netDxf;

namespace DxfToRevit.Tests.Dxf;

/// <summary>
/// Creating raw DxfLineModels and argument checks.
/// </summary>
public class DxfLineModelTests
{
    /// <summary>
    /// Constructor_ValidInput_StoresGeometryAndLayer: Layer/thickness resolution.
    /// </summary>
    [Fact]
    public void Constructor_ValidInput_StoresGeometryAndLayer()
    {
        var geometry = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(5000, 0));

        var model = new DxfLineModel(
            geometry,
            "WALL-20",
            "TEST001");

        Assert.Same(geometry, model.Geometry);
        Assert.Equal("WALL-20", model.SourceLayer);
    }

    /// <summary>
    /// Constructor_NullGeometry_ThrowsArgumentNullException: Null argument → exception.
    /// </summary>
    [Fact]
    public void Constructor_NullGeometry_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DxfLineModel(null!, "WALL-20", "TEST001"));
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
        var geometry = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(5000, 0));

        Assert.Throws<ArgumentException>(() =>
            new DxfLineModel(geometry, layerName, "TEST001"));
    }
}

