using DxfToRevit.Core.Building;
using DxfToRevit.Core.Building.InMemory;
using DxfToRevit.Core.Geometry;
using netDxf;
using netDxf.Entities;
using Xunit;

namespace DxfToRevit.Tests.Building;

/// <summary>
/// BuildingExportFromDxf: wall solids + COLUMN polyline → in-memory host.
/// </summary>
public sealed class BuildingExportFromDxfTests
{
    /// <summary>
    /// 1 wall solid + 1 closed COLUMN polyline → 1 wall + 1 column success.
    /// </summary>
    [Fact]
    public void Export_WallAndColumn_CreatesBoth()
    {
        var model = new InMemoryBuildingModel();
        var service = new BuildingModelService(
            new InMemoryWallCreator(model),
            new InMemoryColumnCreator(model));
        var export = new BuildingExportFromDxf(service);

        var cl = new LineSegment2D(new Vector2(0, 0), new Vector2(1000, 0));
        var solid = new WallFootprintBuilder().Build(cl, 200);

        var doc = new DxfDocument();
        doc.Layers.Add(new netDxf.Tables.Layer("COLUMN"));
        var poly = new Polyline2D(new[]
        {
            new Vector2(5000, 5000),
            new Vector2(5300, 5000),
            new Vector2(5300, 5600),
            new Vector2(5000, 5600)
        }, isClosed: true)
        {
            Layer = doc.Layers["COLUMN"]
        };
        doc.Entities.Add(poly);

        BuildingExportFromDxf.ExportResult result = export.Export(new[] { solid }, doc);

        Assert.Equal(1, result.SuccessfulWalls);
        Assert.Equal(1, result.SuccessfulColumns);
        Assert.Single(model.Walls);
        Assert.Single(model.Columns);
        Assert.Equal(300, model.Columns[0].Request.Type.WidthMm);
        Assert.Equal(600, model.Columns[0].Request.Type.DepthMm);
    }
}
