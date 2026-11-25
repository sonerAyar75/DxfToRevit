
using DxfToRevit.Core.Geometry;
using netDxf;

namespace DxfToRevit.Tests.Geometry;

/// <summary>
/// Intersection of the supporting lines of two segments: p + t·r = q + u·s, t = (qp×s)/(r×s). Parallel (r×s ≈ 0 or IsParallelTo) → false.
/// </summary>
public class LineIntersectionCalculatorTests
{
    private readonly LineIntersectionCalculator _calculator = new();

    /// <summary>
    /// TryFindIntersection_PerpendicularLines_ReturnsIntersection: Line intersection.
    /// </summary>
    /// <summary>

    /// Perpendicular lines intersect at a known point.

    /// </summary>

    [Fact]
    public void TryFindIntersection_PerpendicularLines_ReturnsIntersection()
    {
        var horizontal = new LineSegment2D(
            new Vector2(0, 100),
            new Vector2(200, 100));

        var vertical = new LineSegment2D(
            new Vector2(100, 0),
            new Vector2(100, 200));

        bool found = _calculator.TryFindIntersection(
            horizontal,
            vertical,
            out Vector2 point);

        Assert.True(found);
        Assert.Equal(100, point.X, 6);
        Assert.Equal(100, point.Y, 6);
    }

    /// <summary>
    /// TryFindIntersection_ExtendedLines_ReturnsIntersection: Line intersection.
    /// </summary>
    [Fact]
    public void TryFindIntersection_ExtendedLines_ReturnsIntersection()
    {
        var horizontal = new LineSegment2D(
            new Vector2(0, 100),
            new Vector2(50, 100));

        var vertical = new LineSegment2D(
            new Vector2(100, 0),
            new Vector2(100, 50));

        bool found = _calculator.TryFindIntersection(
            horizontal,
            vertical,
            out Vector2 point);

        Assert.True(found);
        Assert.Equal(100, point.X, 6);
        Assert.Equal(100, point.Y, 6);
    }

    /// <summary>
    /// TryFindIntersection_ParallelLines_ReturnsFalse: Parallelism / parallel distance check.
    /// </summary>
    /// <summary>

    /// Parallel → no intersection.

    /// </summary>

    [Fact]
    public void TryFindIntersection_ParallelLines_ReturnsFalse()
    {
        var first = new LineSegment2D(
            new Vector2(0, 100),
            new Vector2(200, 100));

        var second = new LineSegment2D(
            new Vector2(0, 200),
            new Vector2(200, 200));

        bool found = _calculator.TryFindIntersection(
            first,
            second,
            out _);

        Assert.False(found);
    }

    /// <summary>
    /// TryFindIntersection_ShortLines_ReturnsIntersection: Line intersection.
    /// </summary>
    [Fact]
    public void TryFindIntersection_ShortLines_ReturnsIntersection()
    {
        var horizontal = new LineSegment2D(
            new Vector2(0, 5),
            new Vector2(10, 5));

        var vertical = new LineSegment2D(
            new Vector2(5, 0),
            new Vector2(5, 10));

        bool found = _calculator.TryFindIntersection(
            horizontal,
            vertical,
            out Vector2 point);

        Assert.True(found);

        Assert.Equal(5, point.X, 6);
        Assert.Equal(5, point.Y, 6);
    }

    /// <summary>
    /// TryFindIntersection_VeryShortLines_ReturnsIntersection: Line intersection.
    /// </summary>
    [Fact]
    public void TryFindIntersection_VeryShortLines_ReturnsIntersection()
    {
        var horizontal = new LineSegment2D(
            new Vector2(0, 0.5),
            new Vector2(1, 0.5));

        var vertical = new LineSegment2D(
            new Vector2(0.5, 0),
            new Vector2(0.5, 1));

        bool found = _calculator.TryFindIntersection(
            horizontal,
            vertical,
            out Vector2 point);

        Assert.True(found);

        Assert.Equal(0.5, point.X, 6);
        Assert.Equal(0.5, point.Y, 6);
    }

    /// <summary>
    /// TryFindIntersection_ExtremelyShortLines_ReturnsIntersection: Line intersection.
    /// </summary>
    [Fact]
    public void TryFindIntersection_ExtremelyShortLines_ReturnsIntersection()
    {
        var horizontal = new LineSegment2D(
            new Vector2(0, 0.0005),
            new Vector2(0.001, 0.0005));

        var vertical = new LineSegment2D(
            new Vector2(0.0005, 0),
            new Vector2(0.0005, 0.001));

        bool found = _calculator.TryFindIntersection(
            horizontal,
            vertical,
            out Vector2 point);

        Assert.True(found);

        Assert.Equal(0.0005, point.X, 6);
        Assert.Equal(0.0005, point.Y, 6);
    }

    /// <summary>
    /// TryFindIntersection_NearlyParallelLines_ReturnsFalse: Parallelism / parallel distance check.
    /// </summary>
    /// <summary>

    /// Nearly parallel within tolerance → false.

    /// </summary>

    [Fact]
    public void TryFindIntersection_NearlyParallelLines_ReturnsFalse()
    {
        var first = new LineSegment2D(
            new Vector2(0, 100),
            new Vector2(1000, 100));

        var second = new LineSegment2D(
            new Vector2(0, 110),
            new Vector2(1000, 110.5));

        bool found = _calculator.TryFindIntersection(
            first,
            second,
            out _);

        Assert.False(found);
    }

    /// <summary>
    /// TryFindIntersection_AngleOutsideParallelTolerance_ReturnsIntersection: Parallelism / parallel distance check.
    /// </summary>
    [Fact]
    public void TryFindIntersection_AngleOutsideParallelTolerance_ReturnsIntersection()
    {
        var first = new LineSegment2D(
            new Vector2(0, 100),
            new Vector2(1000, 100));

        var second = new LineSegment2D(
            new Vector2(0, 90),
            new Vector2(1000, 93.490481));

        bool found = _calculator.TryFindIntersection(
            first,
            second,
            out Vector2 point);

        Assert.True(found);

        Assert.Equal(2864.93466, point.X, 4);
        Assert.Equal(100, point.Y, 6);
    }
}

