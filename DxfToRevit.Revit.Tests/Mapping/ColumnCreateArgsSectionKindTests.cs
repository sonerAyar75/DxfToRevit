using DxfToRevit.Core.Columns;
using DxfToRevit.Revit.Columns;
using netDxf;

namespace DxfToRevit.Revit.Tests.Mapping;

/// <summary>ColumnCreateArgs carries the section kind and diameter from the request.</summary>
public sealed class ColumnCreateArgsSectionKindTests
{
    [Fact]
    public void Mapper_CarriesSectionKindAndDiameter()
    {
        var outline = new[]
        {
            new Vector2(0, 0), new Vector2(600, 0), new Vector2(600, 600), new Vector2(0, 600)
        };
        var type = new ColumnTypeProperties(
            "Round Column", widthMm: 600, depthMm: 600, isStructural: false,
            sectionKind: ColumnSectionKind.Circular, radiusMm: 300);
        var request = new ColumnCreationRequest("C001", outline, new Vector2(300, 300), type);

        ColumnCreateArgs args = new ColumnCreateArgsMapper(3000).Map(request);

        Assert.Equal(ColumnSectionKind.Circular, args.SectionKind);
        Assert.Equal(600, args.DiameterMm);
    }

    [Fact]
    public void Mapper_Rectangular_HasNoDiameter()
    {
        var outline = new[]
        {
            new Vector2(0, 0), new Vector2(400, 0), new Vector2(400, 300), new Vector2(0, 300)
        };
        var type = new ColumnTypeProperties("Rectangular Column", widthMm: 400, depthMm: 300);
        var request = new ColumnCreationRequest("C002", outline, new Vector2(200, 150), type);

        ColumnCreateArgs args = new ColumnCreateArgsMapper(3000).Map(request);

        Assert.Equal(ColumnSectionKind.Rectangular, args.SectionKind);
        Assert.Null(args.DiameterMm);
    }
}
