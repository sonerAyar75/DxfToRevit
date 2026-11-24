namespace DxfToRevit.Revit;

/// <summary>
/// Collects the warnings Revit raises at the commit of an import. The batch suppresses warnings so
/// Revit shows no dialog per element; this log keeps them, grouped, for the import summary.
/// Free of the Revit API so it can be tested.
/// </summary>
public sealed class RevitWarningLog
{
    private readonly Dictionary<(RevitWarningKind Kind, string Text), (int Count, HashSet<string> Elements)> _groups = new();
    private readonly List<(RevitWarningKind Kind, string Text)> _order = new();

    /// <summary>Records one warning.</summary>
    /// <param name="kind">Warning kind.</param>
    /// <param name="text">Revit's description text; blank text is recorded as "Revit warning".</param>
    /// <param name="elementIds">Ids of the elements involved.</param>
    public void Add(RevitWarningKind kind, string? text, IEnumerable<string> elementIds)
    {
        ArgumentNullException.ThrowIfNull(elementIds);
        string key = string.IsNullOrWhiteSpace(text) ? "Revit warning" : text.Trim();

        if (!_groups.TryGetValue((kind, key), out var group))
        {
            group = (0, new HashSet<string>(StringComparer.Ordinal));
            _order.Add((kind, key));
        }

        group.Elements.UnionWith(elementIds);
        _groups[(kind, key)] = (group.Count + 1, group.Elements);
    }

    /// <summary>The warnings grouped by kind and text: overlaps first, then in the order first seen.</summary>
    public IReadOnlyList<RevitWarningGroup> Groups =>
        _order.OrderBy(k => k.Kind == RevitWarningKind.Overlap ? 0 : 1)
              .Select(k => new RevitWarningGroup(k.Kind, k.Text, _groups[k].Count, _groups[k].Elements.Count))
              .ToList();
}
