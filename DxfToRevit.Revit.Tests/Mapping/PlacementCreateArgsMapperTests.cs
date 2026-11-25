using DxfToRevit.Core.Import;
using DxfToRevit.Revit.Common;

namespace DxfToRevit.Revit.Tests.Mapping;

/// <summary>
/// ElementPlacement → level/height/offset in feet (shared by walls and columns).
/// </summary>
public sealed class PlacementCreateArgsMapperTests
{
    private readonly PlacementCreateArgsMapper _mapper = new(3000);

    [Fact]
    public void Map_ExplicitHeightAndLevel_ConvertsToFeet()
    {
        var placement = new ElementPlacement("Level 1", baseOffsetMm: 0, heightMm: 3048);
        PlacementCreateArgs args = _mapper.Map(placement);

        Assert.Equal("Level 1", args.LevelName);
        Assert.Equal(10.0, args.HeightFeet, 10);
        Assert.Equal(3048, args.HeightMm, 6);
        Assert.Equal(0, args.BaseOffsetFeet, 10);
    }

    [Fact]
    public void Map_NullHeight_UsesDefault3000Mm()
    {
        var placement = new ElementPlacement(null);
        PlacementCreateArgs args = _mapper.Map(placement);

        Assert.Null(args.LevelName);
        Assert.Equal(LengthUnitConverter.MmToFeet(3000), args.HeightFeet, 10);
    }

    [Fact]
    public void Map_WithTopLevel_CopiesTopLevelName()
    {
        var placement = new ElementPlacement(
            levelName: "Level 1",
            heightMm: 3000,
            topLevelName: "Level 3");

        PlacementCreateArgs args = _mapper.Map(placement);

        Assert.Equal("Level 1", args.LevelName);
        Assert.Equal("Level 3", args.TopLevelName);
        Assert.Equal(LengthUnitConverter.MmToFeet(3000), args.HeightFeet, 10);
    }

    [Fact]
    public void Map_WithoutTopLevel_KeepsUnconnected()
    {
        var placement = new ElementPlacement(
            levelName: "Level 1",
            heightMm: 3500);

        PlacementCreateArgs args = _mapper.Map(placement);

        Assert.Null(args.TopLevelName);
        Assert.Equal(LengthUnitConverter.MmToFeet(3500), args.HeightFeet, 10);
    }
}
