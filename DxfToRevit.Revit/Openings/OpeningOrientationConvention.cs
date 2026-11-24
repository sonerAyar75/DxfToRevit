namespace DxfToRevit.Revit.Openings;

/// <summary>
/// How the orientation vectors of a Revit door or window family relate to its swing.
/// </summary>
/// <param name="FacingPointsToSwingSide">
/// Whether the instance facing orientation points to the side the leaf swings to.
/// </param>
/// <param name="HandPointsToHinge">
/// Whether the instance hand orientation points from the opening centre towards the hinge.
/// </param>
public sealed record OpeningOrientationConvention(bool FacingPointsToSwingSide, bool HandPointsToHinge)
{
    /// <summary>
    /// Standard Revit door families (verified in Revit 2025): the facing points to the swing side
    /// and the hand points towards the hinge.
    /// </summary>
    public static OpeningOrientationConvention StandardDoor { get; } = new(true, true);

    /// <summary>
    /// Standard Revit window families: the facing points to the side the sash swings to and the
    /// hand points towards the hinge, as for doors. Revit 2025 showed that the first assumption
    /// (facing away from the swing side) reversed the swing of every window.
    /// </summary>
    public static OpeningOrientationConvention StandardWindow { get; } = new(true, true);
}
