using System.Globalization;

namespace DxfToRevit.Revit.Walls;

/// <summary>
/// Decides which wall type to use for a type key and thickness (no Revit API needed).
/// </summary>
/// <remarks>
/// <list type="number">
/// <item><description>If the key is a type name, use that type.</description></item>
/// <item><description>If the key is a family name, use the type of that family with the right thickness.</description></item>
/// <item><description>Otherwise propose duplicating the family type with the fewest layers.</description></item>
/// </list>
/// </remarks>
public sealed class WallTypeMatchPlanner
{
    /// <summary>Allowed width difference in millimetres.</summary>
    public double WidthToleranceMm { get; }

    /// <summary>Creates the planner.</summary>
    /// <param name="widthToleranceMm">Allowed width difference in millimetres.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">The tolerance is negative.</exception>
    public WallTypeMatchPlanner(double widthToleranceMm = 0.5)
    {
        if (widthToleranceMm < 0)
            throw new ArgumentOutOfRangeException(nameof(widthToleranceMm));
        WidthToleranceMm = widthToleranceMm;
    }

    /// <summary>Chooses a wall type.</summary>
    /// <param name="candidates">Wall types of the project.</param>
    /// <param name="key">Type or family name.</param>
    /// <param name="thicknessMm">Required thickness in millimetres.</param>
    /// <returns>The match decision.</returns>
    /// <exception cref="System.ArgumentException">The key is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">The thickness is not positive.</exception>
    public WallTypeMatch Plan(IReadOnlyList<WallTypeCandidate> candidates, string key, double thicknessMm)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (thicknessMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(thicknessMm));

        key = key.Trim();

        for (int i = 0; i < candidates.Count; i++)
        {
            if (Same(candidates[i].Name, key))
                return new WallTypeMatch(WallTypeMatchKind.ExactName, i);
        }

        int source = -1;
        for (int i = 0; i < candidates.Count; i++)
        {
            WallTypeCandidate c = candidates[i];
            if (!Same(c.FamilyName, key))
                continue;

            if (Math.Abs(c.WidthMm - thicknessMm) <= WidthToleranceMm)
                return new WallTypeMatch(WallTypeMatchKind.FamilyWidthMatch, i);

            if (source < 0 || Rank(c) < Rank(candidates[source]))
                source = i;
        }

        if (source >= 0)
        {
            string newName = BuildUniqueName(candidates, candidates[source].FamilyName, thicknessMm);
            return new WallTypeMatch(WallTypeMatchKind.DuplicateFromFamily, source, newName);
        }

        return new WallTypeMatch(WallTypeMatchKind.NotFound, -1);
    }

    /// <summary>Ranks types for duplication: fewer layers first; unknown layer counts last.</summary>
    private static int Rank(WallTypeCandidate c) => c.LayerCount > 0 ? c.LayerCount : int.MaxValue;

    /// <summary>Case-insensitive, trimmed name comparison.</summary>
    private static bool Same(string a, string b) =>
        string.Equals(a.Trim(), b, StringComparison.OrdinalIgnoreCase);

    /// <summary>Builds <c>"&lt;family&gt; - &lt;t&gt;mm"</c>, adding <c>(2)</c>, <c>(3)</c>, ... when the name is taken.</summary>
    private static string BuildUniqueName(
        IReadOnlyList<WallTypeCandidate> candidates, string family, double thicknessMm)
    {
        string mm = thicknessMm.ToString("0.##", CultureInfo.InvariantCulture);
        string baseName = $"{family} - {mm}mm";
        string name = baseName;
        int n = 2;
        while (candidates.Any(c => Same(c.Name, name)))
            name = $"{baseName} ({n++})";
        return name;
    }
}
