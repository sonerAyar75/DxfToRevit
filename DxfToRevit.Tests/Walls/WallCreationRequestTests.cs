using DxfToRevit.Core.Walls;
using netDxf;

namespace DxfToRevit.Tests.Walls;

/// <summary>
/// WallCreationRequest model validation.
/// </summary>
public sealed class WallCreationRequestTests
{
    /// <summary>Valid request: Basic Wall, length 1000 mm.</summary>
    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var req = new WallCreationRequest(
            "W001",
            new Vector2(0, 0),
            new Vector2(1000, 0),
            thicknessMm: 200);

        Assert.Equal("W001", req.Id);
        Assert.Equal(200, req.ThicknessMm);
        Assert.Equal(1000, req.LengthMm, 6);
        Assert.Equal("Basic Wall", req.WallTypeKey);
    }

    /// <summary>Thickness ≤ 0 → ArgumentOutOfRangeException.</summary>
    [Fact]
    public void Constructor_NonPositiveThickness_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WallCreationRequest("W1", new Vector2(0, 0), new Vector2(10, 0), 0));
    }
}
