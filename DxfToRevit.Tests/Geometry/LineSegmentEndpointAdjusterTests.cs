
using DxfToRevit.Core.Geometry;
using netDxf;
using Xunit;

namespace DxfToRevit.Tests.Core.Geometry;

/// <summary>
/// Endpoint extension at L/T junctions: rules for moving endpoints onto the intersection.
/// </summary>
public sealed class LineSegmentEndpointAdjusterTests
{
    private readonly LineSegmentEndpointAdjuster _adjuster = new();

    /// <summary>
    /// TryJoinAtIntersection_ExtendsBothEndpoints: Line intersection.
    /// </summary>
    [Fact]
    public void TryJoinAtIntersection_ExtendsBothEndpoints()
    {
        var horizontal = new LineSegment2D(
            new Vector2(0, 100),
            new Vector2(80, 100));

        var vertical = new LineSegment2D(
            new Vector2(100, 0),
            new Vector2(100, 80));

        var intersection = new Vector2(100, 100);

        bool joined = _adjuster.TryJoinAtIntersection(
            horizontal,
            vertical,
            intersection,
            out var adjustedA,
            out var adjustedB);

        Assert.True(joined);
        Assert.NotNull(adjustedA);
        Assert.NotNull(adjustedB);

        Assert.Equal(100, adjustedA!.End.X, 6);
        Assert.Equal(100, adjustedA.End.Y, 6);

        Assert.Equal(100, adjustedB!.End.X, 6);
        Assert.Equal(100, adjustedB.End.Y, 6);
    }

    /// <summary>
    /// TryJoinAtIntersection_IntersectionInsideSegment_ReturnsFalse: Line intersection.
    /// </summary>
    [Fact]
    public void TryJoinAtIntersection_IntersectionInsideSegment_ReturnsFalse()
    {
        var horizontal = new LineSegment2D(
            new Vector2(0, 100),
            new Vector2(200, 100));

        var vertical = new LineSegment2D(
            new Vector2(100, 0),
            new Vector2(100, 200));

        var intersection = new Vector2(100, 100);

        bool joined = _adjuster.TryJoinAtIntersection(
            horizontal,
            vertical,
            intersection,
            out var adjustedA,
            out var adjustedB);

        Assert.False(joined);
        Assert.Null(adjustedA);
        Assert.Null(adjustedB);
    }


    /// <summary>
    /// TryExtendBranchToMainLine_ExtendsBranchToMainLine: branch extension scenario.
    /// </summary>
    [Fact]
    public void TryExtendBranchToMainLine_ExtendsBranchToMainLine()
    {
        var mainLine = new LineSegment2D(
            new Vector2(0, 100),
            new Vector2(200, 100));

        var branchLine = new LineSegment2D(
            new Vector2(100, 0),
            new Vector2(100, 80));

        var intersection = new Vector2(100, 100);

        bool extended = _adjuster.TryExtendBranchToMainLine(
            mainLine,
            branchLine,
            intersection,
            out var adjustedBranch);

        Assert.True(extended);
        Assert.NotNull(adjustedBranch);

        // The branch end must reach the main line.
        Assert.Equal(100, adjustedBranch!.End.X, 6);
        Assert.Equal(100, adjustedBranch.End.Y, 6);

        // The main line must stay unchanged.
        Assert.Equal(0, mainLine.Start.X, 6);
        Assert.Equal(200, mainLine.End.X, 6);
        Assert.Equal(100, mainLine.Start.Y, 6);
        Assert.Equal(100, mainLine.End.Y, 6);
    }


