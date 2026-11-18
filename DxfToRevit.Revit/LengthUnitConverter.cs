namespace DxfToRevit.Revit;

/// <summary>
/// Converts between DXF millimetres and Revit internal feet.
/// </summary>
/// <remarks>
/// The Revit API works in feet: <c>feet = mm / 304.8</c>. Building requests are kept in
/// millimetres and converted with this class at the adapter boundary.
/// </remarks>
public static class LengthUnitConverter
{
    /// <summary>Millimetres per foot (1 ft = 304.8 mm).</summary>
    public const double MillimetersPerFoot = 304.8;

    /// <summary>Converts millimetres to feet.</summary>
    /// <param name="mm">Length in millimetres.</param>
    public static double MmToFeet(double mm) => mm / MillimetersPerFoot;

    /// <summary>Converts feet to millimetres.</summary>
    /// <param name="feet">Length in feet.</param>
    public static double FeetToMm(double feet) => feet * MillimetersPerFoot;
}
