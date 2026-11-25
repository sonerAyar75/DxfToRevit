using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Import;
using DxfToRevit.Core.InMemory;
using netDxf.Entities;
using netDxf.Tables;
using netDxf;

namespace DxfToRevit.Tests.Columns;

/// <summary>
/// Reading freeform / geometrically closed column polylines and creating them in memory.
/// </summary>
public sealed class DxfColumnReaderFreeformTests
{
    [Fact]
    public void ReadColumns_LShapeClosed_IsRead()
    {
        var doc = new DxfDocument();
        doc.Layers.Add(new Layer("COLUMN"));
        var poly = new Polyline2D(new[]
        {
            new Vector2(0, 0),
            new Vector2(400, 0),
            new Vector2(400, 100),
            new Vector2(100, 100),
            new Vector2(100, 400),
            new Vector2(0, 400)
        }, isClosed: true)
        {
            Layer = doc.Layers["COLUMN"]
        };
        doc.Entities.Add(poly);

        var models = new DxfColumnReader().ReadColumns(doc);
        Assert.Single(models);
        Assert.Equal(6, models[0].Outline.Count);
    }

    [Fact]
    public void ReadColumns_GeometricallyClosedButFlagFalse_IsRead()
    {
        var doc = new DxfDocument();
        doc.Layers.Add(new Layer("COLUMN"));
        // IsClosed=false ama son≈ilk
        var poly = new Polyline2D(new[]
        {
            new Vector2(0, 0),
            new Vector2(300, 0),
            new Vector2(250, 200),
            new Vector2(0, 0)
        }, isClosed: false)
        {
            Layer = doc.Layers["COLUMN"]
        };
        doc.Entities.Add(poly);

        var models = new DxfColumnReader().ReadColumns(doc);
        Assert.Single(models);
        Assert.True(models[0].Outline.Count >= 3);
    }

    [Fact]
    public void Factory_LShape_IsFreeform_AndCreatesInMemory()
    {
        var outline = new[]
        {
            new Vector2(0, 0),
            new Vector2(400, 0),
            new Vector2(400, 100),
            new Vector2(100, 100),
            new Vector2(100, 400),
            new Vector2(0, 400)
        };
        var model = new ColumnModel("C001", "COLUMN", "H1", outline);
        var factory = new ColumnCreationRequestFactory(ImportSessionOptions.ForLevel("Level 1"));
        var requests = factory.FromColumnModels(new[] { model });

        Assert.Single(requests);
        Assert.Equal(ColumnSectionKind.Freeform, requests[0].Type.SectionKind);
        Assert.Equal("Freeform Column", requests[0].Type.TypeKey);

        var mem = new InMemoryBuildingModel();
        var creator = new InMemoryColumnCreator(mem);
        var result = creator.CreateColumn(requests[0]);

        Assert.True(result.Success, result.Message);
        Assert.Single(mem.Columns);
    }

    [Fact]
    public void IsApproximatelyRectangular_LShape_False()
    {
        var outline = new[]
        {
            new Vector2(0, 0),
            new Vector2(4, 0),
            new Vector2(4, 1),
            new Vector2(1, 1),
            new Vector2(1, 3),
            new Vector2(0, 3)
        };
        Assert.False(PolygonMetrics2D.IsApproximatelyRectangular(outline));
    }

    [Fact]
    public void IsApproximatelyRectangular_UnitSquare_True()
    {
        var outline = new[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        Assert.True(PolygonMetrics2D.IsApproximatelyRectangular(outline));
    }
}