    /// <summary>
    /// TryJoinAtIntersection_InclinedLines_JoinsAtIntersection: Line intersection.
    /// </summary>
    [Fact]
    public void TryJoinAtIntersection_InclinedLines_JoinsAtIntersection()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(80, 0));

        var lineB = new LineSegment2D(
            new Vector2(100, 30),
            new Vector2(80, 0));

        var intersection = new Vector2(80, 0);

        bool joined = _adjuster.TryJoinAtIntersection(
            lineA,
            lineB,
            intersection,
            out var adjustedA,
            out var adjustedB);

        Assert.True(joined);
        Assert.NotNull(adjustedA);
        Assert.NotNull(adjustedB);

        Assert.Equal(80, adjustedA!.End.X, 6);
        Assert.Equal(0, adjustedA.End.Y, 6);

        Assert.Equal(80, adjustedB!.End.X, 6);
        Assert.Equal(0, adjustedB.End.Y, 6);
    }


    /// <summary>
    /// TryJoinAtIntersection_ExtendsInclinedLinesToIntersection: Line intersection.
    /// </summary>
    [Fact]
    public void TryJoinAtIntersection_ExtendsInclinedLinesToIntersection()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(40, 40));

        var lineB = new LineSegment2D(
            new Vector2(60, 40),
            new Vector2(80, 20));

        var intersectionCalculator = new LineIntersectionCalculator();

        bool found = intersectionCalculator.TryFindIntersection(
            lineA,
            lineB,
            out Vector2 intersection);

        Assert.True(found);
        Assert.Equal(50, intersection.X, 6);
        Assert.Equal(50, intersection.Y, 6);

        bool joined = _adjuster.TryJoinAtIntersection(
            lineA,
            lineB,
            intersection,
            out var adjustedA,
            out var adjustedB);

        Assert.True(joined);
        Assert.NotNull(adjustedA);
        Assert.NotNull(adjustedB);

        Assert.Equal(50, adjustedA!.End.X, 6);
        Assert.Equal(50, adjustedA.End.Y, 6);

        Assert.Equal(50, adjustedB!.Start.X, 6);
        Assert.Equal(50, adjustedB.Start.Y, 6);
    }


    /// <summary>
    /// TryJoinAtIntersection_WithinLimit_JoinsLines: Line intersection.
    /// </summary>
    [Fact]
    public void TryJoinAtIntersection_WithinLimit_JoinsLines()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(40, 40));

        var lineB = new LineSegment2D(
            new Vector2(60, 40),
            new Vector2(80, 20));

        var calculator = new LineIntersectionCalculator();

        Assert.True(calculator.TryFindIntersection(
            lineA,
            lineB,
            out Vector2 intersection));

        bool joined = _adjuster.TryJoinAtIntersection(
            lineA,
            lineB,
            intersection,
            20,
            out var adjustedA,
            out var adjustedB);

        Assert.True(joined);
        Assert.NotNull(adjustedA);
        Assert.NotNull(adjustedB);
    }

    /// <summary>
    /// TryJoinAtIntersection_ExceedsLimit_ReturnsFalse: Line intersection.
    /// </summary>
    [Fact]
    public void TryJoinAtIntersection_ExceedsLimit_ReturnsFalse()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(40, 40));

        var lineB = new LineSegment2D(
            new Vector2(60, 40),
            new Vector2(80, 20));

        var calculator = new LineIntersectionCalculator();

        Assert.True(calculator.TryFindIntersection(
            lineA,
            lineB,
            out Vector2 intersection));

        bool joined = _adjuster.TryJoinAtIntersection(
            lineA,
            lineB,
            intersection,
            10,
            out var adjustedA,
            out var adjustedB);

        Assert.False(joined);
        Assert.Null(adjustedA);
        Assert.Null(adjustedB);
    }


    /// <summary>
    /// TryExtendBranchToMainLine_InclinedT_JoinsAtIntersection: Line intersection.
    /// </summary>
    [Fact]
    public void TryExtendBranchToMainLine_InclinedT_JoinsAtIntersection()
    {
        // Inclined main wall
        var mainLine = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(200, 200));

        // Branch wall in a different direction.
        // The branch end does not reach the main line.
        var branchLine = new LineSegment2D(
            new Vector2(80, 120),
            new Vector2(90, 100));

        var intersectionCalculator = new LineIntersectionCalculator();

        bool found = intersectionCalculator.TryFindIntersection(
            mainLine,
            branchLine,
            out Vector2 intersection);

        Assert.True(found, "1: Intersection not found.");

        // Intersection of the two infinite lines: (110,110).
        Assert.Equal(93.333333, intersection.X, 5);
        Assert.Equal(93.333333, intersection.Y, 5);

        bool extended = _adjuster.TryExtendBranchToMainLine(
            mainLine,
            branchLine,
            intersection,
            out var adjustedBranch);

        Assert.True(extended, "2: Branch extension failed.");
        Assert.NotNull(adjustedBranch);


        // The branch line is expected to reach the intersection.
        Assert.Equal(93.333333, adjustedBranch!.End.X, 5);
        Assert.Equal(93.333333, adjustedBranch.End.Y, 5);

        // The main line must stay unchanged.
        Assert.Equal(0, mainLine.Start.X, 6);
        Assert.Equal(0, mainLine.Start.Y, 6);
        Assert.Equal(200, mainLine.End.X, 6);
        Assert.Equal(200, mainLine.End.Y, 6);
    }


    /// <summary>
    /// TryExtendBranchToMainLine_WithinLimit_ExtendsBranch: branch extension scenario.
    /// </summary>
    [Fact]
    public void TryExtendBranchToMainLine_WithinLimit_ExtendsBranch()
    {
        var mainLine = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(200, 200));

        var branchLine = new LineSegment2D(
            new Vector2(80, 120),
            new Vector2(90, 100));

        var calculator = new LineIntersectionCalculator();

        Assert.True(calculator.TryFindIntersection(
            mainLine,
            branchLine,
            out Vector2 intersection));

        bool extended = _adjuster.TryExtendBranchToMainLine(
            mainLine,
            branchLine,
            intersection,
            10,
            out var adjustedBranch);

        Assert.True(extended);
        Assert.NotNull(adjustedBranch);

        Assert.Equal(93.333333, adjustedBranch!.End.X, 5);
        Assert.Equal(93.333333, adjustedBranch.End.Y, 5);
    }

    /// <summary>
    /// TryExtendBranchToMainLine_ExceedsLimit_ReturnsFalse: Valid input / happy path.
    /// </summary>
    [Fact]
    public void TryExtendBranchToMainLine_ExceedsLimit_ReturnsFalse()
    {
        var mainLine = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(200, 200));

        var branchLine = new LineSegment2D(
            new Vector2(80, 120),
            new Vector2(90, 100));

        var calculator = new LineIntersectionCalculator();

        Assert.True(calculator.TryFindIntersection(
            mainLine,
            branchLine,
            out Vector2 intersection));

        bool extended = _adjuster.TryExtendBranchToMainLine(
            mainLine,
            branchLine,
            intersection,
            4,
            out var adjustedBranch);

        Assert.False(extended);
        Assert.Null(adjustedBranch);
    }


    /// <summary>
    /// TryJoinAtIntersection_PointNotOnBothLines_ReturnsFalse: Line intersection.
    /// </summary>
    [Fact]
    public void TryJoinAtIntersection_PointNotOnBothLines_ReturnsFalse()
    {
        var horizontal = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(100, 0));

        var vertical = new LineSegment2D(
            new Vector2(100, 20),
            new Vector2(100, 100));

        // The point lies on the vertical line but not on the horizontal line.
        var invalidIntersection = new Vector2(100, 10);

        bool joined = _adjuster.TryJoinAtIntersection(
            horizontal,
            vertical,
            invalidIntersection,
            out var adjustedA,
            out var adjustedB);

        Assert.False(joined);
        Assert.Null(adjustedA);
        Assert.Null(adjustedB);
    }



    /// <summary>
    /// TryJoinAtIntersection_LJunction_ExtendsBothLines: Line intersection.
    /// </summary>
    [Fact]
    public void TryJoinAtIntersection_LJunction_ExtendsBothLines()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(100, 0));

        var lineB = new LineSegment2D(
            new Vector2(100, 20),
            new Vector2(100, 100));

        var intersectionCalculator = new LineIntersectionCalculator();

        bool found = intersectionCalculator.TryFindIntersection(
            lineA,
            lineB,
            out Vector2 intersection);

        Assert.True(found, "Intersection not found.");

        Assert.Equal(100, intersection.X, 6);
        Assert.Equal(0, intersection.Y, 6);

        bool joined = _adjuster.TryJoinAtIntersection(
            lineA,
            lineB,
            intersection,
            20,
            out var adjustedA,
            out var adjustedB);

        Assert.True(joined, "L-junction failed.");
        Assert.NotNull(adjustedA);
        Assert.NotNull(adjustedB);

        // The first line must stay unchanged.
        Assert.Equal(0, adjustedA!.Start.X, 6);
        Assert.Equal(0, adjustedA.Start.Y, 6);
        Assert.Equal(100, adjustedA.End.X, 6);
        Assert.Equal(0, adjustedA.End.Y, 6);

        // The start of the second line must move to the junction point.
        Assert.Equal(100, adjustedB!.Start.X, 6);
        Assert.Equal(0, adjustedB.Start.Y, 6);
    }


    /// <summary>
    /// TryExtendBranchToMainLine_TJunction_ExtendsOnlyBranch: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void TryExtendBranchToMainLine_TJunction_ExtendsOnlyBranch()
    {
        // Main line
        var mainLine = new LineSegment2D(
            new Vector2(0, 100),
            new Vector2(200, 100));

        // Branch line: ends before reaching the main line.
        var branchLine = new LineSegment2D(
            new Vector2(100, 0),
            new Vector2(100, 80));

        var intersectionCalculator = new LineIntersectionCalculator();

        bool found = intersectionCalculator.TryFindIntersection(
            mainLine,
            branchLine,
            out Vector2 intersection);

        Assert.True(found, "Intersection not found.");

        Assert.Equal(100, intersection.X, 6);
        Assert.Equal(100, intersection.Y, 6);

        bool extended = _adjuster.TryExtendBranchToMainLine(
            mainLine,
            branchLine,
            intersection,
            20,
            out var adjustedBranch);

        Assert.True(extended, "T-junction extension failed.");
        Assert.NotNull(adjustedBranch);

        // The branch line must reach the main line.
        Assert.Equal(100, adjustedBranch!.End.X, 6);
        Assert.Equal(100, adjustedBranch.End.Y, 6);

        // The main line must not change.
        Assert.Equal(0, mainLine.Start.X, 6);
        Assert.Equal(100, mainLine.Start.Y, 6);
        Assert.Equal(200, mainLine.End.X, 6);
        Assert.Equal(100, mainLine.End.Y, 6);
    }


    /// <summary>
    /// TryJoinAtIntersection_InclinedLJunction_JoinsBothLines: Line intersection.
    /// </summary>
    [Fact]
    public void TryJoinAtIntersection_InclinedLJunction_JoinsBothLines()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(40, 40));

        var lineB = new LineSegment2D(
            new Vector2(60, 40),
            new Vector2(80, 20));

        var calculator = new LineIntersectionCalculator();

        bool found = calculator.TryFindIntersection(
            lineA,
            lineB,
            out Vector2 intersection);

        Assert.True(found, "Intersection not found.");

        Assert.Equal(50, intersection.X, 6);
        Assert.Equal(50, intersection.Y, 6);

        bool joined = _adjuster.TryJoinAtIntersection(
            lineA,
            lineB,
            intersection,
            15,
            out var adjustedA,
            out var adjustedB);

        Assert.True(joined, "Inclined L-junction failed.");
        Assert.NotNull(adjustedA);
        Assert.NotNull(adjustedB);

        Assert.Equal(50, adjustedA!.End.X, 6);
        Assert.Equal(50, adjustedA.End.Y, 6);

        Assert.Equal(50, adjustedB!.Start.X, 6);
        Assert.Equal(50, adjustedB.Start.Y, 6);
    }


    /// <summary>
    /// TryExtendBranchToMainLine_PointNotOnBranchLine_ReturnsFalse: Valid input / happy path.
    /// </summary>
    [Fact]
    public void TryExtendBranchToMainLine_PointNotOnBranchLine_ReturnsFalse()
    {
        var mainLine = new LineSegment2D(
            new Vector2(0, 100),
            new Vector2(200, 100));

        var branchLine = new LineSegment2D(
            new Vector2(100, 0),
            new Vector2(110, 80));

        // The point lies on the main line but not on the branch line.
        var invalidIntersection = new Vector2(100, 100);

        bool extended = _adjuster.TryExtendBranchToMainLine(
            mainLine,
            branchLine,
            invalidIntersection,
            out var adjustedBranch);

        Assert.False(extended);
        Assert.Null(adjustedBranch);
    }


    /// <summary>
    /// TryJoinAtIntersection_PointAtEndpoint_ReturnsTrue: Line intersection.
    /// </summary>
    [Fact]
    public void TryJoinAtIntersection_PointAtEndpoint_ReturnsTrue()
    {
        var lineA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(100, 0));

        var lineB = new LineSegment2D(
            new Vector2(100, 0),
            new Vector2(100, 100));

        var intersection = new Vector2(100, 0);

        bool joined = _adjuster.TryJoinAtIntersection(
            lineA,
            lineB,
            intersection,
            out var adjustedA,
            out var adjustedB);

        Assert.True(joined);
        Assert.NotNull(adjustedA);
        Assert.NotNull(adjustedB);

        Assert.Equal(100, adjustedA!.End.X, 6);
        Assert.Equal(0, adjustedA.End.Y, 6);

        Assert.Equal(100, adjustedB!.Start.X, 6);
        Assert.Equal(0, adjustedB.Start.Y, 6);
    }





}

