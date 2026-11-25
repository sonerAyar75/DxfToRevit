using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Import;
using DxfToRevit.Core.InMemory;
using netDxf.Entities;
using netDxf.Tables;
using netDxf;

namespace DxfToRevit.Tests.Columns;

/// <summary>
/// CIRCLE entity → circular column.
/// </summary>
public sealed class DxfColumnReaderCircleTests
{
    [Fact]
    public void ReadColumns_CircleOnColumnLayer_HasRadiusAndCentroid()
    {
        var doc = new DxfDocument();
        doc.Layers.Add(new Layer("COLUMN"));
        var circle = new Circle(new Vector3(1000, 2000, 0), 150)
        {
            Layer = doc.Layers["COLUMN"]
        };
        doc.Entities.Add(circle);

        var models = new DxfColumnReader().ReadColumns(doc);

        Assert.Single(models);
        Assert.Equal(150, models[0].RadiusMm);
        Assert.Equal(1000, models[0].Centroid.X, 1);
        Assert.Equal(2000, models[0].Centroid.Y, 1);
        Assert.True(models[0].Outline.Count >= 8);
    }

    [Fact]
    public void Factory_Circle_IsCircularSection_CreatesInMemory()
    {
        var model = ColumnModel.FromCircle("C1", "COLUMN", "H1", new Vector2(0, 0), 200);
        var factory = new ColumnCreationRequestFactory(ImportSessionOptions.ForLevel("Level 1"));
        var requests = factory.FromColumnModels(new[] { model });

        Assert.Equal(ColumnSectionKind.Circular, requests[0].Type.SectionKind);
        Assert.Equal("Circular Column", requests[0].Type.TypeKey);
        Assert.Equal(200, requests[0].Type.RadiusMm);
        Assert.Equal(400, requests[0].Type.DiameterMm);
        Assert.Equal(400, requests[0].Type.WidthMm);

        var mem = new InMemoryBuildingModel();
        var result = new InMemoryColumnCreator(mem).CreateColumn(requests[0]);
        Assert.True(result.Success, result.Message);
    }

    [Fact]
    public void ReadColumns_CircleOnWallLayer_Ignored()
    {
        var doc = new DxfDocument();
        doc.Layers.Add(new Layer("WALL-20"));
        doc.Entities.Add(new Circle(new Vector3(0, 0, 0), 100)
        {
            Layer = doc.Layers["WALL-20"]
        });

        Assert.Empty(new DxfColumnReader().ReadColumns(doc));
    }
}
