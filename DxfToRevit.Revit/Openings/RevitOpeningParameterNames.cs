namespace DxfToRevit.Revit.Openings;

/// <summary>
/// Candidate names of the size parameters in door and window families; the first parameter found
/// is used, then the built-in width/height parameters.
/// </summary>
/// <remarks>
/// Each list holds the English name and the Turkish name used by localised families; the
/// non-English entries are data for matching, not documentation.
/// </remarks>
public sealed class RevitOpeningParameterNames
{
    /// <summary>Default name lists.</summary>
    public static RevitOpeningParameterNames Default { get; } = new();

    /// <summary>Names of the width parameter.</summary>
    public IReadOnlyList<string> Width { get; init; } = new[] { "Width", "Genişlik" };

    /// <summary>Names of the height parameter.</summary>
    public IReadOnlyList<string> Height { get; init; } = new[] { "Height", "Yükseklik" };
}
