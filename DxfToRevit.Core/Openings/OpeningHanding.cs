namespace DxfToRevit.Core.Openings;

/// <summary>
/// Orientation of a door or window, relative to the host wall direction, as read from the one
/// circle of its marker. What the orientation means for a double, sliding or multi-panel leaf is
/// up to the Revit family mapped to the <c>TYPE</c> code.
/// </summary>
/// <param name="HingeEnd">
/// End of the opening on which the circle lies; <c>null</c> when the circle is in the middle of the
/// opening (left/right is then left as Revit places it).
/// </param>
/// <param name="SwingSide">Wall face to which the leaf swings (the face holding the circle).</param>
public sealed record OpeningHanding(HingeEnd? HingeEnd, SwingSide SwingSide);
