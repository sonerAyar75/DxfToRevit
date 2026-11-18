using System.Globalization;
using System.Text.RegularExpressions;

namespace DxfToRevit.Core.Rules;

/// <summary>
/// Wall layer naming convention. A wall layer is <c>WALL</c> or <c>WALL-*</c>; when the suffix is a
/// number it encodes the thickness in centimetres: <c>WALL-20</c> → 200 mm.
/// </summary>
public sealed class WallLayerRule
{
    private static readonly Regex WallLayerRegex = new(
        @"^\s*WALL-(\d+)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Reads the wall thickness encoded in a layer name (<c>WALL-&lt;cm&gt;</c>, case-insensitive,
    /// surrounding whitespace allowed).
    /// </summary>
    /// <param name="layerName">Layer name to inspect.</param>
    /// <param name="thicknessMm">Thickness in millimetres (suffix × 10); 0 when not found.</param>
    /// <returns><c>true</c> when the name has a positive numeric thickness code.</returns>
    public bool TryGetThicknessMm(string? layerName, out double thicknessMm)
    {
        thicknessMm = 0;
        if (string.IsNullOrWhiteSpace(layerName))
            return false;

        Match match = WallLayerRegex.Match(layerName);
        if (!match.Success)
            return false;

        if (!int.TryParse(
                match.Groups[1].Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int code))
        {
            return false;
        }

        thicknessMm = code * 10.0;
        return thicknessMm > 0;
    }

    /// <summary>
    /// Returns <c>true</c> when the layer is a wall layer, with or without a thickness code.
    /// Only <c>WALL</c> and <c>WALL-*</c> qualify; names such as <c>WALLPAPER</c> or <c>WALLS</c> do not.
    /// </summary>
    /// <param name="layerName">Layer name to inspect (case-insensitive, trimmed).</param>
    public bool IsWallLayer(string? layerName)
    {
        if (string.IsNullOrWhiteSpace(layerName))
            return false;

        string name = layerName.Trim();
        return name.Equals("WALL", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("WALL-", StringComparison.OrdinalIgnoreCase);
    }
}
