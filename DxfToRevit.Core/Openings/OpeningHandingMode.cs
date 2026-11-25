namespace DxfToRevit.Core.Openings;

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
