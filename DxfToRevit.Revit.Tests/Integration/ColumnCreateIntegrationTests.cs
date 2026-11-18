using DxfToRevit.Core.Building;
using DxfToRevit.Revit;
using netDxf;

namespace DxfToRevit.Revit.Tests.Integration;

/// <summary>
/// Real column creation — skipped until a Revit document is bound.
/// </summary>
public sealed class ColumnCreateIntegrationTests
{
    /// <summary>
    /// 300×300 mm rectangular column section.
    /// </summary>
    [Fact(Skip = "Revit Document integration requires a Revit session host. Wire RevitColumnCreator to Document, then enable this test.")]
    public void CreateColumn_RectangularSection_SucceedsInDocument()
    {
        var creator = new RevitColumnCreator();
        var request = new ColumnCreationRequest(
            "C-INT-001",
            new[]
            {
                new Vector2(0, 0),
                new Vector2(300, 0),
                new Vector2(300, 300),
                new Vector2(0, 300)
            },
            new Vector2(150, 150),
            type: new ColumnTypeProperties(widthMm: 300, depthMm: 300),
            placement: new ElementPlacement(levelName: "Level 1", heightMm: 3000));

        ColumnCreationResult result = creator.CreateColumn(request);

        Assert.True(result.Success, result.Message);
        Assert.False(string.IsNullOrWhiteSpace(result.HostElementId));
    }
}
