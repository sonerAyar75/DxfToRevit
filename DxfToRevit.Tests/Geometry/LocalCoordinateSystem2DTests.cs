using DxfToRevit.Core.Geometry;
using netDxf;
using Xunit;

namespace DxfToRevit.Tests.Geometry;

/// <summary>
/// LocalCoordinateSystem2D: orthonormal frame from an origin and an X direction. ToLocal: p' = ((p-o)·ux, (p-o)·uy); ToWorld: o + x·ux + y·uy.
/// </summary>
public class LocalCoordinateSystem2DTests
{
    /// <summary>
    /// ToLocal_PointOnReferenceLine_ReturnsCorrectCoordinates: Valid input / happy path.
    /// </summary>
    [Fact]
    public void ToLocal_PointOnReferenceLine_ReturnsCorrectCoordinates()
    {
        var coordinateSystem = new LocalCoordinateSystem2D(
            new Vector2(100, 100),
            new Vector2(1000, 0));

        Vector2 result = coordinateSystem.ToLocal(
            new Vector2(600, 100));

        Assert.Equal(500, result.X, 6);
        Assert.Equal(0, result.Y, 6);
    }

    /// <summary>
    /// ToLocal_PointAboveReferenceLine_ReturnsCorrectCoordinates: Valid input / happy path.
    /// </summary>
    [Fact]
    public void ToLocal_PointAboveReferenceLine_ReturnsCorrectCoordinates()
    {
        var coordinateSystem = new LocalCoordinateSystem2D(
            new Vector2(100, 100),
            new Vector2(1000, 0));

        Vector2 result = coordinateSystem.ToLocal(
            new Vector2(600, 300));

        Assert.Equal(500, result.X, 6);
        Assert.Equal(200, result.Y, 6);
    }

    /// <summary>
    /// ToWorld_LocalPoint_ReturnsOriginalWorldCoordinates: Valid input / happy path.
    /// </summary>
    [Fact]
    public void ToWorld_LocalPoint_ReturnsOriginalWorldCoordinates()
    {
        var coordinateSystem = new LocalCoordinateSystem2D(
            new Vector2(100, 100),
            new Vector2(1000, 0));

        Vector2 result = coordinateSystem.ToWorld(
            new Vector2(500, 200));

        Assert.Equal(600, result.X, 6);
        Assert.Equal(300, result.Y, 6);
    }
}
