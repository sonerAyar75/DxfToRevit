using DxfToRevit.Core.Geometry;
using netDxf;
using Xunit;

namespace DxfToRevit.Tests.Geometry;

/// <summary>
/// Junction extension limit: L = (T/2) / tan(α/2) + 5 mm. 90° → T/2; L grows as the angle gets smaller.
/// </summary>
public sealed class JunctionExtensionLimitTests
{
    /// <summary>α=90°: tan(45°)=1 → L = 200/2 + 5 = 105 mm.</summary>
    [Fact]
    public void ForCrossingWall_At90Degrees_IsHalfThicknessPlusMargin()
    {
        double limit = JunctionExtensionLimit.ForCrossingWall(200, Math.PI / 2);
        Assert.Equal(105, limit, 6);
    }

    /// <summary>
    /// α=60°: L = 75/tan(30°) + 5 ≈ 134.9 mm.
    /// The chevron measurement (~129.9 mm) stays below this limit.
    /// </summary>
    [Fact]
    public void ForCrossingWall_At60Degrees_MatchesMeasuredChevronGap()
    {
        double limit = JunctionExtensionLimit.ForCrossingWall(150, Math.PI / 3);
        Assert.Equal(75 / Math.Tan(Math.PI / 6) + 5, limit, 6);
        Assert.True(limit > 129.9);
    }

    /// <summary>
    /// ForPair: allowA comes from the thickness of B, allowB from the thickness of A (90°).
    /// allowA=55, allowB=105.
    /// </summary>
    [Fact]
    public void ForPair_UsesOtherWallThickness()
    {
        JunctionExtensionLimit.ForPair(
            new Vector2(1, 0), 200,
            new Vector2(0, 1), 100,
            out double allowA, out double allowB);
        Assert.Equal(55, allowA, 6);
        Assert.Equal(105, allowB, 6);
    }

    /// <summary>Directions −60° and −120° have an acute angle of 60° (π/3).</summary>
    [Fact]
    public void AcuteAngle_Between60AndMinus120_Is60Degrees()
    {
        var d1 = new Vector2(Math.Cos(-Math.PI / 3), Math.Sin(-Math.PI / 3));
        var d2 = new Vector2(Math.Cos(-2 * Math.PI / 3), Math.Sin(-2 * Math.PI / 3));
        Assert.Equal(Math.PI / 3, JunctionExtensionLimit.AcuteAngleRadians(d1, d2), 6);
    }
}
