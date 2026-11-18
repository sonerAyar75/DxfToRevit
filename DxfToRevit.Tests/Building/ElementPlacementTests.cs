
using DxfToRevit.Core.Building;
using Xunit;

namespace DxfToRevit.Tests.Building;

/// <summary>ElementPlacement validation: positive heights and blank top level names.</summary>
public sealed class ElementPlacementTests
{
    [Fact]
    public void Constructor_Unconnected_SetsHeight()
    {
        var placement = new ElementPlacement(
            levelName: "Level 1",
            heightMm: 3500);

        Assert.Equal("Level 1", placement.LevelName);
        Assert.Null(placement.TopLevelName);
        Assert.Equal(3500, placement.HeightMm);
    }

    [Fact]
    public void Constructor_WithTopLevel_SetsTopLevelName()
    {
        var placement = new ElementPlacement(
            levelName: "Level 1",
            topLevelName: "Level 3");

        Assert.Equal("Level 1", placement.LevelName);
        Assert.Equal("Level 3", placement.TopLevelName);
        Assert.Null(placement.HeightMm);
    }

    [Fact]
    public void Constructor_NonPositiveHeight_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ElementPlacement(heightMm: 0));
    }
}

