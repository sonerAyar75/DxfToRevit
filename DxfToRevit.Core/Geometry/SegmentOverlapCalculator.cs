using netDxf;

namespace DxfToRevit.Core.Geometry;

/// <summary>
/// Computes the overlap interval of two parallel segments along a reference axis.
/// </summary>
public sealed class SegmentOverlapCalculator
{
    /// <summary>
    /// Projects <paramref name="otherSegment"/> onto the local X axis of
    /// <paramref name="referenceSegment"/> and returns the overlapping
    /// parameter range [overlapStart, overlapEnd] in reference length units.
    /// </summary>
    /// <returns>False when there is no positive-length overlap.</returns>
    public bool TryGetOverlapInterval(
        LineSegment2D referenceSegment,
        LineSegment2D otherSegment,
        out double overlapStart,
        out double overlapEnd)
    {
        ArgumentNullException.ThrowIfNull(referenceSegment);
        ArgumentNullException.ThrowIfNull(otherSegment);

        overlapStart = 0;
        overlapEnd = 0;

        if (referenceSegment.IsDegenerate ||
            otherSegment.IsDegenerate ||
            !referenceSegment.IsParallelTo(otherSegment))
        {
            return false;
        }

        var localSystem = new LocalCoordinateSystem2D(
            referenceSegment.Start,
            referenceSegment.Direction);

        Vector2 otherStart = localSystem.ToLocal(otherSegment.Start);
        Vector2 otherEnd = localSystem.ToLocal(otherSegment.End);

        double referenceLength = referenceSegment.Length;
        double otherMin = Math.Min(otherStart.X, otherEnd.X);
        double otherMax = Math.Max(otherStart.X, otherEnd.X);

        overlapStart = Math.Max(0, otherMin);
        overlapEnd = Math.Min(referenceLength, otherMax);

        return overlapEnd > overlapStart;
    }
}
