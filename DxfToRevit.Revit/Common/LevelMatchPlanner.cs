namespace DxfToRevit.Revit.Common;

/// <summary>
/// Decides which level to use for a requested level name (no Revit API needed).
/// When no name is given, the lowest level is chosen.
/// </summary>
public sealed class LevelMatchPlanner
{
    /// <summary>Returns the index of the chosen level.</summary>
    /// <param name="levels">Levels of the project.</param>
    /// <param name="name">Requested name (case-insensitive, trimmed); blank selects the lowest level.</param>
    /// <param name="error">Reason when no level is chosen.</param>
    /// <returns>The level index, or -1 when there are no levels or the name is not found.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="levels"/> is <c>null</c>.</exception>
    public int Plan(IReadOnlyList<LevelCandidate> levels, string? name, out string? error)
    {
        ArgumentNullException.ThrowIfNull(levels);
        error = null;

        if (levels.Count == 0)
        {
            error = "The project has no levels.";
            return -1;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            int lowest = 0;
            for (int i = 1; i < levels.Count; i++)
            {
                if (levels[i].ElevationFeet < levels[lowest].ElevationFeet)
                    lowest = i;
            }

            return lowest;
        }

        string wanted = name.Trim();
        for (int i = 0; i < levels.Count; i++)
        {
            if (string.Equals(levels[i].Name.Trim(), wanted, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        error = $"Level '{wanted}' not found. Available: " +
                string.Join(", ", levels.Select(l => $"'{l.Name}'")) + ".";
        return -1;
    }
}
