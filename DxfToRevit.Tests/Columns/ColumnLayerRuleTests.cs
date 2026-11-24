using DxfToRevit.Core.Columns;

namespace DxfToRevit.Tests.Columns;

/// <summary>
/// ColumnLayerRule: COLUMN, COLUMNS and COLUMN-* count as column layers.
/// </summary>
public sealed class ColumnLayerRuleTests
{
    private readonly ColumnLayerRule _rule = new();

    [Theory]
    [InlineData("COLUMN")]
    [InlineData("column")]
    [InlineData("COLUMNS")]
    [InlineData("COLUMN-600")]
    public void IsColumnLayer_ValidNames_ReturnsTrue(string name)
    {
        Assert.True(_rule.IsColumnLayer(name));
    }

    [Theory]
    [InlineData("WALL-20")]
    [InlineData("COL")]
    [InlineData("")]
    [InlineData(null)]
    public void IsColumnLayer_InvalidNames_ReturnsFalse(string? name)
    {
        Assert.False(_rule.IsColumnLayer(name));
    }
}
