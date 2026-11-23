using DxfToRevit.Core.Openings;

namespace DxfToRevit.Revit;

/// <summary>
/// Decides whether a placed door must be flipped (Revit "flip facing" / "flip hand") so that its
/// hinge and swing match the DXF marker. It compares the instance's actual orientation vectors with
/// the desired ones instead of assuming how a family is built, so it works for any door family.
/// No Revit API is needed.
/// </summary>
/// <remarks>
/// Conventions of Revit door families (see <see cref="FacingPointsToSwingSide"/> and
/// <see cref="HandPointsToHinge"/>): verified in Revit 2025 with the standard families, the facing
/// orientation points to the side the leaf swings to, and the hand orientation points towards the
/// hinge. Both are configurable in case a family library uses the opposite convention. Decide the
/// facing first, flip it, then decide the hand with the orientation read after the facing flip.
/// </remarks>
public sealed class OpeningOrientationPlanner
{
    /// <summary>Whether the instance facing orientation points to the side the leaf swings to (default <c>true</c>).</summary>
    public bool FacingPointsToSwingSide { get; init; } = true;

    /// <summary>Whether the instance hand orientation points from the opening centre towards the hinge (default <c>true</c>).</summary>
    public bool HandPointsToHinge { get; init; } = true;

    /// <summary>Whether the facing must be flipped.</summary>
    /// <param name="wallDirection">Unit wall direction (start → end).</param>
    /// <param name="handing">Desired swing.</param>
    /// <param name="currentFacing">Current facing orientation of the instance (plan components).</param>
    public bool NeedsFacingFlip((double X, double Y) wallDirection, OpeningHanding handing, (double X, double Y) currentFacing)
    {
        ArgumentNullException.ThrowIfNull(handing);
        (double X, double Y) swing = handing.SwingSide == SwingSide.Left
            ? (-wallDirection.Y, wallDirection.X)
            : (wallDirection.Y, -wallDirection.X);
        (double X, double Y) desired = FacingPointsToSwingSide ? swing : (-swing.X, -swing.Y);
        return Dot(currentFacing, desired) < 0;
    }

    /// <summary>Whether the hand must be flipped.</summary>
    /// <param name="wallDirection">Unit wall direction (start → end).</param>
    /// <param name="handing">Desired swing.</param>
    /// <param name="currentHand">Current hand orientation of the instance (plan components).</param>
    public bool NeedsHandFlip((double X, double Y) wallDirection, OpeningHanding handing, (double X, double Y) currentHand)
    {
        ArgumentNullException.ThrowIfNull(handing);
        (double X, double Y) towardsHinge = handing.HingeEnd == HingeEnd.End
            ? wallDirection
            : (-wallDirection.X, -wallDirection.Y);
        (double X, double Y) desired = HandPointsToHinge ? towardsHinge : (-towardsHinge.X, -towardsHinge.Y);
        return Dot(currentHand, desired) < 0;
    }

    private static double Dot((double X, double Y) a, (double X, double Y) b) => a.X * b.X + a.Y * b.Y;
}
