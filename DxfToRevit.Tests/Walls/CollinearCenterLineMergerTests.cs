using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Walls;
using netDxf;

namespace DxfToRevit.Tests.Walls;

/// <summary>
/// Collinear segment merge: gap ≤ maxGap gives one segment; a larger gap stays separate.
/// </summary>
public sealed class CollinearCenterLineMergerTests
{
    /// <summary>
    /// Touching collinear pieces (gap = 0) → one segment 0→250.
    /// </summary>
    [Fact]
    public void Merge_TouchingCollinearSegments_ReturnsOneSegment()
    {
        var a = new LineSegment2D(new Vector2(0, 0), new Vector2(100, 0));
        var b = new LineSegment2D(new Vector2(100, 0), new Vector2(250, 0));

        var result = new CollinearCenterLineMerger().Merge(new[] { a, b });

        Assert.Single(result);
        Assert.Equal(0, result[0].Start.X, 6);
        Assert.Equal(0, result[0].Start.Y, 6);
        Assert.Equal(250, result[0].End.X, 6);
        Assert.Equal(0, result[0].End.Y, 6);
    }

    /// <summary>
    /// 3 mm gap ≤ maxGap 5 mm → merged (0→200).
    /// </summary>
    [Fact]
    public void Merge_SmallGapWithinTolerance_ReturnsOneSegment()
    {
        var a = new LineSegment2D(new Vector2(0, 0), new Vector2(100, 0));
        var b = new LineSegment2D(new Vector2(103, 0), new Vector2(200, 0));

        var result = new CollinearCenterLineMerger(maxGapMm: 5.0)
            .Merge(new[] { a, b });

        Assert.Single(result);
        Assert.Equal(0, result[0].Start.X, 6);
        Assert.Equal(200, result[0].End.X, 6);
    }

    /// <summary>
    /// 200 mm gap ≫ 5 mm → two separate segments (the opening is preserved).
    /// </summary>
    [Fact]
    public void Merge_LargeGap_PreservesTwoSegments()
    {
        var a = new LineSegment2D(new Vector2(0, 0), new Vector2(100, 0));
        var b = new LineSegment2D(new Vector2(300, 0), new Vector2(400, 0));

        var result = new CollinearCenterLineMerger(maxGapMm: 5.0)
            .Merge(new[] { a, b });

        Assert.Equal(2, result.Count);
    }

    /// <summary>
    /// Collinear segments with opposite directions merge after projection.
    /// </summary>
    [Fact]
    public void Merge_ReversedDirection_StillMerges()
    {
        var a = new LineSegment2D(new Vector2(0, 0), new Vector2(100, 0));
        var b = new LineSegment2D(new Vector2(250, 0), new Vector2(100, 0));

        var result = new CollinearCenterLineMerger().Merge(new[] { a, b });

        Assert.Single(result);
        Assert.Equal(0, result[0].Start.X, 6);
        Assert.Equal(250, result[0].End.X, 6);
    }

    /// <summary>
    /// Merge_VerticalCollinear_ReturnsOneSegment: Yatay/dikey geometri.
    /// </summary>
    [Fact]
    public void Merge_VerticalCollinear_ReturnsOneSegment()
    {
        var a = new LineSegment2D(new Vector2(-2400, 2400), new Vector2(-2400, 0));
        var b = new LineSegment2D(new Vector2(-2400, 0), new Vector2(-2400, -2400));

        var result = new CollinearCenterLineMerger().Merge(new[] { a, b });

        Assert.Single(result);
        Assert.Equal(-2400, result[0].Start.X, 6);
        Assert.Equal(-2400, result[0].End.X, 6);
        double yMin = Math.Min(result[0].Start.Y, result[0].End.Y);
        double yMax = Math.Max(result[0].Start.Y, result[0].End.Y);
        Assert.Equal(-2400, yMin, 6);
        Assert.Equal(2400, yMax, 6);
    }

    /// <summary>
    /// Parallel but offset (50 mm) → not collinear → 2 segments.
    /// </summary>
    [Fact]
    public void Merge_NonCollinear_KeepsSeparate()
    {
        var a = new LineSegment2D(new Vector2(0, 0), new Vector2(100, 0));
        var b = new LineSegment2D(new Vector2(0, 50), new Vector2(100, 50));

        var result = new CollinearCenterLineMerger().Merge(new[] { a, b });

        Assert.Equal(2, result.Count);
    }

    /// <summary>
    /// Empty input → empty output.
    /// </summary>
    [Fact]
    public void Merge_Empty_ReturnsEmpty()
    {
        var result = new CollinearCenterLineMerger()
            .Merge(Array.Empty<LineSegment2D>());

        Assert.Empty(result);
    }

    /// <summary>
    /// 0→200 merged + 500→600 separate; lengths 200 and 100.
    /// </summary>
    [Fact]
    public void Merge_ThreeCollinearWithOneLargeGap_ReturnsTwo()
    {
        var a = new LineSegment2D(new Vector2(0, 0), new Vector2(100, 0));
        var b = new LineSegment2D(new Vector2(100, 0), new Vector2(200, 0));
        var c = new LineSegment2D(new Vector2(500, 0), new Vector2(600, 0));

        var result = new CollinearCenterLineMerger(maxGapMm: 5.0)
            .Merge(new[] { a, b, c });

        Assert.Equal(2, result.Count);
        Assert.Equal(200, Math.Max(result[0].Length, result[1].Length), 6);
        Assert.Equal(100, Math.Min(result[0].Length, result[1].Length), 6);
    }
}
