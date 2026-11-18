using DxfToRevit.Core.Building;
using DxfToRevit.Core.Models;
using netDxf;
using Xunit;

namespace DxfToRevit.Tests.Building;

/// <summary>
/// ColumnModel → ColumnCreationRequest (+ session placement).
/// </summary>
public sealed class ColumnCreationRequestFactoryTests
{
    [Fact]
    public void FromColumnModels_Rectangle_SetsWidthDepth()
    {
        var outline = new[]
        {
            new Vector2(0, 0),
            new Vector2(300, 0),
            new Vector2(300, 500),
            new Vector2(0, 500)
        };
        var model = new ColumnModel("C001", "COLUMN", "H1", outline);
        var session = ImportSessionOptions.ForLevel("Level 1", heightMm: 3000);
        var factory = new ColumnCreationRequestFactory(session);

        IReadOnlyList<ColumnCreationRequest> requests = factory.FromColumnModels(new[] { model });

        Assert.Single(requests);
        Assert.Equal(300, requests[0].Type.WidthMm);
        Assert.Equal(500, requests[0].Type.DepthMm);
        Assert.Equal("Level 1", requests[0].Placement.LevelName);
        Assert.Equal(3000, requests[0].Placement.HeightMm);
        Assert.Equal("COLUMN", requests[0].SourceLayer);
        Assert.True(requests[0].Type.IsStructural);
    }
    [Fact]
    public void FromColumnModels_CircleToSquareFallback_UsesDiameterForBothDimensions()
    {
        var model = ColumnModel.FromCircle(
            "C-CIRC-01", "COLUMN", "H-CIRC-01", new Vector2(1000, 2000), radiusMm: 250);
        var factory = new ColumnCreationRequestFactory
        {
            ConvertCircularToSquare = true,
            SquareColumnTypeKey = "Concrete Square::300x300"
        };

        IReadOnlyList<ColumnCreationRequest> requests = factory.FromColumnModels(new[] { model });

        Assert.Single(requests);
        Assert.Equal(ColumnSectionKind.Rectangular, requests[0].Type.SectionKind);
        Assert.Equal("Concrete Square::300x300", requests[0].Type.TypeKey);
        Assert.Equal(500, requests[0].Type.WidthMm);
        Assert.Equal(500, requests[0].Type.DepthMm);
        Assert.Null(requests[0].Type.RadiusMm);
    }

    [Fact]
    public void FromColumnModels_CircleWithoutFallback_RemainsCircular()
    {
        var model = ColumnModel.FromCircle(
            "C-CIRC-02", "COLUMN", "H-CIRC-02", new Vector2(0, 0), radiusMm: 200);
        var factory = new ColumnCreationRequestFactory
        {
            CircularColumnTypeKey = "Concrete Circular::D400"
        };

        IReadOnlyList<ColumnCreationRequest> requests = factory.FromColumnModels(new[] { model });

        Assert.Single(requests);
        Assert.Equal(ColumnSectionKind.Circular, requests[0].Type.SectionKind);
        Assert.Equal("Concrete Circular::D400", requests[0].Type.TypeKey);
        Assert.Equal(400, requests[0].Type.DiameterMm);
    }

}
