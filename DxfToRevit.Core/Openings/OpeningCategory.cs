namespace DxfToRevit.Core.Openings;

/// <summary>
/// Host category an opening is created in. Decides the Revit category (doors, windows) or that no
/// family is used at all (empty opening).
/// </summary>
public enum OpeningCategory
{
    /// <summary>A door family instance hosted by the wall.</summary>
    Door,

    /// <summary>A window family instance hosted by the wall.</summary>
    Window,

    /// <summary>A plain hole in the wall without a family (for example a passage without a door).</summary>
    EmptyOpening
}

/// <summary>How the host creates an opening.</summary>
public enum OpeningCreationMode
{
    /// <summary>A loadable family instance (door or window) hosted by the wall.</summary>
    HostedFamily,

    /// <summary>A rectangular cut in the wall without a family.</summary>
    WallOpening
}

/// <summary>How the swing of an opening is described by the marker block.</summary>
public enum OpeningHandingMode
{
    /// <summary>No swing (empty openings).</summary>
    None,

    /// <summary>
    /// Leaves (doors, windows): the circles of the block give the swing side and, for a single
    /// hinged leaf, the hinge end (see <see cref="OpeningLeafLayout"/>).
    /// </summary>
    Hinged
}

/// <summary>
/// Leaf arrangement read from the circles of a marker block. It decides which orientation can be
/// applied; the kind of door or window itself (double, sliding, multi-panel) comes from the Revit
/// family mapped to the <c>TYPE</c> code.
/// </summary>
public enum OpeningLeafLayout
{
    /// <summary>
    /// One circle near one end of the opening: a single hinged leaf. Both the swing side and the
    /// hinge end are applied.
    /// </summary>
    SingleHinged,

    /// <summary>
    /// Circles near both ends: two hinged leaves (double door, double casement). Only the swing
    /// side is applied; left/right is meaningless.
    /// </summary>
    DoubleHinged,

    /// <summary>
    /// Circles only near the middle: no hinge end (sliding, fixed or multi-panel). Only the side
    /// (inside/outside) is applied.
    /// </summary>
    SideOnly
}

/// <summary>End of the opening, along the wall direction, on which the hinge lies.</summary>
public enum HingeEnd
{
    /// <summary>The end nearer to the wall centerline start.</summary>
    Start,

    /// <summary>The end nearer to the wall centerline end.</summary>
    End
}

/// <summary>
/// Wall face, relative to the wall direction (start → end), to which a door or window leaf swings
/// (the face on which the marker's circle lies).
/// </summary>
public enum SwingSide
{
    /// <summary>The face to the left of the wall direction.</summary>
    Left,

    /// <summary>The face to the right of the wall direction.</summary>
    Right
}
