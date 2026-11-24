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
