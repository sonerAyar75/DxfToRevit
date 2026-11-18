using DxfToRevit.Core.Models;
using DxfToRevit.Core.Rules;

namespace DxfToRevit.Core.Services;

/// <summary>
/// Classifies a single DXF line by its layer name: whether it is a wall line and, if the layer
/// encodes one, the wall thickness in millimetres (see <see cref="WallLayerRule"/>).
/// </summary>
public sealed class DxfLineClassifier
{
    private readonly WallLayerRule _wallLayerRule = new();

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
