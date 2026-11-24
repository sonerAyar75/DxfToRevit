namespace DxfToRevit.Core.Openings;

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
