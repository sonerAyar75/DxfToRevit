using System.Globalization;
using System.Text.RegularExpressions;

namespace DxfToRevit.Core.Walls;

/// <summary>
/// Wall layer naming rule. A wall layer is one of the base names (default <c>WALL</c>) or a base
/// name followed by <c>-</c> and a suffix; when the suffix is a number it encodes the thickness in
/// centimetres: <c>WALL-20</c> → 200 mm. Matching is case-insensitive and ignores surrounding
/// whitespace.
/// </summary>
/// <remarks>
/// The base names are data, so an office standard with other layer names (for example
/// <c>A-WALL</c>) only needs another rule in <c>LayerRules</c>, not a code change.
/// </remarks>
public sealed class WallLayerRule
{
    private readonly string[] _names;
    private readonly Regex _thicknessRegex;

    /// <summary>Creates the rule.</summary>
    /// <param name="baseNames">Base layer names; <c>WALL</c> when <c>null</c>.</param>
    /// <exception cref="ArgumentException">No base name, or a blank one, is given.</exception>
    public WallLayerRule(IEnumerable<string>? baseNames = null)
    {
        _names = (baseNames ?? new[] { "WALL" }).Select(n => n?.Trim() ?? string.Empty).ToArray();
        if (_names.Length == 0 || _names.Any(n => n.Length == 0))
            throw new ArgumentException("At least one non-blank wall layer name is required.", nameof(baseNames));

        string alternatives = string.Join("|", _names.Select(Regex.Escape));
        _thicknessRegex = new Regex(
            $@"^\s*(?:{alternatives})-(\d+)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    /// <summary>The base layer names.</summary>
    public IReadOnlyList<string> BaseNames => _names;

    /// <summary>Reads the wall thickness from a layer name such as <c>WALL-20</c>.</summary>
    /// <param name="layerName">Layer name.</param>
    /// <param name="thicknessMm">Thickness in millimetres (suffix × 10); 0 when not found.</param>
    /// <returns><c>true</c> when the layer carries a positive thickness code.</returns>
    public bool TryGetThicknessMm(string? layerName, out double thicknessMm)
    {
        thicknessMm = 0;
        if (string.IsNullOrWhiteSpace(layerName))
            return false;

        Match match = _thicknessRegex.Match(layerName);
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

    /// <summary>Whether the layer is a wall layer (a base name, or a base name with a <c>-</c> suffix).</summary>
    /// <param name="layerName">Layer name.</param>
    public bool IsWallLayer(string? layerName)
    {
        if (string.IsNullOrWhiteSpace(layerName))
            return false;

        string name = layerName.Trim();
        return _names.Any(n =>
            name.Equals(n, StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith(n + "-", StringComparison.OrdinalIgnoreCase));
    }
}
