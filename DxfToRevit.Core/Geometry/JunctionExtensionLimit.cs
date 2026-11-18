using netDxf;

namespace DxfToRevit.Core.Geometry;

/// <summary>
/// Computes how far a centerline may be extended to reach a junction.
/// </summary>
/// <remarks>
/// Formula: <c>limit = (T_other / 2) / tan(α / 2) + margin</c>, where <c>T_other</c> is the
/// thickness of the crossing wall and <c>α</c> the acute angle between the walls. At 90° the limit
/// is about <c>T_other / 2</c>; it grows as the angle gets sharper (clamped at
/// <see cref="MinAngleDegrees"/> so near-parallel walls do not produce huge extensions).
/// </remarks>
public static class JunctionExtensionLimit
{
    /// <summary>
    /// Minimum acute angle (degrees) used when clamping near-parallel lines.
    /// Prevents the limit from exploding.
    /// </summary>
    public const double MinAngleDegrees = 5.0;

    /// <summary>
    /// Extra margin (mm) added on top of the geometric limit.
    /// </summary>
    public const double MarginMm = 5.0;

    /// <summary>
    /// Acute angle between two direction vectors, in radians, in the range (0, π/2].
    /// Zero-length vectors are treated as perpendicular; an angle of zero is replaced by
    /// <see cref="MinAngleDegrees"/>.
    /// </summary>
    /// <param name="directionA">First direction.</param>
    /// <param name="directionB">Second direction.</param>
    public static double AcuteAngleRadians(Vector2 directionA, Vector2 directionB)
    {
        double lenA = directionA.Modulus();
        double lenB = directionB.Modulus();
        if (lenA < 1e-12 || lenB < 1e-12)
            return Math.PI / 2.0;

        double cos = Vector2.DotProduct(directionA, directionB) / (lenA * lenB);
        cos = Math.Clamp(cos, -1.0, 1.0);
        double angle = Math.Acos(Math.Abs(cos)); // [0, π/2]
        if (angle < 1e-12)
            angle = MinAngleDegrees * Math.PI / 180.0;
        return angle;
    }

    /// <summary>
    /// Maximum extension allowed for a centerline approaching a crossing wall
    /// of the given thickness at the given acute intersection angle.
    /// </summary>
    /// <param name="crossingWallThicknessMm">Thickness of the crossing wall (mm); must be positive.</param>
    /// <param name="acuteAngleRadians">Acute angle between the two walls.</param>
    /// <returns>The allowed extension in millimetres.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The thickness is not positive.</exception>
    public static double ForCrossingWall(
        double crossingWallThicknessMm,
        double acuteAngleRadians)
    {
        if (crossingWallThicknessMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(crossingWallThicknessMm));

        double minAngle = MinAngleDegrees * Math.PI / 180.0;
        double alpha = Math.Max(acuteAngleRadians, minAngle);
        // α in (0, π/2] → α/2 in (0, π/4], tan positive and ≤ 1
        double half = alpha / 2.0;
        double tanHalf = Math.Tan(half);
        if (tanHalf < 1e-6)
            tanHalf = 1e-6;

        return (crossingWallThicknessMm / 2.0) / tanHalf + MarginMm;
    }

    /// <summary>
    /// Convenience: limits for both legs of a junction.
    /// <paramref name="allowA"/> is how far line A may extend (uses the thickness of B);
    /// <paramref name="allowB"/> is how far line B may extend (uses the thickness of A).
    /// </summary>
    /// <param name="directionA">Direction of line A.</param>
    /// <param name="thicknessA">Thickness of wall A (mm).</param>
    /// <param name="directionB">Direction of line B.</param>
    /// <param name="thicknessB">Thickness of wall B (mm).</param>
    /// <param name="allowA">Allowed extension of line A (mm).</param>
    /// <param name="allowB">Allowed extension of line B (mm).</param>
    public static void ForPair(
        Vector2 directionA,
        double thicknessA,
        Vector2 directionB,
        double thicknessB,
        out double allowA,
        out double allowB)
    {
        double alpha = AcuteAngleRadians(directionA, directionB);
        allowA = ForCrossingWall(thicknessB, alpha);
        allowB = ForCrossingWall(thicknessA, alpha);
    }
}
