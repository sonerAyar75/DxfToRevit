using DxfToRevit.Core.Geometry;

namespace DxfToRevit.Core.Walls;

/// <summary>
/// Creates <see cref="WallModel"/>s from face pairs or from ready centerlines. For face pairs the
/// centerline runs midway between the overlapping parts of the two faces.
/// </summary>
public sealed class WallModelManager
{
    private readonly WallLayerRule _layerRule = new();

    /// <summary>
    /// Creates a wall from an existing centerline. The thickness comes from the layer name; when the
    /// layer has no thickness code, <paramref name="userThicknessMm"/> is used.
    /// </summary>
    /// <param name="id">Wall identifier.</param>
    /// <param name="sourceLayer">Layer name.</param>
    /// <param name="centerLine">Wall centerline.</param>
    /// <param name="userThicknessMm">Thickness to use when the layer does not define one.</param>
    /// <returns>The wall model.</returns>
    /// <exception cref="InvalidOperationException">Neither the layer nor the caller provides a thickness.</exception>
    public WallModel CreateFromCenterLine(
        string id,
        string sourceLayer,
        LineSegment2D centerLine,
        double? userThicknessMm = null)
    {
        double thicknessMm;

        if (_layerRule.TryGetThicknessMm(sourceLayer, out double layerThicknessMm))
            thicknessMm = layerThicknessMm;
        else if (userThicknessMm.HasValue)
            thicknessMm = userThicknessMm.Value;
        else
        {
            throw new InvalidOperationException(
                $"Wall thickness is not defined for layer '{sourceLayer}'. " +
                "A thickness must be supplied by the user.");
        }

        return new WallModel(id, sourceLayer, thicknessMm, centerLine: centerLine);
    }

    /// <summary>
    /// Creates a wall from a face pair. The faces are trimmed to their overlapping stretch and the
    /// centerline is placed midway between them.
    /// </summary>
    /// <param name="id">Wall identifier.</param>
    /// <param name="facePair">Parallel face pair.</param>
    /// <returns>The wall model with centerline and trimmed faces.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="facePair"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The faces do not overlap.</exception>
    public WallModel CreateFromFacePair(string id, WallFacePair facePair)
    {
        ArgumentNullException.ThrowIfNull(facePair);

        if (!SegmentOverlapGeometry.TryCreate(
                facePair.FaceA,
                facePair.FaceB,
                out SegmentOverlapGeometry? geometry) || geometry is null)
        {
            throw new InvalidOperationException(
                $"Wall face pair '{facePair.FaceAHandle}' / " +
                $"'{facePair.FaceBHandle}' does not have a valid overlap.");
        }

        return new WallModel(
            id,
            facePair.SourceLayer,
            facePair.ThicknessMm,
            centerLine: geometry.CenterLine,
            faceA: geometry.FaceAOverlap,
            faceB: geometry.FaceBOverlap);
    }
}
