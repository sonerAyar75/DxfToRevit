namespace DxfToRevit.Core.Rules;

/// <summary>
/// Column layer naming convention: <c>COLUMN</c>, <c>COLUMNS</c> or <c>COLUMN-*</c>
/// (case-insensitive, surrounding whitespace ignored).
/// </summary>
public sealed class ColumnLayerRule
{
    /// <summary>Returns <c>true</c> when the layer is a column layer.</summary>
    /// <param name="layerName">Layer name to inspect.</param>
    public bool IsColumnLayer(string? layerName)
    {
        if (string.IsNullOrWhiteSpace(layerName))
            return false;

        string name = layerName.Trim();
        return name.Equals("COLUMN", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("COLUMN-", StringComparison.OrdinalIgnoreCase)
               || name.Equals("COLUMNS", StringComparison.OrdinalIgnoreCase);
    }
}
