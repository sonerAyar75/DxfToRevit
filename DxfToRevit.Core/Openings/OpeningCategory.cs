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
    /// Leaves (doors, windows): the one circle of the block gives the swing side and the hinge end.
    /// The kind of door or window (single, double, sliding, multi-panel) comes from the Revit family
    /// mapped to the <c>TYPE</c> code, not from the block.
    /// </summary>
    Hinged
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
