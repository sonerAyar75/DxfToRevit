namespace DxfToRevit.Core.Geometry;

/// <summary>
/// Global geometric tolerances. DXF coordinates are assumed to be in millimetres.
/// </summary>
public static class GeometryTolerance
{
    /// <summary>
    /// Maximum distance (mm) for treating two points as coincident, or two parallel lines as collinear.
    /// </summary>
    public const double PointDistanceMm = 1.0;

    /// <summary>
    /// Maximum angular deviation (degrees) for treating two lines as parallel.
    /// </summary>
    public const double ParallelAngleDegrees = 0.1;
}
