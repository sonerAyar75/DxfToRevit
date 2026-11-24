using DxfToRevit.Core.Geometry;
using netDxf;

namespace DxfToRevit.Tests.Geometry;

/// <summary>
/// Parallel segment overlap: intersection of the other segment's projected interval with the reference [0, L].
/// </summary>
public class SegmentOverlapCalculatorTests
{
    /// <summary>
    /// TryGetOverlapInterval_DifferentLengthSegments_ReturnsCommonInterval: Overlap interval or overlapping segment.
    /// </summary>
    [Fact]
    public void TryGetOverlapInterval_DifferentLengthSegments_ReturnsCommonInterval()
    {
        var reference = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        var other = new LineSegment2D(
            new Vector2(200, 200),
            new Vector2(800, 200));

        var calculator = new SegmentOverlapCalculator();

        bool result = calculator.TryGetOverlapInterval(
            reference,
            other,
            out double overlapStart,
            out double overlapEnd);

        Assert.True(result);
        Assert.Equal(200, overlapStart, 6);
        Assert.Equal(800, overlapEnd, 6);
    }

    /// <summary>
    /// TryGetOverlapInterval_ReversedSegment_ReturnsCommonInterval: Overlap interval or overlapping segment.
    /// </summary>
    [Fact]
    public void TryGetOverlapInterval_ReversedSegment_ReturnsCommonInterval()
    {
        var reference = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        var other = new LineSegment2D(
            new Vector2(800, 200),
            new Vector2(200, 200));

        var calculator = new SegmentOverlapCalculator();

        bool result = calculator.TryGetOverlapInterval(
            reference,
            other,
            out double overlapStart,
            out double overlapEnd);

        Assert.True(result);
        Assert.Equal(200, overlapStart, 6);
        Assert.Equal(800, overlapEnd, 6);
    }
}
