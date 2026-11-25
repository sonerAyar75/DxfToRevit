using System.IO;

namespace DxfToRevit.Revit.Common;

/// <summary>
/// File search in Revit library and template folders (no Revit API needed).
/// When the same file exists in several languages, the English (metric) folder wins.
/// Missing or inaccessible folders are skipped silently.
/// </summary>
public static class LibraryFileSearch
{
    /// <summary>
    /// Searches every root folder, including subfolders, for <paramref name="fileNames"/> in order
    /// (the first name is the most preferred).
    /// </summary>
    /// <param name="roots">Root folders; blank entries are ignored.</param>
    /// <param name="fileNames">File names in order of preference.</param>
    /// <returns>Full path of the first file found, or <c>null</c>.</returns>
    /// <exception cref="System.ArgumentNullException">An argument is <c>null</c>.</exception>
    public static string? FindFirst(IEnumerable<string?> roots, IEnumerable<string> fileNames)
    {
        ArgumentNullException.ThrowIfNull(roots);
        ArgumentNullException.ThrowIfNull(fileNames);

        IReadOnlyList<string> cleanRoots = Clean(roots);
        foreach (string fileName in fileNames)
        {
            foreach (string root in cleanRoots)
            {
                string? hit = Enumerate(root, fileName, recursive: true)
                    .OrderBy(PreferenceRank)
                    .ThenBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (hit is not null)
                    return hit;
            }
        }

        return null;
    }

    /// <summary>First file matching <paramref name="pattern"/> directly in one of the folders (no subfolders).</summary>
    /// <param name="directories">Folders to search; blank entries are ignored.</param>
    /// <param name="pattern">File pattern, e.g. <c>*.rfa</c>.</param>
    /// <returns>Full path of the first match (alphabetical per folder), or <c>null</c>.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="directories"/> is <c>null</c>.</exception>
    public static string? FindFirstInDirectories(IEnumerable<string?> directories, string pattern)
    {
        ArgumentNullException.ThrowIfNull(directories);
        foreach (string dir in Clean(directories))
        {
            string? hit = Enumerate(dir, pattern, recursive: false)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            if (hit is not null)
                return hit;
        }

        return null;
    }

    /// <summary>
    /// Ranks a path: inside a folder named exactly "English" 0, inside another folder whose name
    /// contains "English" (e.g. English-Imperial) 1, everything else 2.
    /// </summary>
    /// <param name="path">Full file path.</param>
    public static int PreferenceRank(string path)
    {
        if (path.Contains(Path.DirectorySeparatorChar + "English" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return 0;
        if (path.Contains("English", StringComparison.OrdinalIgnoreCase))
            return 1;
        return 2;
    }

    /// <summary>Removes blank entries, trims and de-duplicates folder paths.</summary>
    private static IReadOnlyList<string> Clean(IEnumerable<string?> dirs) =>
        dirs.Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Lists matching files case-insensitively; returns nothing for missing or inaccessible folders.</summary>
    private static IReadOnlyList<string> Enumerate(string dir, string pattern, bool recursive)
    {
        try
        {
            if (!Directory.Exists(dir))
                return Array.Empty<string>();

            var opts = new EnumerationOptions
            {
                RecurseSubdirectories = recursive,
                IgnoreInaccessible = true,
                MatchCasing = MatchCasing.CaseInsensitive
            };
            return Directory.EnumerateFiles(dir, pattern, opts).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }
    }
}
