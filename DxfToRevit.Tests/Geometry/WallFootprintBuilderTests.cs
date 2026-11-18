using DxfToRevit.Core.Geometry;
using netDxf;
using Xunit;

namespace DxfToRevit.Tests.Geometry;

/// <summary>
/// WallFootprintBuilder: a closed four-corner rectangle from a ±T/2 perpendicular offset of the centerline.
/// Horizontal centerline (0,0)→(1000,0), T = 200 → Y = ±100, X = 0 and 1000.
/// </summary>
public sealed class WallFootprintBuilderTests
{
    /// <summary>
    /// Horizontal centerline: outline X ∈ {0, 1000}, Y ∈ {±100}; 4 corners.
    /// </summary>
    [Fact]
    public void Build_HorizontalCenterLine_ReturnsRectangleOfCorrectThickness()
    {
        var cl = new LineSegment2D(new Vector2(0, 0), new Vector2(1000, 0));
        var solid = new WallFootprintBuilder().Build(cl, 200);

        Assert.Equal(4, solid.Outline.Count);
        Assert.Equal(200, solid.ThicknessMm, 6);

        double yMin = solid.Outline.Min(p => p.Y);
        double yMax = solid.Outline.Max(p => p.Y);
        Assert.Equal(-100, yMin, 6);
        Assert.Equal(100, yMax, 6);

        double xMin = solid.Outline.Min(p => p.X);
        double xMax = solid.Outline.Max(p => p.X);
        Assert.Equal(0, xMin, 6);
        Assert.Equal(1000, xMax, 6);
    }

    /// <summary>
    /// Dikey centerline x=0, y=0→500, T=150 → X∈{±75}.
    /// </summary>
    [Fact]
    public void Build_VerticalCenterLine_OffsetsAlongX()
    {
        var cl = new LineSegment2D(new Vector2(0, 0), new Vector2(0, 500));
        var solid = new WallFootprintBuilder().Build(cl, 150);

        double xMin = solid.Outline.Min(p => p.X);
        double xMax = solid.Outline.Max(p => p.X);
        Assert.Equal(-75, xMin, 6);
        Assert.Equal(75, xMax, 6);
    }

    /// <summary>Dejenere centerline → ArgumentException.</summary>
    [Fact]
    public void Build_Degenerate_Throws()
    {
        var cl = new LineSegment2D(new Vector2(1, 1), new Vector2(1, 1));
        Assert.Throws<ArgumentException>(() => new WallFootprintBuilder().Build(cl, 200));
    }
}
