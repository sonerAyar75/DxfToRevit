using DxfToRevit.Core.Geometry;

namespace DxfToRevit.Core.Building;

/// <summary>
/// Business rules a column request must satisfy before it is sent to a host.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Circular: radius at least <see cref="MinRadiusMm"/>.</description></item>
/// <item><description>Rectangular and freeform: section area at least <see cref="MinAreaMm2"/>.</description></item>
/// <item><description>Rectangular: each side at least <see cref="MinSectionMm"/> (true side lengths for rotated rectangles).</description></item>
/// </list>
/// </remarks>
public sealed class ColumnCreationValidator
{
    /// <summary>Minimum side length of a rectangular section, in millimetres.</summary>
    public double MinSectionMm { get; }

    /// <summary>Minimum section area for rectangular and freeform sections, in square millimetres.</summary>
    public double MinAreaMm2 { get; }

    /// <summary>Minimum radius of a circular section, in millimetres.</summary>
    public double MinRadiusMm { get; }

    /// <summary>Creates the validator.</summary>
    /// <param name="minSectionMm">Minimum rectangular side length (mm).</param>
    /// <param name="minAreaMm2">Minimum section area (mm²).</param>
    /// <param name="minRadiusMm">Minimum circular radius (mm).</param>
    /// <exception cref="ArgumentOutOfRangeException">A limit is negative.</exception>
    public ColumnCreationValidator(
        double minSectionMm = 50.0,
        double minAreaMm2 = 2500.0,
        double minRadiusMm = 25.0)
    {
        if (minSectionMm < 0) throw new ArgumentOutOfRangeException(nameof(minSectionMm));
        if (minAreaMm2 < 0) throw new ArgumentOutOfRangeException(nameof(minAreaMm2));
        if (minRadiusMm < 0) throw new ArgumentOutOfRangeException(nameof(minRadiusMm));
        MinSectionMm = minSectionMm;
        MinAreaMm2 = minAreaMm2;
        MinRadiusMm = minRadiusMm;
    }

    /// <summary>Checks a request against the rules.</summary>
    /// <param name="request">Request to check.</param>
    /// <returns><c>null</c> when valid; otherwise a human-readable error message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    public string? Validate(ColumnCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.OutlineMm.Count < 3)
            return "Column outline needs at least 3 vertices.";

        if (request.Placement.HeightMm is <= 0)
            return "Height must be greater than zero when specified.";

        if (request.Type.SectionKind == ColumnSectionKind.Circular)
        {
            double r = request.Type.RadiusMm
                       ?? (request.Type.WidthMm is double d ? d / 2.0 : 0);
            if (r < MinRadiusMm)
                return $"Circular column radius must be at least {MinRadiusMm} mm (got {r:F1}).";
            return null;
        }

        double area = PolygonMetrics2D.AbsoluteArea(request.OutlineMm);
        if (area < MinAreaMm2)
            return $"Column section area must be at least {MinAreaMm2} mm² (got {area:F1}).";

        if (request.Type.SectionKind == ColumnSectionKind.Rectangular)
        {
            // Prefer the true (oriented) size from the type: for a rotated column the
            // axis-aligned bounding box is larger than the real section.
            double w, d;
            bool hasSize;
            if (request.Type.WidthMm is double typeWidth && request.Type.DepthMm is double typeDepth)
            {
                w = typeWidth;
                d = typeDepth;
                hasSize = true;
            }
            else
            {
                hasSize = PolygonMetrics2D.TryGetWidthDepth(request.OutlineMm, out w, out d);
            }

            if (hasSize && (w < MinSectionMm || d < MinSectionMm))
                return $"Rectangular column section must be at least {MinSectionMm} mm in each direction (got {w:F1} x {d:F1}).";
        }

        return null;
    }
}
