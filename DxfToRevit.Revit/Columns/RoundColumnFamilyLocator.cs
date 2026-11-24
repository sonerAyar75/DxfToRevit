using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;

namespace DxfToRevit.Revit.Columns;

/// <summary>
/// Finds a loadable round column family (.rfa) on disk (no Revit API needed).
/// </summary>
/// <remarks>
/// Search order:
/// <list type="number">
/// <item><description>Add-in folder <c>Resources/Families/*.rfa</c> (the first family a user placed there).</description></item>
/// <item><description>Revit library folders: well-known round column file names (metric first).</description></item>
/// </list>
/// </remarks>
public sealed class RoundColumnFamilyLocator
{
    /// <summary>Well-known round column families of the Revit library, in order of preference.</summary>
    public static readonly IReadOnlyList<string> KnownLibraryFileNames = new[]
    {
        "M_Round Column.rfa",
        "M_Concrete-Round-Column.rfa",
        "Round Column.rfa",
        "Concrete-Round-Column.rfa"
    };

    private readonly IReadOnlyList<string?> _bundledDirectories;
    private readonly IReadOnlyList<string?> _libraryRoots;

    /// <summary>Creates the locator.</summary>
    /// <param name="bundledDirectories">Family folders shipped with the add-in (the first .rfa is used).</param>
    /// <param name="libraryRoots">Revit library root folders (searched including subfolders).</param>
    /// <exception cref="System.ArgumentNullException">An argument is <c>null</c>.</exception>
    public RoundColumnFamilyLocator(
        IEnumerable<string?> bundledDirectories,
        IEnumerable<string?> libraryRoots)
    {
        ArgumentNullException.ThrowIfNull(bundledDirectories);
        ArgumentNullException.ThrowIfNull(libraryRoots);
        _bundledDirectories = bundledDirectories.ToList();
        _libraryRoots = libraryRoots.ToList();
    }

    /// <summary>Full path of the first family found, or <c>null</c>.</summary>
    public string? Find() =>
        LibraryFileSearch.FindFirstInDirectories(_bundledDirectories, "*.rfa")
        ?? LibraryFileSearch.FindFirst(_libraryRoots, KnownLibraryFileNames);
}
