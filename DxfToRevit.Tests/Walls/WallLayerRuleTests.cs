
using DxfToRevit.Core.Rules;
using Xunit;

namespace DxfToRevit.Tests.Walls;

/// <summary>
/// WallLayerRule: WALL-NN → thickness = NN × 10 mm (WALL-20 → 200 mm).
/// </summary>
public class WallLayerRuleTests
{
    private readonly WallLayerRule _rule = new();

    /// <summary>
    /// TryGetThicknessMm_ValidLayer_ReturnsThickness: Layer/thickness resolution.
    /// </summary>
    /// <summary>

    /// Valid layer names give code × 10 thickness.

    /// </summary>

    [Theory]
    [InlineData("WALL-15", 150)]
    [InlineData("WALL-20", 200)]
    [InlineData("WALL-30", 300)]
    [InlineData("wall-20", 200)]
    [InlineData("Wall-15", 150)]
    [InlineData("  WALL-20  ", 200)]
    public void TryGetThicknessMm_ValidLayer_ReturnsThickness(
        string layerName,
        double expectedThicknessMm)
    {
        bool result = _rule.TryGetThicknessMm(
            layerName,
            out double thicknessMm);

        Assert.True(result);
        Assert.Equal(expectedThicknessMm, thicknessMm);
    }

    /// <summary>
    /// TryGetThicknessMm_InvalidLayer_ReturnsFalse: Layer/thickness resolution.
    /// </summary>
    /// <summary>

    /// WALL, WALL-, harf sonek, 0 kod → false.

    /// </summary>

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("wall")]
    [InlineData("WALL")]
    [InlineData("WALL-")]
    [InlineData("WALL-ABC")]
    [InlineData("WALL-20A")]
    [InlineData("WALL-0")]
    [InlineData("WALL--20")]
    [InlineData("COLUMN-20")]
    public void TryGetThicknessMm_InvalidLayer_ReturnsFalse(
        string layerName)
    {
        bool result = _rule.TryGetThicknessMm(
            layerName,
            out double thicknessMm);

        Assert.False(result);
        Assert.Equal(0, thicknessMm);
    }
}

