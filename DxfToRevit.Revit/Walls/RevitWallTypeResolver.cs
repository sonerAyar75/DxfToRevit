#if REVIT_API_AVAILABLE
using Autodesk.Revit.DB;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;

namespace DxfToRevit.Revit.Walls;

/// <summary>
/// Finds a Basic Wall type of the required thickness, creating one (in its own transaction) when the
/// family exists without that thickness. The choice is made by the API-free
/// <see cref="WallTypeMatchPlanner"/>; results are cached for the lifetime of the resolver.
/// </summary>
internal sealed class RevitWallTypeResolver
{
    private readonly Document _document;
    private readonly WallTypeMatchPlanner _planner;
    private readonly Dictionary<string, WallType> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates the resolver.</summary>
    /// <param name="document">Project document.</param>
    /// <param name="widthToleranceMm">Thickness tolerance (mm) when matching types.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public RevitWallTypeResolver(Document document, double widthToleranceMm = 0.5)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _planner = new WallTypeMatchPlanner(widthToleranceMm);
    }

    /// <summary>
    /// Returns a Basic Wall type for a type/family key and thickness. When the family exists but has
    /// no type of that thickness, a new type is created.
    /// </summary>
    /// <param name="key">Wall type name or Basic Wall family name.</param>
    /// <param name="thicknessMm">Required thickness in millimetres.</param>
    /// <param name="error">Reason when no type could be provided.</param>
    public WallType? Resolve(string key, double thicknessMm, out string? error)
    {
        error = null;
        string cacheKey = $"{key.Trim()}|{RevitParameters.FormatSize(thicknessMm)}";
        if (_cache.TryGetValue(cacheKey, out WallType? cached) && cached.IsValidObject)
            return cached;

        List<WallType> types = new FilteredElementCollector(_document)
            .OfClass(typeof(WallType))
            .Cast<WallType>()
            .Where(t => t.Kind == WallKind.Basic)
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)   // deterministic order
            .ToList();

        var candidates = types
            .Select(t => new WallTypeCandidate(
                t.Name,
                t.FamilyName,
                LengthUnitConverter.FeetToMm(t.Width),
                t.GetCompoundStructure()?.LayerCount ?? 0))
            .ToList();

        WallTypeMatch match = _planner.Plan(candidates, key, thicknessMm);

        WallType? result;
        switch (match.Kind)
        {
            case WallTypeMatchKind.ExactName:
            case WallTypeMatchKind.FamilyWidthMatch:
                result = types[match.Index];
                break;

            case WallTypeMatchKind.DuplicateFromFamily:
                result = DuplicateWithWidth(types[match.Index], match.NewTypeName!, thicknessMm, out error);
                break;

            default:
                string families = string.Join(", ",
                    types.Select(t => $"'{t.FamilyName}'").Distinct().Take(10));
                error = $"WallType '{key}' not found (neither a type name nor a Basic Wall family). " +
                        $"Available families: {families}.";
                result = null;
                break;
        }

        if (result is not null)
            _cache[cacheKey] = result;

        return result;
    }

    /// <summary>
    /// Duplicates a wall type and changes the width of its widest layer so the total width equals
    /// <paramref name="thicknessMm"/>.
    /// </summary>
    private WallType? DuplicateWithWidth(
        WallType source, string newName, double thicknessMm, out string? error)
    {
        error = null;
        try
        {
            using var tx = RevitChangeScope.Start(_document, "DxfToRevit Create WallType");

            var newType = (WallType)source.Duplicate(newName);
            CompoundStructure? cs = newType.GetCompoundStructure();
            if (cs is null || cs.LayerCount == 0)
            {
                tx.RollBack();
                error = $"WallType '{source.Name}' has no compound structure; cannot adjust thickness.";
                return null;
            }

            // Adjust the widest layer so the total width reaches the target.
            int idx = IndexOfWidestLayer(cs);
            double targetFeet = LengthUnitConverter.MmToFeet(thicknessMm);
            double newLayerWidth = cs.GetLayerWidth(idx) + (targetFeet - cs.GetWidth());
            if (newLayerWidth <= 0)
            {
                tx.RollBack();
                error = $"Cannot reach {thicknessMm} mm by adjusting the widest layer of '{source.Name}'.";
                return null;
            }

            cs.SetLayerWidth(idx, newLayerWidth);
            newType.SetCompoundStructure(cs);
            tx.Commit();
            return newType;
        }
        catch (Exception ex)
        {
            error = $"Failed to create WallType '{newName}': {ex.Message}";
            return null;
        }
    }

    /// <summary>Index of the widest layer of a compound structure.</summary>
    private static int IndexOfWidestLayer(CompoundStructure cs)
    {
        int best = 0;
        for (int i = 1; i < cs.LayerCount; i++)
        {
            if (cs.GetLayerWidth(i) > cs.GetLayerWidth(best))
                best = i;
        }

        return best;
    }
}
#endif
