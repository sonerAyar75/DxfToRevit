using DxfToRevit.Core.Building;
using DxfToRevit.Revit;
using netDxf;

namespace DxfToRevit.Revit.Tests.Smoke;

/// <summary>
/// Smoke tests for the Revit adapter and document injection (no API or document needed).
/// </summary>
public sealed class RevitAdapterSmokeTests
{
    private static WallCreationRequest SampleWall() =>
        new("W-SMOKE", new Vector2(0, 0), new Vector2(1000, 0), 200);

    /// <summary>Parametresiz ctor → Null context → Fail.</summary>
    [Fact]
    public void RevitWallCreator_DefaultCtor_UsesNullContext_ReturnsFail()
    {
        var creator = new RevitWallCreator();
        Assert.False(creator.DocumentContext.IsAvailable);

        WallCreationResult result = creator.CreateWall(SampleWall());
        Assert.False(result.Success);
        Assert.Contains("not available", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Explicitly injected NullRevitDocumentContext → fail.</summary>
    [Fact]
    public void RevitWallCreator_NullContext_ReturnsFail()
    {
        var creator = new RevitWallCreator(NullRevitDocumentContext.Instance);
        WallCreationResult result = creator.CreateWall(SampleWall());
        Assert.False(result.Success);
    }

#if !REVIT_API_AVAILABLE
    /// <summary>
    /// RevitApiDocumentContext without the API → IsAvailable is false → fail.
    /// Only compiled for the API-free build, where the stub constructor exists.
    /// </summary>
    [Fact]
    public void RevitApiDocumentContext_WithoutApi_IsNotAvailable()
    {
        var ctx = new RevitApiDocumentContext();
        Assert.False(ctx.IsAvailable);

        var creator = new RevitWallCreator(ctx);
        WallCreationResult result = creator.CreateWall(SampleWall());
        Assert.False(result.Success);
    }
#endif

    /// <summary>CreateColumn fails while no document is bound.</summary>
    [Fact]
    public void RevitColumnCreator_WithoutDocument_ReturnsFail()
    {
        var creator = new RevitColumnCreator();
        var request = new ColumnCreationRequest(
            "C-SMOKE",
            new[]
            {
                new Vector2(0, 0),
                new Vector2(300, 0),
                new Vector2(300, 300),
                new Vector2(0, 300)
            },
            new Vector2(150, 150));

        ColumnCreationResult result = creator.CreateColumn(request);
        Assert.False(result.Success);
    }

    /// <summary>304.8 mm = 1 ft.</summary>
    [Fact]
    public void LengthUnitConverter_MmToFeet_OneFoot()
    {
        Assert.Equal(1.0, LengthUnitConverter.MmToFeet(304.8), 10);
    }
}
