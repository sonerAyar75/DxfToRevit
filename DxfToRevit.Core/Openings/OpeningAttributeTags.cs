namespace DxfToRevit.Core.Openings;

/// <summary>Attribute tags read from opening marker blocks.</summary>
public static class OpeningAttributeTags
{
    /// <summary>Type code, mapped to a Revit type by the opening type map.</summary>
    public const string Type = "TYPE";

    /// <summary>Opening height in centimetres.</summary>
    public const string Height = "HEIGHT";

    /// <summary>Sill height in centimetres (windows and empty openings).</summary>
    public const string SillHeight = "SILL_HEIGHT";
}
