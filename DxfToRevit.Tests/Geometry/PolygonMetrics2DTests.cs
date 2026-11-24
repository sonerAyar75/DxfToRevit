using DxfToRevit.Core.Geometry;
using netDxf;

namespace DxfToRevit.Tests.Geometry;

/// <summary>
/// Bounding box, area and area-weighted centroid.
/// </summary>
public sealed class PolygonMetrics2DTests
{
    [Fact]
    public void TryGetWidthDepth_Rectangle_ReturnsCorrectSize()
    {
        var outline = new[]
        {
            new Vector2(10, 20),
            new Vector2(110, 20),
            new Vector2(110, 70),
            new Vector2(10, 70)
        };

        Assert.True(PolygonMetrics2D.TryGetWidthDepth(outline, out double w, out double d));
        Assert.Equal(100, w, 6);
        Assert.Equal(50, d, 6);
    }

    [Fact]
    public void SignedArea_UnitSquare_IsOne()
    {
        var outline = new[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        Assert.Equal(1.0, PolygonMetrics2D.SignedArea(outline), 9);
    }

    [Fact]
    public void TryGetAreaCentroid_LShape_MatchesAnalytical()
    {
        var outline = new[]
        {
            new Vector2(0, 0),
            new Vector2(4, 0),
            new Vector2(4, 1),
            new Vector2(1, 1),
            new Vector2(1, 3),
            new Vector2(0, 3)
        };

        Assert.True(PolygonMetrics2D.TryGetAreaCentroid(outline, out Vector2 c));
        Assert.Equal(1.5, c.X, 6);
        Assert.Equal(1.0, c.Y, 6);
    }
}
