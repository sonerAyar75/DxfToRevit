using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Import;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using netDxf;

namespace DxfToRevit.Revit.Tests.Mapping;

/// <summary>
/// ColumnCreationRequest → ColumnCreateArgs: location, level, height and section in feet.
/// </summary>
public sealed class ColumnCreateArgsMapperTests
{
    private readonly ColumnCreateArgsMapper _mapper = new(3000);

    [Fact]
    public void Map_RectangularColumn_LocationLevelHeightWidthDepth()
    {
        var outline = new[]
        {
            new Vector2(0, 0),
            new Vector2(304.8, 0),
            new Vector2(304.8, 609.6),
            new Vector2(0, 609.6)
        };
        // centroid 152.4, 304.8
        var request = new ColumnCreationRequest(
            "C001",
            outline,
            new Vector2(152.4, 304.8),
            type: new ColumnTypeProperties(
                typeKey: "M_Concrete-Rectangular-Column",
                widthMm: 304.8,
                depthMm: 609.6,
                isStructural: true),
            placement: new ElementPlacement("Level 1", baseOffsetMm: 0, heightMm: 3000),
            sourceLayer: "COLUMN");

        ColumnCreateArgs args = _mapper.Map(request);

        Assert.Equal("C001", args.RequestId);
        Assert.Equal(0.5, args.LocationXFeet, 10);
        Assert.Equal(1.0, args.LocationYFeet, 10);
        Assert.Equal("Level 1", args.Placement.LevelName);
        Assert.Equal(LengthUnitConverter.MmToFeet(3000), args.Placement.HeightFeet, 10);
        Assert.Equal(1.0, args.WidthFeet!.Value, 10);
        Assert.Equal(2.0, args.DepthFeet!.Value, 10);
        Assert.Equal("M_Concrete-Rectangular-Column", args.FamilyTypeKey);
        Assert.True(args.IsStructural);
        Assert.Equal(4, args.OutlineFeet.Count);
        Assert.Equal("COLUMN", args.SourceLayer);
    }

    [Fact]
    public void Map_DefaultPlacement_UsesDefaultHeight()
    {
        var outline = new[]
        {
            new Vector2(0, 0), new Vector2(300, 0),
            new Vector2(300, 300), new Vector2(0, 300)
        };
        var request = new ColumnCreationRequest("C2", outline, new Vector2(150, 150));

        ColumnCreateArgs args = _mapper.Map(request);

        Assert.Equal(LengthUnitConverter.MmToFeet(3000), args.Placement.HeightFeet, 10);
        Assert.Null(args.Placement.LevelName);
        Assert.Null(args.Placement.TopLevelName);
    }

    [Fact]
    public void Map_RotatedColumn_CarriesRotation()
    {
        var outline = new[]
        {
            new Vector2(0, 0), new Vector2(300, 0),
            new Vector2(300, 300), new Vector2(0, 300)
        };
        var request = new ColumnCreationRequest("C4", outline, new Vector2(150, 150), rotationRadians: 0.5);

        ColumnCreateArgs args = _mapper.Map(request);

        Assert.Equal(0.5, args.RotationRadians, 10);
    }

    /// <summary>When the import session has a top level, the column is bound to it as well (same as walls).</summary>
    [Fact]
    public void Map_SessionWithTopLevel_ColumnCarriesTopLevel()
    {
        var session = new ImportSessionOptions(baseLevelName: "Level 1", topLevelName: "Level 2");
        var column = new DxfToRevit.Core.Columns.ColumnModel(
            "C3", "COLUMN", "A1",
            new[] { new Vector2(0, 0), new Vector2(400, 0), new Vector2(400, 400), new Vector2(0, 400) });

        ColumnCreationRequest request = new ColumnCreationRequestFactory(session).FromColumnModels(new[] { column })[0];
        ColumnCreateArgs args = _mapper.Map(request);

        Assert.Equal("Level 1", args.Placement.LevelName);
        Assert.Equal("Level 2", args.Placement.TopLevelName);
    }

}
