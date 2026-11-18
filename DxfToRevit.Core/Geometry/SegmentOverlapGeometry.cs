using netDxf;

namespace DxfToRevit.Core.Geometry;

/// <summary>
/// The overlapping part of two parallel wall faces and the centerline over that stretch.
/// </summary>
public sealed class SegmentOverlapGeometry
{
    /// <summary>Overlapping part on face A.</summary>
    public LineSegment2D FaceAOverlap { get; }

    /// <summary>Overlapping part on face B.</summary>
    public LineSegment2D FaceBOverlap { get; }

    /// <summary>Centerline midway between the two overlapping parts.</summary>
    public LineSegment2D CenterLine { get; }

    private SegmentOverlapGeometry(
        LineSegment2D faceAOverlap,
        LineSegment2D faceBOverlap,
        LineSegment2D centerLine)
    {
        FaceAOverlap = faceAOverlap;
        FaceBOverlap = faceBOverlap;
        CenterLine = centerLine;
    }

    /// <summary>
    /// Tries to build the overlap geometry of two parallel faces. The overlap is measured along
    /// face A's axis; face B is trimmed to the same stretch.
    /// </summary>
    /// <param name="faceA">First face.</param>
    /// <param name="faceB">Second face.</param>
    /// <param name="result">The overlap geometry when successful.</param>
    /// <returns><c>false</c> when a face is degenerate, the faces are not parallel or they do not overlap.</returns>
    public static bool TryCreate(
        LineSegment2D faceA,
        LineSegment2D faceB,
        out SegmentOverlapGeometry? result)
    {
        ArgumentNullException.ThrowIfNull(faceA);
        ArgumentNullException.ThrowIfNull(faceB);

        result = null;

        if (faceA.IsDegenerate || faceB.IsDegenerate || !faceA.IsParallelTo(faceB))
            return false;

        var calculator = new SegmentOverlapCalculator();
        if (!calculator.TryGetOverlapInterval(
                faceA, faceB, out double overlapStart, out double overlapEnd))
            return false;

        var localSystem = new LocalCoordinateSystem2D(faceA.Start, faceA.Direction);
        Vector2 faceBStartLocal = localSystem.ToLocal(faceB.Start);

        Vector2 faceAStart = localSystem.ToWorld(new Vector2(overlapStart, 0));
        Vector2 faceAEnd = localSystem.ToWorld(new Vector2(overlapEnd, 0));
        Vector2 faceBStart = localSystem.ToWorld(new Vector2(overlapStart, faceBStartLocal.Y));
        Vector2 faceBEnd = localSystem.ToWorld(new Vector2(overlapEnd, faceBStartLocal.Y));

        var faceAOverlap = new LineSegment2D(faceAStart, faceAEnd);
        var faceBOverlap = new LineSegment2D(faceBStart, faceBEnd);
        var centerLine = new LineSegment2D(
            (faceAStart + faceBStart) / 2.0,
            (faceAEnd + faceBEnd) / 2.0);

        result = new SegmentOverlapGeometry(faceAOverlap, faceBOverlap, centerLine);
        return true;
    }
}
