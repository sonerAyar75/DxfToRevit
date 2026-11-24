using DxfToRevit.Core.Dxf;

namespace DxfToRevit.Core.Walls;

/// <summary>
/// Classifies a single DXF line by its layer name: whether it is a wall line and, if the layer
/// encodes one, the wall thickness in millimetres (see <see cref="WallLayerRule"/>).
/// </summary>
public sealed class DxfLineClassifier
{
    private readonly WallLayerRule _wallLayerRule;

    /// <summary>Creates the classifier.</summary>
    /// <param name="wallLayerRule">Wall layer rule; the default rule when <c>null</c>.</param>
    public DxfLineClassifier(WallLayerRule? wallLayerRule = null)
    {
        _wallLayerRule = wallLayerRule ?? new WallLayerRule();
    }

    /// <summary>Assigns the wall flag and thickness of a line from its layer name.</summary>
    /// <param name="line">Raw line.</param>
    /// <returns>The classified line.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="line"/> is <c>null</c>.</exception>
    public ClassifiedDxfLine Classify(DxfLineModel line)
    {
        ArgumentNullException.ThrowIfNull(line);

        bool isWallLayer = _wallLayerRule.IsWallLayer(line.SourceLayer);

        if (!isWallLayer)
        {
            return new ClassifiedDxfLine(
                line.Geometry,
                line.SourceLayer,
                isWallLayer: false,
                wallThicknessMm: null,
                line.Handle);
        }

        if (_wallLayerRule.TryGetThicknessMm(line.SourceLayer, out double thicknessMm))
        {
            return new ClassifiedDxfLine(
                line.Geometry,
                line.SourceLayer,
                isWallLayer: true,
                wallThicknessMm: thicknessMm,
                line.Handle);
        }

        return new ClassifiedDxfLine(
            line.Geometry,
            line.SourceLayer,
            isWallLayer: true,
            wallThicknessMm: null,
            line.Handle);
    }
}
