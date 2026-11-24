using DxfToRevit.Core.Openings;

namespace DxfToRevit.Revit.Openings;

/// <summary>
/// Decides whether a placed door or window must be flipped (Revit "flip facing" / "flip hand") so
/// that its swing side and hinge match the DXF marker. It compares the instance's actual
/// orientation vectors with the desired ones instead of assuming how a family is built, so it works
/// for any family. No Revit API is needed.
/// </summary>
/// <remarks>
/// Doors and windows have their own <see cref="OpeningOrientationConvention"/> (<see cref="Door"/>,
/// <see cref="Window"/>), configurable in case a family library uses other conventions. Decide the
/// facing first, flip it, then decide the hand with the orientation read after the facing flip.
/// The hand is only decided when the marker gives a hinge end (not for a circle in the middle).
/// </remarks>
public sealed class OpeningOrientationPlanner
{
    /// <summary>Convention of door families (default <see cref="OpeningOrientationConvention.StandardDoor"/>).</summary>
    public OpeningOrientationConvention Door { get; init; } = OpeningOrientationConvention.StandardDoor;

    /// <summary>Convention of window families (default <see cref="OpeningOrientationConvention.StandardWindow"/>).</summary>
    public OpeningOrientationConvention Window { get; init; } = OpeningOrientationConvention.StandardWindow;

    /// <summary>The convention used for a category (windows use <see cref="Window"/>, everything else <see cref="Door"/>).</summary>
    /// <param name="category">Opening category.</param>
    public OpeningOrientationConvention ConventionFor(OpeningCategory category) =>
        category == OpeningCategory.Window ? Window : Door;

    /// <summary>Whether the facing must be flipped.</summary>
    /// <param name="category">Door or window.</param>
    /// <param name="wallDirection">Unit wall direction (start → end).</param>
    /// <param name="handing">Desired orientation.</param>
    /// <param name="currentFacing">Current facing orientation of the instance (plan components).</param>
    public bool NeedsFacingFlip(
        OpeningCategory category,
        (double X, double Y) wallDirection,
        OpeningHanding handing,
        (double X, double Y) currentFacing)
    {
        ArgumentNullException.ThrowIfNull(handing);
        (double X, double Y) swing = handing.SwingSide == SwingSide.Left
            ? (-wallDirection.Y, wallDirection.X)
            : (wallDirection.Y, -wallDirection.X);
        (double X, double Y) desired = ConventionFor(category).FacingPointsToSwingSide ? swing : (-swing.X, -swing.Y);
        return Dot(currentFacing, desired) < 0;
    }

    /// <summary>Whether the hand must be flipped; always <c>false</c> when the marker has no hinge end.</summary>
    /// <param name="category">Door or window.</param>
    /// <param name="wallDirection">Unit wall direction (start → end).</param>
    /// <param name="handing">Desired orientation.</param>
    /// <param name="currentHand">Current hand orientation of the instance (plan components).</param>
    public bool NeedsHandFlip(
        OpeningCategory category,
        (double X, double Y) wallDirection,
        OpeningHanding handing,
        (double X, double Y) currentHand)
    {
        ArgumentNullException.ThrowIfNull(handing);
        if (handing.HingeEnd is not HingeEnd end)
            return false;

        (double X, double Y) towardsHinge = end == HingeEnd.End
            ? wallDirection
            : (-wallDirection.X, -wallDirection.Y);
        (double X, double Y) desired = ConventionFor(category).HandPointsToHinge
            ? towardsHinge
            : (-towardsHinge.X, -towardsHinge.Y);
        return Dot(currentHand, desired) < 0;
    }

    private static double Dot((double X, double Y) a, (double X, double Y) b) => a.X * b.X + a.Y * b.Y;
}
