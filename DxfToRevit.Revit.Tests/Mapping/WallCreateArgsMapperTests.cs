using DxfToRevit.Core.Import;
using DxfToRevit.Core.Walls;
using netDxf;

namespace DxfToRevit.Revit.Tests.Mapping;

/// <summary>
/// WallCreationRequest (mm) → WallCreateArgs (feet): API-free verification of the Revit create inputs.
/// </summary>
public sealed class WallCreateArgsMapperTests
{
    private readonly WallCreateArgsMapper _mapper = new(defaultHeightMm: 3000);

    /// <summary>
    /// 0,0 → 304.8,0 mm (1 ft uzunluk), height 3048 mm (10 ft), Level 1, Basic Wall, 200 mm.
    /// </summary>
    [Fact]
    public void Map_HorizontalOneFootSegment_ConvertsUnitsAndCopiesNames()
    {
        var request = new WallCreationRequest(
            id: "W001",
            startMm: new Vector2(0, 0),
            endMm: new Vector2(304.8, 0),
            thicknessMm: 200,
            levelName: "Level 1",
            heightMm: 3048,
            wallTypeKey: "Basic Wall",
            isStructural: true,
            baseOffsetMm: 0);

        WallCreateArgs args = _mapper.Map(request);

        Assert.Equal("W001", args.RequestId);
        Assert.Equal(0, args.StartXFeet, 10);
        Assert.Equal(0, args.StartYFeet, 10);
        Assert.Equal(1.0, args.EndXFeet, 10);
        Assert.Equal(0, args.EndYFeet, 10);
        Assert.Equal(1.0, args.LengthFeet, 10);
        Assert.Equal(10.0, args.HeightFeet, 10);
        Assert.Equal(0, args.BaseOffsetFeet, 10);
        Assert.Equal("Level 1", args.LevelName);
        Assert.Equal("Basic Wall", args.WallTypeKey);
        Assert.True(args.IsStructural);
        Assert.Equal(200, args.ThicknessMm, 6);
    }

    /// <summary>
    /// Height null → DefaultHeightMm (3000) → 3000/304.8 feet.
    /// </summary>
    [Fact]
    public void Map_NullHeight_UsesDefaultHeightMm()
    {
        var request = new WallCreationRequest(
            "W002",
            new Vector2(0, 0),
            new Vector2(1000, 0),
            thicknessMm: 150);

        WallCreateArgs args = _mapper.Map(request);

        Assert.Equal(LengthUnitConverter.MmToFeet(3000), args.HeightFeet, 10);
        Assert.Equal("Basic Wall", args.WallTypeKey);
        Assert.Null(args.LevelName);
        Assert.False(args.IsStructural);
    }

    /// <summary>
    /// Base offset 152.4 mm → 0.5 ft.
    /// </summary>
    [Fact]
    public void Map_BaseOffset_ConvertsToFeet()
    {
        var request = new WallCreationRequest(
            "W003",
            new Vector2(0, 0),
            new Vector2(500, 0),
            thicknessMm: 200,
            baseOffsetMm: 152.4);

        WallCreateArgs args = _mapper.Map(request);

        Assert.Equal(0.5, args.BaseOffsetFeet, 10);
    }

    /// <summary>Dejenere segment → ArgumentException.</summary>
    [Fact]
    public void Map_Degenerate_Throws()
    {
        var request = new WallCreationRequest(
            "W004",
            new Vector2(1, 1),
            new Vector2(1, 1),
            thicknessMm: 200);

        Assert.Throws<ArgumentException>(() => _mapper.Map(request));
    }

    /// <summary>MapMany: iki istek → iki args, id korunur.</summary>
    [Fact]
    public void MapMany_PreservesOrderAndIds()
    {
        var requests = new[]
        {
            new WallCreationRequest("A", new Vector2(0, 0), new Vector2(100, 0), 200),
            new WallCreationRequest("B", new Vector2(0, 0), new Vector2(0, 100), 150)
        };

        IReadOnlyList<WallCreateArgs> list = _mapper.MapMany(requests);

        Assert.Equal(2, list.Count);
        Assert.Equal("A", list[0].RequestId);
        Assert.Equal("B", list[1].RequestId);
        Assert.Equal(150, list[1].ThicknessMm);
    }


    [Fact]
    public void Map_WithTopLevel_CopiesTopLevelName()
    {
        var placement = new ElementPlacement(
            levelName: "Level 1",
            topLevelName: "Level 3");

        var request = new WallCreationRequest(
            "W005",
            new Vector2(0, 0),
            new Vector2(1000, 0),
            new WallTypeProperties(200, "Basic Wall", false),
            placement);

        WallCreateArgs args = _mapper.Map(request);

        Assert.Equal("Level 1", args.LevelName);
        Assert.Equal("Level 3", args.TopLevelName);
    }

    [Fact]
    public void Map_WithoutTopLevel_KeepsUnconnected()
    {
        var placement = new ElementPlacement(
            levelName: "Level 1",
            heightMm: 3500);

        var request = new WallCreationRequest(
            "W006",
            new Vector2(0, 0),
            new Vector2(1000, 0),
            new WallTypeProperties(200, "Basic Wall", false),
            placement);

        WallCreateArgs args = _mapper.Map(request);

        Assert.Null(args.TopLevelName);
        Assert.Equal(
            LengthUnitConverter.MmToFeet(3500),
            args.HeightFeet,
            10);
    }

}
