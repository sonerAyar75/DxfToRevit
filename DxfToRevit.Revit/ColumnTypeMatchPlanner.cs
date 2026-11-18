using System.Globalization;

namespace DxfToRevit.Revit;

/// <summary>
/// API-free summary of a Revit column family type used by <see cref="ColumnTypeMatchPlanner"/>.
/// Dimensions that cannot be read are <see cref="double.NaN"/>.
/// </summary>
/// <param name="Name">Type name.</param>
/// <param name="FamilyName">Family name.</param>
/// <param name="WidthMm">Width (or diameter) in millimetres.</param>
/// <param name="DepthMm">Depth (or diameter) in millimetres.</param>
public readonly record struct ColumnTypeCandidate(string Name, string FamilyName, double WidthMm, double DepthMm);

/// <summary>How a column type was matched.</summary>
public enum ColumnTypeMatchKind
{
    /// <summary>No type or family matched the key.</summary>
    NotFound,

    /// <summary>The key equals a type name (dimensions are not checked).</summary>
    ExactName,

    /// <summary>The key is a family name and a type with the right dimensions exists.</summary>
    FamilySizeMatch,

    /// <summary>The family exists but no type has the dimensions; a type should be duplicated and resized.</summary>
    DuplicateFromFamily
}

/// <summary>Planner output.</summary>
/// <param name="Kind">Kind of match.</param>
/// <param name="Index">Index into the candidate list (-1 when not found).</param>
/// <param name="NewTypeName">Name for the duplicated type when <paramref name="Kind"/> is <see cref="ColumnTypeMatchKind.DuplicateFromFamily"/>.</param>
public readonly record struct ColumnTypeMatch(ColumnTypeMatchKind Kind, int Index, string? NewTypeName = null);

/// <summary>
/// Decides which column type to use (no Revit API needed). Same strategy as
/// <see cref="WallTypeMatchPlanner"/>: type name → family with matching size → duplicate a family type.
/// </summary>
public sealed class ColumnTypeMatchPlanner
{
    /// <summary>Allowed size difference in millimetres.</summary>
    public double SizeToleranceMm { get; }

    /// <summary>Creates the planner.</summary>
    /// <param name="sizeToleranceMm">Allowed size difference in millimetres.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">The tolerance is negative.</exception>
    public ColumnTypeMatchPlanner(double sizeToleranceMm = 0.5)
    {
        if (sizeToleranceMm < 0)
            throw new ArgumentOutOfRangeException(nameof(sizeToleranceMm));
        SizeToleranceMm = sizeToleranceMm;
    }

    /// <summary>Chooses a column type.</summary>
    /// <param name="candidates">Column types of the project.</param>
    /// <param name="key">Type or family name.</param>
    /// <param name="widthMm">Required width in millimetres (the diameter for circular columns).</param>
    /// <param name="depthMm">Required depth in millimetres (the diameter for circular columns).</param>
    /// <param name="circular">Whether the column is circular; affects the generated type name.</param>
    /// <returns>The match decision.</returns>
    /// <exception cref="System.ArgumentException">The key is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">A dimension is not positive.</exception>
    public ColumnTypeMatch Plan(
        IReadOnlyList<ColumnTypeCandidate> candidates,
        string key,
        double widthMm,
        double depthMm,
        bool circular = false)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (widthMm <= 0) throw new ArgumentOutOfRangeException(nameof(widthMm));
        if (depthMm <= 0) throw new ArgumentOutOfRangeException(nameof(depthMm));

        key = key.Trim();

        for (int i = 0; i < candidates.Count; i++)
        {
            if (Same(candidates[i].Name, key))
                return new ColumnTypeMatch(ColumnTypeMatchKind.ExactName, i);
        }

        int source = -1;
        for (int i = 0; i < candidates.Count; i++)
        {
            ColumnTypeCandidate c = candidates[i];
            if (!Same(c.FamilyName, key))
                continue;

            if (Math.Abs(c.WidthMm - widthMm) <= SizeToleranceMm &&
                Math.Abs(c.DepthMm - depthMm) <= SizeToleranceMm)
                return new ColumnTypeMatch(ColumnTypeMatchKind.FamilySizeMatch, i);

            if (source < 0)
                source = i;
        }

        if (source >= 0)
        {
            string name = BuildUniqueName(
                candidates.Select(c => c.Name), candidates[source].FamilyName, widthMm, depthMm, circular);
            return new ColumnTypeMatch(ColumnTypeMatchKind.DuplicateFromFamily, source, name);
        }

        return new ColumnTypeMatch(ColumnTypeMatchKind.NotFound, -1);
    }

    /// <summary>
    /// Builds a type name that is not in <paramref name="existingNames"/>:
    /// <c>"&lt;family&gt; - D&lt;d&gt;mm"</c> for circular columns or
    /// <c>"&lt;family&gt; - &lt;w&gt;x&lt;d&gt;mm"</c> otherwise, with <c>(2)</c>, <c>(3)</c>, ... appended when taken.
    /// </summary>
    /// <param name="existingNames">Type names already in use.</param>
    /// <param name="family">Family name.</param>
    /// <param name="widthMm">Width (or diameter) in millimetres.</param>
    /// <param name="depthMm">Depth in millimetres.</param>
    /// <param name="circular">Whether to use the diameter form.</param>
    internal static string BuildUniqueName(
        IEnumerable<string> existingNames, string family,
        double widthMm, double depthMm, bool circular)
    {
        var taken = new HashSet<string>(existingNames.Select(n => n.Trim()), StringComparer.OrdinalIgnoreCase);
        string baseName = circular
            ? $"{family} - D{Fmt(widthMm)}mm"
            : $"{family} - {Fmt(widthMm)}x{Fmt(depthMm)}mm";
        string name = baseName;
        int n = 2;
        while (taken.Contains(name))
            name = $"{baseName} ({n++})";
        return name;
    }

    /// <summary>Case-insensitive, trimmed name comparison.</summary>
    private static bool Same(string a, string b) =>
        string.Equals(a.Trim(), b, StringComparison.OrdinalIgnoreCase);

    /// <summary>Formats a size with up to two decimals, invariant culture.</summary>
    private static string Fmt(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}
