#if REVIT_API_AVAILABLE
using Autodesk.Revit.DB;
using DxfToRevit.Core.Columns;
using DxfToRevit.Revit.Common;
using System.IO;

namespace DxfToRevit.Revit.Columns;

/// <summary>
/// Finds an architectural column (<c>OST_Columns</c>) family type of the required size, duplicating
/// and resizing a type when no existing type matches, and loading a round column family from disk
/// when needed. The choice is made by the API-free <see cref="ColumnTypeMatchPlanner"/>; each change
/// runs in its own transaction and results are cached for the lifetime of the resolver.
/// </summary>
internal sealed class RevitColumnTypeResolver
{
    private readonly Document _document;
    private readonly ColumnTypeMatchPlanner _planner;
    private readonly RevitColumnParameterNames _params;
    private readonly Dictionary<string, FamilySymbol> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _roundFamilyLoadAttempted;

    /// <summary>Creates the resolver.</summary>
    /// <param name="document">Project document.</param>
    /// <param name="sizeToleranceMm">Size tolerance (mm) when matching types.</param>
    /// <param name="parameterNames">Column size parameter names; defaults to <see cref="RevitColumnParameterNames.Default"/>.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public RevitColumnTypeResolver(
        Document document, double sizeToleranceMm = 0.5, RevitColumnParameterNames? parameterNames = null)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _planner = new ColumnTypeMatchPlanner(sizeToleranceMm);
        _params = parameterNames ?? RevitColumnParameterNames.Default;
    }

    /// <summary>
    /// Returns a column type with the required size. The key can be
    /// <see cref="ColumnTypeKeys.AutoRoundFamily"/> (choose or load a round family), a
    /// <c>Family::Type</c> pair selected in the UI (used as the template), or a type/family name.
    /// For circular columns <paramref name="widthMm"/> = <paramref name="depthMm"/> = diameter.
    /// </summary>
    /// <param name="key">Type key.</param>
    /// <param name="kind">Section kind.</param>
    /// <param name="widthMm">Required width (or diameter) in millimetres.</param>
    /// <param name="depthMm">Required depth (or diameter) in millimetres.</param>
    /// <param name="error">Reason when no type could be provided.</param>
    public FamilySymbol? Resolve(
        string key, ColumnSectionKind kind, double widthMm, double depthMm, out string? error)
    {
        error = null;
        bool circular = kind == ColumnSectionKind.Circular;
        string cacheKey = $"{key.Trim()}|{kind}|{RevitParameters.FormatSize(widthMm)}|{RevitParameters.FormatSize(depthMm)}";
        if (_cache.TryGetValue(cacheKey, out FamilySymbol? cached) && cached.IsValidObject)
            return cached;

        List<FamilySymbol> symbols = CollectSymbols();

        // Automatic round family: a loaded family with a diameter, otherwise load one from disk.
        if (string.Equals(key, ColumnTypeKeys.AutoRoundFamily, StringComparison.Ordinal))
        {
            FamilySymbol? template = PickRoundTemplate(symbols, widthMm);
            string? loadError = null;
            if (template is null && TryLoadRoundFamily(out loadError))
            {
                symbols = CollectSymbols();
                template = PickRoundTemplate(symbols, widthMm);
            }

            if (template is null)
            {
                error = loadError ??
                        "No circular column family is loaded and none could be found in the add-in " +
                        "Resources/Families folder or the Revit library.";
                return null;
            }

            FamilySymbol? auto = UseAsTemplate(template, symbols, circular: true, widthMm, depthMm, out error);
            if (auto is not null)
                _cache[cacheKey] = auto;
            return auto;
        }

        // A Family::Type key from the settings form uses the selected type as the template;
        // when the DXF size differs, the type is duplicated and resized.
        int separatorIndex = key.IndexOf("::", StringComparison.Ordinal);
        if (separatorIndex >= 0)
        {
            string familyName = key[..separatorIndex].Trim();
            string typeName = key[(separatorIndex + 2)..].Trim();
            FamilySymbol? selected = symbols.FirstOrDefault(s =>
                string.Equals(s.FamilyName, familyName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(s.Name, typeName, StringComparison.OrdinalIgnoreCase));

            if (selected is null)
            {
                error = $"Selected column type '{familyName} : {typeName}' is no longer loaded.";
                return null;
            }

            FamilySymbol? resolved = UseAsTemplate(selected, symbols, circular, widthMm, depthMm, out error);
            if (resolved is not null)
                _cache[cacheKey] = resolved;
            return resolved;
        }

        if (symbols.Count == 0)
        {
            error = "No architectural column family is loaded. " +
                    "Load one via Insert > Load Family > Architecture > Columns.";
            return null;
        }

        var candidates = symbols.Select(s => ReadCandidate(s, circular)).ToList();
        ColumnTypeMatch match = _planner.Plan(candidates, key, widthMm, depthMm, circular);

        FamilySymbol? result;
        switch (match.Kind)
        {
            case ColumnTypeMatchKind.ExactName:
            case ColumnTypeMatchKind.FamilySizeMatch:
                result = symbols[match.Index];
                break;

            case ColumnTypeMatchKind.DuplicateFromFamily:
                result = Duplicate(symbols[match.Index], match.NewTypeName!, circular, widthMm, depthMm, out error);
                break;

            default:
                string families = string.Join(", ",
                    symbols.Select(s => $"'{s.FamilyName}'").Distinct().Take(10));
                error = $"Column family/type '{key}' not found. Available column families: {families}.";
                result = null;
                break;
        }

        if (result is not null)
            _cache[cacheKey] = result;

        return result;
    }

    /// <summary>All architectural column types of the document, sorted by family and type name.</summary>
    private List<FamilySymbol> CollectSymbols() =>
        new FilteredElementCollector(_document)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_Columns)
            .Cast<FamilySymbol>()
            .OrderBy(s => s.FamilyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// Uses <paramref name="template"/> directly when its size matches the DXF; otherwise duplicates
    /// it and sets the size on the copy.
    /// </summary>
    private FamilySymbol? UseAsTemplate(
        FamilySymbol template, IReadOnlyList<FamilySymbol> symbols, bool circular,
        double widthMm, double depthMm, out string? error)
    {
        error = null;
        ColumnTypeCandidate candidate = ReadCandidate(template, circular);
        bool sizeMatches = double.IsFinite(candidate.WidthMm) &&
                           double.IsFinite(candidate.DepthMm) &&
                           Math.Abs(candidate.WidthMm - widthMm) <= _planner.SizeToleranceMm &&
                           Math.Abs(candidate.DepthMm - depthMm) <= _planner.SizeToleranceMm;
        if (sizeMatches)
            return template;

        string newTypeName = ColumnTypeMatchPlanner.BuildUniqueName(
            symbols.Select(s => s.Name), template.FamilyName, widthMm, depthMm, circular);
        return Duplicate(template, newTypeName, circular, widthMm, depthMm, out error);
    }

    /// <summary>
    /// Picks a loaded column type with a diameter parameter. Preference: a type that already has
    /// the diameter → a family whose name suggests a round column → the first round family.
    /// </summary>
    private FamilySymbol? PickRoundTemplate(IReadOnlyList<FamilySymbol> symbols, double diameterMm)
    {
        var round = symbols
            .Where(s => RevitParameters.Find(s, _params.Diameter) is { StorageType: StorageType.Double })
            .ToList();
        if (round.Count == 0)
            return null;

        FamilySymbol? exact = round.FirstOrDefault(s =>
            RevitParameters.ReadLengthMm(s, _params.Diameter) is double d &&
            Math.Abs(d - diameterMm) <= _planner.SizeToleranceMm);
        if (exact is not null)
            return exact;

        return round.FirstOrDefault(s => _params.RoundFamilyNameHints.Any(
                   h => s.FamilyName.Contains(h, StringComparison.OrdinalIgnoreCase)))
               ?? round[0];
    }

    /// <summary>
    /// Finds a round column family (.rfa) on disk and loads it into the project. Searches the
    /// add-in's <c>Resources/Families</c> folder first, then the Revit library. Only one attempt is
    /// made per resolver, so a missing family does not trigger a disk search for every column.
    /// </summary>
    private bool TryLoadRoundFamily(out string? error)
    {
        error = null;
        if (_roundFamilyLoadAttempted)
        {
            error = "No round column family could be loaded automatically.";
            return false;
        }

        _roundFamilyLoadAttempted = true;

        var app = _document.Application;
        string? addinDir = Path.GetDirectoryName(typeof(RevitColumnTypeResolver).Assembly.Location);
        var bundled = new string?[] { addinDir is null ? null : Path.Combine(addinDir, "Resources", "Families") };

        var libraryRoots = new List<string?>();
        try
        {
            libraryRoots.AddRange(app.GetLibraryPaths().Values);
        }
        catch
        {
            // Library paths could not be read; fall back to the default folder below.
        }

        libraryRoots.Add(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Autodesk", $"RVT {app.VersionNumber}", "Libraries"));

        string? path = new RoundColumnFamilyLocator(bundled, libraryRoots).Find();
        if (path is null)
        {
            error = "No round column family (.rfa) was found in the add-in Resources/Families folder " +
                    "or the Revit library.";
            return false;
        }

        try
        {
            using var tx = RevitChangeScope.Start(_document, "DxfToRevit Load Round Column Family");
            _document.LoadFamily(path, out Family _);
            tx.Commit();
            return true;
        }
        catch (Exception ex)
        {
            error = $"Could not load round column family '{Path.GetFileName(path)}': {ex.Message}";
            return false;
        }
    }

    /// <summary>Reads the size of a column type for the planner (diameter for circular columns).</summary>
    private ColumnTypeCandidate ReadCandidate(FamilySymbol symbol, bool circular)
    {
        double w, d;
        if (circular)
        {
            double dia = RevitParameters.ReadLengthMm(symbol, _params.Diameter)
                         ?? RevitParameters.ReadLengthMm(symbol, _params.Width)
                         ?? double.NaN;
            w = d = dia;
        }
        else
        {
            w = RevitParameters.ReadLengthMm(symbol, _params.Width) ?? double.NaN;
            d = RevitParameters.ReadLengthMm(symbol, _params.Depth) ?? double.NaN;
        }

        return new ColumnTypeCandidate(symbol.Name, symbol.FamilyName, w, d);
    }

    /// <summary>Duplicates a column type under a new name and sets its size (own transaction).</summary>
    private FamilySymbol? Duplicate(
        FamilySymbol source, string newName, bool circular, double widthMm, double depthMm, out string? error)
    {
        error = null;
        try
        {
            using var tx = RevitChangeScope.Start(_document, "DxfToRevit Create ColumnType");

            var newSymbol = (FamilySymbol)source.Duplicate(newName);
            if (!TrySetSize(newSymbol, circular, widthMm, depthMm, out error))
            {
                tx.RollBack();
                return null;
            }

            tx.Commit();
            return newSymbol;
        }
        catch (Exception ex)
        {
            error = $"Failed to create column type '{newName}': {ex.Message}";
            return null;
        }
    }

    /// <summary>
    /// Sets the type size: the diameter for circular columns when available, otherwise width and depth.
    /// The error message lists the writable length parameters when the expected ones are missing.
    /// </summary>
    private bool TrySetSize(
        FamilySymbol symbol, bool circular, double widthMm, double depthMm, out string? error)
    {
        error = null;
        double widthFt = LengthUnitConverter.MmToFeet(widthMm);
        double depthFt = LengthUnitConverter.MmToFeet(depthMm);

        if (circular)
        {
            Parameter? dia = RevitParameters.Find(symbol, _params.Diameter, writable: true);
            if (dia is not null)
                return dia.Set(widthFt) || Fail(out error, symbol, "Diameter");
        }

        Parameter? w = RevitParameters.Find(symbol, _params.Width, writable: true);
        Parameter? d = RevitParameters.Find(symbol, _params.Depth, writable: true);
        if (w is null || d is null)
        {
            error = $"Family '{symbol.FamilyName}' has no writable " +
                    (circular ? "Diameter or Width/Depth" : "Width/Depth") +
                    " type parameters (looked for: " +
                    string.Join("/", circular ? _params.Diameter : _params.Width) + ", " +
                    string.Join("/", _params.Depth) + "). " +
                    $"Writable length parameters: {RevitParameters.DescribeWritableLengths(symbol)}.";
            return false;
        }

        return (w.Set(widthFt) && d.Set(depthFt)) || Fail(out error, symbol, "Width/Depth");
    }

    /// <summary>Sets <paramref name="error"/> for a parameter that could not be written and returns <c>false</c>.</summary>
    private static bool Fail(out string? error, FamilySymbol symbol, string what)
    {
        error = $"Could not set {what} on '{symbol.FamilyName}'.";
        return false;
    }
}
#endif
