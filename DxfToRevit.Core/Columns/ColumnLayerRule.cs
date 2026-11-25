namespace DxfToRevit.Core.Columns;

/// <summary>
/// Column layer naming rule: a layer is a column layer when it equals one of the names (default
/// <c>COLUMN</c>, <c>COLUMNS</c>) or starts with one of the prefixes (default <c>COLUMN-</c>).
/// Matching is case-insensitive and ignores surrounding whitespace.
/// </summary>
/// <remarks>
/// The names are data, so an office standard with other layer names only needs another rule in
/// <c>LayerRules</c>, not a code change.
/// </remarks>
public sealed class ColumnLayerRule
{
    private readonly string[] _names;
    private readonly string[] _prefixes;

    /// <summary>Creates the rule.</summary>
    /// <param name="names">Exact layer names; <c>COLUMN</c> and <c>COLUMNS</c> when <c>null</c>.</param>
    /// <param name="prefixes">Layer name prefixes; <c>COLUMN-</c> when <c>null</c>.</param>
    /// <exception cref="ArgumentException">Neither a name nor a prefix is given, or one is blank.</exception>
    public ColumnLayerRule(IEnumerable<string>? names = null, IEnumerable<string>? prefixes = null)
    {
        _names = (names ?? new[] { "COLUMN", "COLUMNS" }).Select(n => n?.Trim() ?? string.Empty).ToArray();
        _prefixes = (prefixes ?? new[] { "COLUMN-" }).Select(p => p?.Trim() ?? string.Empty).ToArray();
        if (_names.Length + _prefixes.Length == 0 || _names.Concat(_prefixes).Any(n => n.Length == 0))
            throw new ArgumentException("At least one non-blank column layer name or prefix is required.");
    }

    /// <summary>The exact layer names.</summary>
    public IReadOnlyList<string> Names => _names;

    /// <summary>The layer name prefixes.</summary>
    public IReadOnlyList<string> Prefixes => _prefixes;

    /// <summary>Whether the layer is a column layer.</summary>
    /// <param name="layerName">Layer name.</param>
    public bool IsColumnLayer(string? layerName)
    {
        if (string.IsNullOrWhiteSpace(layerName))
            return false;

        string name = layerName.Trim();
        return _names.Any(n => name.Equals(n, StringComparison.OrdinalIgnoreCase))
               || _prefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}
