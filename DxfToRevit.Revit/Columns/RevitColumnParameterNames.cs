namespace DxfToRevit.Revit.Columns;

/// <summary>
/// Candidate names of the size parameters in column families; the first parameter found is used.
/// </summary>
/// <remarks>
/// Parameter names depend on the family and on the language of the template it was made from, so
/// each list holds the English name, the usual structural abbreviation and the Turkish name.
/// The non-English entries are data used to match localized families, not documentation.
/// When no name matches, error messages list the writable length parameters actually present.
/// </remarks>
public sealed class RevitColumnParameterNames
{
    /// <summary>Default name lists.</summary>
    public static RevitColumnParameterNames Default { get; } = new();

    /// <summary>Names of the width (local X) parameter.</summary>
    public IReadOnlyList<string> Width { get; init; } = new[] { "Width", "b", "Genişlik" };

    /// <summary>Names of the depth (local Y) parameter.</summary>
    public IReadOnlyList<string> Depth { get; init; } = new[] { "Depth", "h", "Derinlik" };

    /// <summary>Names of the diameter parameter of round columns.</summary>
    public IReadOnlyList<string> Diameter { get; init; } = new[] { "Diameter", "d", "Çap" };

    /// <summary>
    /// Words that identify a round column family by its name (English and Turkish), used to prefer
    /// such a family when several families have a diameter parameter.
    /// </summary>
    public IReadOnlyList<string> RoundFamilyNameHints { get; init; } = new[] { "round", "circular", "yuvarlak", "dairesel" };
}
