using DxfToRevit.Core.Geometry;
using netDxf;

namespace DxfToRevit.Tests.Geometry;

/// <summary>
/// Building the overlap parts and the middle centerline from two parallel faces.
/// </summary>
public class SegmentOverlapGeometryTests
{
    /// <summary>
    /// TryCreate_DifferentLengthFaces_ReturnsCorrectGeometry: Model creation.
    /// </summary>
    [Fact]
    public void TryCreate_DifferentLengthFaces_ReturnsCorrectGeometry()
    {
        var faceA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        var faceB = new LineSegment2D(
            new Vector2(200, 200),
            new Vector2(800, 200));

        bool result = SegmentOverlapGeometry.TryCreate(
            faceA,
            faceB,
            out var geometry);

        Assert.True(result);
        Assert.NotNull(geometry);

        Assert.Equal(600, geometry!.FaceAOverlap.Length, 6);
        Assert.Equal(600, geometry.FaceBOverlap.Length, 6);
        Assert.Equal(600, geometry.CenterLine.Length, 6);

        Assert.Equal(200, geometry.CenterLine.Start.X, 6);
        Assert.Equal(100, geometry.CenterLine.Start.Y, 6);

        Assert.Equal(800, geometry.CenterLine.End.X, 6);
        Assert.Equal(100, geometry.CenterLine.End.Y, 6);
    }

    /// <summary>
    /// TryCreate_ReversedFaceDirection_ReturnsCorrectGeometry: Model creation.
    /// </summary>
    [Fact]
    public void TryCreate_ReversedFaceDirection_ReturnsCorrectGeometry()
    {
        var faceA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        var faceB = new LineSegment2D(
            new Vector2(800, 200),
            new Vector2(200, 200));

        bool result = SegmentOverlapGeometry.TryCreate(
            faceA,
            faceB,
            out var geometry);

        Assert.True(result);
        Assert.NotNull(geometry);

        Assert.Equal(600, geometry!.FaceAOverlap.Length, 6);
        Assert.Equal(600, geometry.FaceBOverlap.Length, 6);
        Assert.Equal(600, geometry.CenterLine.Length, 6);

        Assert.Equal(200, geometry.CenterLine.Start.X, 6);
        Assert.Equal(100, geometry.CenterLine.Start.Y, 6);

        Assert.Equal(800, geometry.CenterLine.End.X, 6);
        Assert.Equal(100, geometry.CenterLine.End.Y, 6);
    }


    /// <summary>
    /// TryCreate_SlightlyAngledFaces_ReturnsValidGeometry: Slightly angled faces scenario.
    /// </summary>
    [Fact]
    public void TryCreate_SlightlyAngledFaces_ReturnsValidGeometry()
    {
        var faceA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        // An angle of about 0.05 degrees.
        var faceB = new LineSegment2D(
            new Vector2(200, 200),
            new Vector2(800, 200.5236));

        bool success = SegmentOverlapGeometry.TryCreate(
            faceA,
            faceB,
            out SegmentOverlapGeometry? result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.NotNull(result!.CenterLine);

        Assert.Equal(200, result!.FaceAOverlap.Start.X, 6);
        Assert.Equal(800, result.FaceAOverlap.End.X, 6);

        Assert.Equal(200, result.FaceBOverlap.Start.X, 6);
        Assert.Equal(800, result.FaceBOverlap.End.X, 6);

        Assert.Equal(600, result.CenterLine.Length, 3);
    }



}
