using DxfToRevit.Revit;

namespace DxfToRevit.Revit.Tests.Mapping;

/// <summary>
/// mm ↔ feet conversion (Revit internal unit).
/// 304.8 mm = 1 ft.
/// </summary>
public sealed class LengthUnitConverterTests
{
    /// <summary>304.8 mm → 1 foot.</summary>
    [Fact]
    public void MmToFeet_304_8_IsOneFoot()
    {
        Assert.Equal(1.0, LengthUnitConverter.MmToFeet(304.8), 10);
    }

    /// <summary>200 mm → 200/304.8 feet.</summary>
    [Fact]
    public void MmToFeet_200mm_IsExpected()
    {
        Assert.Equal(200.0 / 304.8, LengthUnitConverter.MmToFeet(200), 10);
    }

    /// <summary>Round-trip mm → feet → mm.</summary>
    [Fact]
    public void RoundTrip_PreservesValue()
    {
        double mm = 1234.5;
        Assert.Equal(mm, LengthUnitConverter.FeetToMm(LengthUnitConverter.MmToFeet(mm)), 9);
    }
}
