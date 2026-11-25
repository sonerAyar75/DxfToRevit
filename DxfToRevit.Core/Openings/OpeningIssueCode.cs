namespace DxfToRevit.Core.Openings;

/// <summary>Kind of problem found while reading or interpreting an opening.</summary>
public enum OpeningIssueCode
{
    /// <summary>The marker block has no closed 4-vertex rectangle, or an unsupported orientation.</summary>
    InvalidMarker,

    /// <summary>The marker does not lie on any wall.</summary>
    NoHostWall,

    /// <summary>The marker lies on more than one wall.</summary>
    MultipleWalls,

    /// <summary>The marker extends beyond the end of its wall.</summary>
    OutsideWall,

    /// <summary>Two markers on the same wall overlap.</summary>
    Overlapping,

    /// <summary>An expected attribute is missing; a default was used.</summary>
    MissingAttribute,

    /// <summary>An attribute is not a valid number; a default was used.</summary>
    InvalidAttribute,

    /// <summary>The marker depth differs from the wall thickness.</summary>
    DepthMismatch,

    /// <summary>
    /// The swing of a door or window marker is unclear: no circle or more than one (left as
    /// Revit places it), a circle on the wall axis, or a circle in the middle of the opening.
    /// </summary>
    MissingHinge,

    /// <summary>A gap in a wall has no marker; it was left open.</summary>
    UnmarkedGap
}
