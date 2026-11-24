#if REVIT_API_AVAILABLE
using Autodesk.Revit.DB;
using DxfToRevit.Core.Openings;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;

namespace DxfToRevit.Revit.Openings;

/// <summary>
/// Finds a door or window type of the required size. The type of the type map is the template: it
/// is used as is when its size matches, otherwise a type of the same family with that size is
/// reused, and otherwise the template is duplicated and resized (in its own transaction). Results
/// are cached for the lifetime of the resolver.
/// </summary>
internal sealed class RevitOpeningTypeResolver
{
    private readonly Document _document;
    private readonly double _toleranceMm;
    private readonly RevitOpeningParameterNames _params;
    private readonly Dictionary<string, FamilySymbol> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates the resolver.</summary>
    /// <param name="document">Project document.</param>
    /// <param name="sizeToleranceMm">Size tolerance (mm) when matching types.</param>
    /// <param name="parameterNames">Door/window size parameter names; defaults to <see cref="RevitOpeningParameterNames.Default"/>.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public RevitOpeningTypeResolver(
        Document document, double sizeToleranceMm = 0.5, RevitOpeningParameterNames? parameterNames = null)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _toleranceMm = sizeToleranceMm;
        _params = parameterNames ?? RevitOpeningParameterNames.Default;
    }

    /// <summary>
    /// Returns a door or window type of the required size for a type key (<c>Family::Type</c> or a
    /// type name). A new type is named <c>&lt;Family&gt; - &lt;w&gt;x&lt;h&gt;mm</c>.
    /// </summary>
    /// <param name="key">Type key from the opening type map.</param>
    /// <param name="category">Door or window.</param>
    /// <param name="widthMm">Required width in millimetres.</param>
    /// <param name="heightMm">Required height in millimetres.</param>
    /// <param name="error">Reason when no type could be provided.</param>
    public FamilySymbol? Resolve(
        string key, OpeningCategory category, double widthMm, double heightMm, out string? error)
    {
        error = null;
        if (!OpeningTypeKey.TryParse(key, out OpeningTypeKey? typeKey) || typeKey is null)
        {
            error = $"Invalid {category} type key '{key}'; expected 'Family::Type'.";
            return null;
        }

        string cacheKey = $"{category}|{typeKey}|{RevitParameters.FormatSize(widthMm)}|{RevitParameters.FormatSize(heightMm)}";
        if (_cache.TryGetValue(cacheKey, out FamilySymbol? cached) && cached.IsValidObject)
            return cached;

        List<FamilySymbol> symbols = CollectSymbols(category);
        FamilySymbol? template = symbols.FirstOrDefault(s => typeKey.Matches(s.FamilyName, s.Name));
        if (template is null)
        {
            error = $"{category} type '{typeKey}' is not loaded in the project.";
            return null;
        }

        FamilySymbol? result = SizeMatches(template, widthMm, heightMm)
            ? template
            : symbols.FirstOrDefault(s =>
                  string.Equals(s.FamilyName, template.FamilyName, StringComparison.OrdinalIgnoreCase) &&
                  SizeMatches(s, widthMm, heightMm));

        if (result is null)
        {
            string newName = ColumnTypeMatchPlanner.BuildUniqueName(
                symbols.Select(s => s.Name), template.FamilyName, widthMm, heightMm, circular: false);
            result = Duplicate(template, newName, category, widthMm, heightMm, out error);
        }

        if (result is not null)
            _cache[cacheKey] = result;

        return result;
    }

    /// <summary>All door or window types of the document, sorted by family and type name.</summary>
    /// <param name="category">Door or window.</param>
    public List<FamilySymbol> CollectSymbols(OpeningCategory category) =>
        new FilteredElementCollector(_document)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(category == OpeningCategory.Window ? BuiltInCategory.OST_Windows : BuiltInCategory.OST_Doors)
            .Cast<FamilySymbol>()
            .OrderBy(s => s.FamilyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Whether the type's width and height match within the size tolerance.</summary>
    private bool SizeMatches(FamilySymbol symbol, double widthMm, double heightMm) =>
        ReadLengthMm(symbol, _params.Width, BuiltInParameter.FAMILY_WIDTH_PARAM) is double w &&
        ReadLengthMm(symbol, _params.Height, BuiltInParameter.FAMILY_HEIGHT_PARAM) is double h &&
        Math.Abs(w - widthMm) <= _toleranceMm &&
        Math.Abs(h - heightMm) <= _toleranceMm;

    /// <summary>Duplicates a door/window type under a new name and sets its width and height (own transaction).</summary>
    private FamilySymbol? Duplicate(
        FamilySymbol source, string newName, OpeningCategory category, double widthMm, double heightMm, out string? error)
    {
        error = null;
        try
        {
            using var tx = RevitChangeScope.Start(_document, $"DxfToRevit Create {category} Type");

            var newSymbol = (FamilySymbol)source.Duplicate(newName);
            Parameter? w = FindSizeParameter(newSymbol, _params.Width, BuiltInParameter.FAMILY_WIDTH_PARAM);
            Parameter? h = FindSizeParameter(newSymbol, _params.Height, BuiltInParameter.FAMILY_HEIGHT_PARAM);
            if (w is null || h is null)
            {
                tx.RollBack();
                error = $"Family '{source.FamilyName}' has no writable Width/Height type parameters " +
                        $"(looked for: {string.Join("/", _params.Width)}, {string.Join("/", _params.Height)}). " +
                        $"Writable length parameters: {RevitParameters.DescribeWritableLengths(source)}.";
                return null;
            }

            if (!w.Set(LengthUnitConverter.MmToFeet(widthMm)) || !h.Set(LengthUnitConverter.MmToFeet(heightMm)))
            {
                tx.RollBack();
                error = $"Could not set Width/Height on '{source.FamilyName}'.";
                return null;
            }

            tx.Commit();
            return newSymbol;
        }
        catch (Exception ex)
        {
            error = $"Failed to create {category} type '{newName}': {ex.Message}";
            return null;
        }
    }

    /// <summary>A writable size parameter by name, then the built-in parameter.</summary>
    private static Parameter? FindSizeParameter(Element element, IReadOnlyList<string> names, BuiltInParameter fallback)
    {
        Parameter? p = RevitParameters.Find(element, names, writable: true);
        if (p is { StorageType: StorageType.Double })
            return p;

        Parameter? builtIn = element.get_Parameter(fallback);
        return builtIn is { StorageType: StorageType.Double, IsReadOnly: false } ? builtIn : null;
    }

    /// <summary>Reads a size parameter by name, then the built-in parameter, in millimetres.</summary>
    private static double? ReadLengthMm(Element element, IReadOnlyList<string> names, BuiltInParameter fallback) =>
        RevitParameters.ReadLengthMm(element, names) ??
        (element.get_Parameter(fallback) is { StorageType: StorageType.Double } p
            ? LengthUnitConverter.FeetToMm(p.AsDouble())
            : null);
}
#endif
