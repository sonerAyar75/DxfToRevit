using DxfToRevit.Core.Geometry;
using Xunit;

namespace DxfToRevit.Tests.Geometry;

/// <summary>
/// GeometryToleranceTests birim testleri.
/// </summary>
public class GeometryToleranceTests
{
    /// <summary>The point tolerance must be exactly 1.0 mm (DXF unit is mm).</summary>
    [Fact]
    public void PointDistanceTolerance_ShouldBeOneMillimeter()
    {
        Assert.Equal(1.0, GeometryTolerance.PointDistanceMm);
    }

    /// <summary>The tolerance must be positive.</summary>
    [Fact]
    public void PointDistanceTolerance_ShouldBePositive()
    {
        Assert.True(GeometryTolerance.PointDistanceMm > 0);
    }
}
