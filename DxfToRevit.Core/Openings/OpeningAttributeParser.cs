using System.Globalization;

namespace DxfToRevit.Core.Openings;

/// <summary>
/// Reads the <see cref="OpeningAttributeTags"/> of a marker. Heights are written in centimetres
/// and converted to millimetres; missing or invalid values fall back to the defaults of the
/// opening kind and are reported as warnings.
/// </summary>
public sealed class OpeningAttributeParser
{
    /// <summary>Millimetres per centimetre.</summary>
    public const double MillimetresPerCentimetre = 10.0;

    /// <summary>Interprets the attributes of a marker.</summary>
    /// <param name="marker">Marker to read.</param>
    /// <param name="issues">Receives warnings for missing or invalid values.</param>
    /// <returns>The attribute values in millimetres.</returns>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public OpeningAttributes Parse(OpeningMarker marker, ICollection<OpeningIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(marker);
        ArgumentNullException.ThrowIfNull(issues);

        OpeningDefinition def = marker.Definition;
        string typeCode = marker.Attributes.TryGetValue(OpeningAttributeTags.Type, out string? code)
            ? code.Trim()
            : string.Empty;

        double heightCm = ReadCentimetres(marker, OpeningAttributeTags.Height, def.DefaultHeightCm,
            allowZero: false, issues);

        double sillCm = def.UsesSillHeight
            ? ReadCentimetres(marker, OpeningAttributeTags.SillHeight, def.DefaultSillHeightCm,
                allowZero: true, issues)
            : def.DefaultSillHeightCm;

        return new OpeningAttributes(
            typeCode,
            heightCm * MillimetresPerCentimetre,
            sillCm * MillimetresPerCentimetre);
    }

    /// <summary>
    /// Parses a centimetre value (dot or comma as decimal separator). Uses the default and adds a
    /// warning when the attribute is missing, not a number or out of range.
    /// </summary>
    private static double ReadCentimetres(
        OpeningMarker marker, string tag, double defaultCm, bool allowZero, ICollection<OpeningIssue> issues)
    {
        if (!marker.Attributes.TryGetValue(tag, out string? raw) || string.IsNullOrWhiteSpace(raw))
        {
            issues.Add(OpeningIssue.Warning(OpeningIssueCode.MissingAttribute, marker.Handle, marker.Center,
                $"{marker.Definition.Name}: attribute {tag} is missing; {defaultCm:0.##} cm used."));
            return defaultCm;
        }

        string normalised = raw.Trim().Replace(',', '.');
        bool ok = double.TryParse(normalised, NumberStyles.Float, CultureInfo.InvariantCulture, out double value);
        bool inRange = ok && double.IsFinite(value) && (allowZero ? value >= 0 : value > 0);
        if (inRange)
            return value;

        issues.Add(OpeningIssue.Warning(OpeningIssueCode.InvalidAttribute, marker.Handle, marker.Center,
            $"{marker.Definition.Name}: attribute {tag} = '{raw}' is not a valid value in cm; {defaultCm:0.##} cm used."));
        return defaultCm;
    }
}
