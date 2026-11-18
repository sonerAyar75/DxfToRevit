
using DxfToRevit.Core.Geometry;
using netDxf;
using Xunit;

namespace DxfToRevit.Tests.Geometry;

/// <summary>
/// LineSegment2D: basic geometry tests for length, parallelism, distance and overlap along the length.
/// </summary>
public class LineSegment2DTests
{
    /// <summary>
    /// IsParallelTo_HorizontalLines_ShouldReturnTrue: Parallelism / parallel distance check.
    /// </summary>
    [Fact]
    public void IsParallelTo_HorizontalLines_ShouldReturnTrue()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        var lineB = new LineSegment2D(
            new Vector2(0, 200),
            new Vector2(1000, 200));

        Assert.True(lineA.IsParallelTo(lineB));
    }

    /// <summary>
    /// GetDistanceToParallelLine_ShouldReturn200: Parallelism / parallel distance check.
    /// </summary>
    [Fact]
    public void GetDistanceToParallelLine_ShouldReturn200()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        var lineB = new LineSegment2D(
            new Vector2(0, 200),
            new Vector2(1000, 200));

        Assert.Equal(200.0, lineA.GetDistanceToParallelLine(lineB), 6);
    }

    /// <summary>
    /// IsParallelTo_PerpendicularLines_ShouldReturnFalse: Parallelism / parallel distance check.
    /// </summary>
    [Fact]
    public void IsParallelTo_PerpendicularLines_ShouldReturnFalse()
    {
        var horizontal = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        var vertical = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(0, 1000));

        Assert.False(horizontal.IsParallelTo(vertical));
    }

    /// <summary>
    /// GetDistanceToParallelLine_NonParallelLines_ShouldThrow: Parallelism / parallel distance check.
    /// </summary>
    [Fact]
    public void GetDistanceToParallelLine_NonParallelLines_ShouldThrow()
    {
        var horizontal = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        var vertical = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(0, 1000));

        Assert.Throws<InvalidOperationException>(
            () => horizontal.GetDistanceToParallelLine(vertical));
    }
    /// <summary>
    /// OverlapsAlongLength_AlignedParallelLines_ShouldReturnTrue: Parallelism / parallel distance check.
    /// </summary>
    [Fact]
    public void OverlapsAlongLength_AlignedParallelLines_ShouldReturnTrue()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        var lineB = new LineSegment2D(
            new Vector2(200, 200),
            new Vector2(800, 200));

        // Beklenti:
        // The two lines are parallel and overlap along their length.
        Assert.True(lineA.OverlapsAlongLength(lineB));
    }

    /// <summary>
    /// OverlapsAlongLength_SeparatedParallelLines_ShouldReturnFalse: Parallelism / parallel distance check.
    /// </summary>
    [Fact]
    public void OverlapsAlongLength_SeparatedParallelLines_ShouldReturnFalse()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        var lineB = new LineSegment2D(
            new Vector2(1200, 200),
            new Vector2(1800, 200));

        // Beklenti:
        // The lines are parallel but do not overlap along their length.
        Assert.False(lineA.OverlapsAlongLength(lineB));
    }

    /// <summary>
    /// IsParallelTo_VeryShortLines_ShouldReturnTrue: Parallelism / parallel distance check.
    /// </summary>
    [Fact]
    public void IsParallelTo_VeryShortLines_ShouldReturnTrue()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(0.001, 0));

        var lineB = new LineSegment2D(
            new Vector2(0, 0.01),
            new Vector2(0.001, 0.01));

        Assert.True(lineA.IsParallelTo(lineB));
    }

    /// <summary>
    /// IsParallelTo_ShortLinesOutsideTolerance_ShouldReturnFalse: Parallelism / parallel distance check.
    /// </summary>
    [Fact]
    public void IsParallelTo_ShortLinesOutsideTolerance_ShouldReturnFalse()
    {
        double angleDegrees = 0.2;
        double angleRadians = angleDegrees * Math.PI / 180.0;

        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(0.001, 0));

        var lineB = new LineSegment2D(
            new Vector2(0, 0.01),
            new Vector2(
                0.001 * Math.Cos(angleRadians),
                0.01 + 0.001 * Math.Sin(angleRadians)));

        Assert.False(lineA.IsParallelTo(lineB));
    }

    /// <summary>
    /// IsParallelTo_DegenerateLine_ShouldReturnFalse: Parallelism / parallel distance check.
    /// </summary>
    [Fact]
    public void IsParallelTo_DegenerateLine_ShouldReturnFalse()
    {
        var normalLine = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(100, 0));

        var zeroLengthLine = new LineSegment2D(
            new Vector2(50, 50),
            new Vector2(50, 50));

        Assert.False(normalLine.IsParallelTo(zeroLengthLine));
        Assert.False(zeroLengthLine.IsParallelTo(normalLine));
    }

    /// <summary>
    /// IsParallelTo_NullArgument_ShouldThrowArgumentNullException: Null argument → exception.
    /// </summary>
    [Fact]
    public void IsParallelTo_NullArgument_ShouldThrowArgumentNullException()
    {
        var line = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(100, 0));

        Assert.Throws<ArgumentNullException>(
            () => line.IsParallelTo(null!));
    }

    /// <summary>
    /// GetDistanceToParallelLine_NullArgument_ShouldThrowArgumentNullException: Null argument → exception.
    /// </summary>
    [Fact]
    public void GetDistanceToParallelLine_NullArgument_ShouldThrowArgumentNullException()
    {
        var line = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(100, 0));

        Assert.Throws<ArgumentNullException>(
            () => line.GetDistanceToParallelLine(null!));
    }

    /// <summary>
    /// GetDistanceToParallelLine_DegenerateLine_ShouldThrowInvalidOperationException: Parallelism / parallel distance check.
    /// </summary>
    [Fact]
    public void GetDistanceToParallelLine_DegenerateLine_ShouldThrowInvalidOperationException()
    {
        var normalLine = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(100, 0));

        var zeroLengthLine = new LineSegment2D(
            new Vector2(50, 50),
            new Vector2(50, 50));

        Assert.Throws<InvalidOperationException>(
            () => normalLine.GetDistanceToParallelLine(zeroLengthLine));
    }

    /// <summary>
    /// GetDistanceToParallelLine_ReversedArguments_ShouldReturnSameDistance: Parallelism / parallel distance check.
    /// </summary>
    [Fact]
    public void GetDistanceToParallelLine_ReversedArguments_ShouldReturnSameDistance()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(100, 0));

        var lineB = new LineSegment2D(
            new Vector2(0, 200),
            new Vector2(100, 200));

        double distanceAB =
            lineA.GetDistanceToParallelLine(lineB);

        double distanceBA =
            lineB.GetDistanceToParallelLine(lineA);

        Assert.Equal(200, distanceAB, 6);
        Assert.Equal(distanceAB, distanceBA, 6);
    }

    /// <summary>
    /// GetDistanceToParallelLine_ReversedLineDirections_ShouldReturnSameDistance: Parallelism / parallel distance check.
    /// </summary>
    [Fact]
    public void GetDistanceToParallelLine_ReversedLineDirections_ShouldReturnSameDistance()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(100, 0));

        var lineB = new LineSegment2D(
            new Vector2(100, 200),
            new Vector2(0, 200));

        double distance =
            lineA.GetDistanceToParallelLine(lineB);

        Assert.Equal(200, distance, 6);
    }

    /// <summary>
    /// GetDistanceToParallelLine_InclinedLines_ShouldReturnPerpendicularDistance: Parallelism / parallel distance check.
    /// </summary>
    [Fact]
    public void GetDistanceToParallelLine_InclinedLines_ShouldReturnPerpendicularDistance()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(100, 100));

        var lineB = new LineSegment2D(
            new Vector2(0, 100),
            new Vector2(100, 200));

        double distance =
            lineA.GetDistanceToParallelLine(lineB);

        Assert.Equal(70.710678, distance, 5);
    }

    /// <summary>
    /// OverlapsAlongLength_NullArgument_ShouldThrowArgumentNullException: Null argument → exception.
    /// </summary>
    [Fact]
    public void OverlapsAlongLength_NullArgument_ShouldThrowArgumentNullException()
    {
        var line = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(100, 0));

        Assert.Throws<ArgumentNullException>(
            () => line.OverlapsAlongLength(null!));
    }

    /// <summary>
    /// OverlapsAlongLength_DegenerateLine_ShouldReturnFalse: Overlap interval or overlapping segment.
    /// </summary>
    [Fact]
    public void OverlapsAlongLength_DegenerateLine_ShouldReturnFalse()
    {
        var normalLine = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(100, 0));

        var zeroLengthLine = new LineSegment2D(
            new Vector2(50, 50),
            new Vector2(50, 50));

        Assert.False(normalLine.OverlapsAlongLength(zeroLengthLine));
        Assert.False(zeroLengthLine.OverlapsAlongLength(normalLine));
    }

    /// <summary>
    /// OverlapsAlongLength_NonParallelLines_ShouldReturnFalse: Parallelism / parallel distance check.
    /// </summary>
    [Fact]
    public void OverlapsAlongLength_NonParallelLines_ShouldReturnFalse()
    {
        var horizontal = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(100, 0));

        var inclined = new LineSegment2D(
            new Vector2(50, -50),
            new Vector2(50, 50));

        Assert.False(horizontal.OverlapsAlongLength(inclined));
    }

    /// <summary>
    /// OverlapsAlongLength_TouchingAtEndpoint_ShouldReturnFalse: Overlap interval or overlapping segment.
    /// </summary>
    [Fact]
    public void OverlapsAlongLength_TouchingAtEndpoint_ShouldReturnFalse()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(100, 0));

        var lineB = new LineSegment2D(
            new Vector2(100, 50),
            new Vector2(200, 50));

        Assert.False(lineA.OverlapsAlongLength(lineB));
    }

    /// <summary>
    /// OverlapsAlongLength_MinimalOverlap_ShouldReturnTrue: Overlap interval or overlapping segment.
    /// </summary>
    [Fact]
    public void OverlapsAlongLength_MinimalOverlap_ShouldReturnTrue()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(100, 0));

        var lineB = new LineSegment2D(
            new Vector2(99.999, 50),
            new Vector2(200, 50));

        Assert.True(lineA.OverlapsAlongLength(lineB));
    }

    /// <summary>
    /// OverlapsAlongLength_ReversedDirection_ShouldReturnTrue: Overlap interval or overlapping segment.
    /// </summary>
    [Fact]
    public void OverlapsAlongLength_ReversedDirection_ShouldReturnTrue()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        var lineB = new LineSegment2D(
            new Vector2(800, 200),
            new Vector2(200, 200));

        Assert.True(lineA.OverlapsAlongLength(lineB));
        Assert.True(lineB.OverlapsAlongLength(lineA));
    }

    /// <summary>
    /// OverlapsAlongLength_InclinedParallelLines_ShouldReturnTrue: Parallelism / parallel distance check.
    /// </summary>
    [Fact]
    public void OverlapsAlongLength_InclinedParallelLines_ShouldReturnTrue()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(100, 100));

        var lineB = new LineSegment2D(
            new Vector2(25, 125),
            new Vector2(125, 225));

        Assert.True(lineA.OverlapsAlongLength(lineB));
    }

    /// <summary>
    /// Length_DiagonalLine_ReturnsCorrectLength: Valid input / happy path.
    /// </summary>
    [Fact]
    public void Length_DiagonalLine_ReturnsCorrectLength()
    {
        var line = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(300, 400));

        Assert.Equal(500, line.Length, 6);
    }

    /// <summary>
    /// Direction_ReturnsCorrectVector: Valid input / happy path.
    /// </summary>
    [Fact]
    public void Direction_ReturnsCorrectVector()
    {
        var line = new LineSegment2D(
            new Vector2(100, 200),
            new Vector2(400, 600));

        Assert.Equal(300, line.Direction.X, 6);
        Assert.Equal(400, line.Direction.Y, 6);
    }

    /// <summary>
    /// MidPoint_ReturnsCorrectCoordinates: Valid input / happy path.
    /// </summary>
    [Fact]
    public void MidPoint_ReturnsCorrectCoordinates()
    {
        var line = new LineSegment2D(
            new Vector2(100, 200),
            new Vector2(400, 600));

        Assert.Equal(250, line.MidPoint.X, 6);
        Assert.Equal(400, line.MidPoint.Y, 6);
    }

}

