#if REVIT_API_AVAILABLE
using Autodesk.Revit.DB;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;

namespace DxfToRevit.Revit.Common;

/// <summary>
/// Finds the base and top levels of the elements of an import. The choice is made by the API-free
/// <see cref="LevelMatchPlanner"/>; the levels are read from the document once.
/// </summary>
internal sealed class RevitLevelResolver
{
    private readonly Document _document;
    private readonly LevelMatchPlanner _planner = new();
    private List<Level>? _levels;

    /// <summary>Creates the resolver.</summary>
    /// <param name="document">Project document.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public RevitLevelResolver(Document document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
    }

    /// <summary>Returns the level with the given name, or the lowest level when no name is given.</summary>
    /// <param name="name">Level name, or <c>null</c>.</param>
    /// <param name="error">Reason when no level is found.</param>
    public Level? Resolve(string? name, out string? error)
    {
        _levels ??= new FilteredElementCollector(_document)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .ToList();

        var candidates = _levels.Select(l => new LevelCandidate(l.Name, l.Elevation)).ToList();
        int index = _planner.Plan(candidates, name, out error);
        return index < 0 ? null : _levels[index];
    }

    /// <summary>
    /// Resolves the base level and the optional top level. A blank top level name means
    /// "Unconnected" (<paramref name="topLevel"/> = <c>null</c>). Fails when a level is not found or
    /// the top level is not above the base level.
    /// </summary>
    /// <param name="baseLevelName">Base level name, or <c>null</c> for the lowest level.</param>
    /// <param name="topLevelName">Top level name, or <c>null</c> for unconnected.</param>
    /// <param name="baseLevel">The base level.</param>
    /// <param name="topLevel">The top level, or <c>null</c>.</param>
    /// <param name="error">Reason when the levels cannot be used.</param>
    public bool TryResolve(
        string? baseLevelName,
        string? topLevelName,
        out Level baseLevel,
        out Level? topLevel,
        out string? error)
    {
        baseLevel = null!;
        topLevel = null;

        Level? level = Resolve(baseLevelName, out error);
        if (level is null)
        {
            error ??= "Level not resolved.";
            return false;
        }

        baseLevel = level;
        if (string.IsNullOrWhiteSpace(topLevelName))
            return true;

        topLevel = Resolve(topLevelName, out string? resolveError);
        if (topLevel is null)
        {
            error = resolveError ?? "Top Level not resolved.";
            return false;
        }

        if (topLevel.Elevation <= baseLevel.Elevation)
        {
            error = "Top Level must be higher than Base Level.";
            topLevel = null;
            return false;
        }

        return true;
    }
}
#endif
