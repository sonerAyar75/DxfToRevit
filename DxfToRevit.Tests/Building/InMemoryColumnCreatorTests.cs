using DxfToRevit.Core.Building;
using DxfToRevit.Core.Building.InMemory;
using netDxf;
using Xunit;

namespace DxfToRevit.Tests.Building;

/// <summary>
/// InMemoryColumnCreator: closed polyline column flow.
/// </summary>
public sealed class InMemoryColumnCreatorTests
{
    private static ColumnCreationRequest Rect(string id, double w, double d) =>
        new(
            id,
            new[]
            {
                new Vector2(0, 0),
                new Vector2(w, 0),
                new Vector2(w, d),
                new Vector2(0, d)
            },
            new Vector2(w / 2, d / 2),
            type: new ColumnTypeProperties(widthMm: w, depthMm: d));

    /// <summary>Two columns → C-0001, C-0002; model.Columns.Count = 2.</summary>
    [Fact]
    public void CreateColumns_TwoValid_AddsRecords()
    {
        var model = new InMemoryBuildingModel();
        var creator = new InMemoryColumnCreator(model);

        var results = creator.CreateColumns(new[]
        {
            Rect("C001", 300, 300),
            Rect("C002", 400, 600)
        });

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
        Assert.Equal(2, model.Columns.Count);
        Assert.Equal("C-0001", model.Columns[0].HostElementId);
        Assert.Equal(400, model.Columns[1].Request.Type.WidthMm);
    }

    /// <summary>Very small section (10×10) below the 50 mm minimum → fail.</summary>
    [Fact]
    public void CreateColumn_TooSmallSection_Fails()
    {
        var model = new InMemoryBuildingModel();
        var creator = new InMemoryColumnCreator(model);

        var result = creator.CreateColumn(Rect("C001", 10, 10));

        Assert.False(result.Success);
        Assert.Empty(model.Columns);
    }
}
