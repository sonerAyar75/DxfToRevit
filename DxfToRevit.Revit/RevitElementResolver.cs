#if REVIT_API_AVAILABLE
using System.Globalization;
using System.IO;
using Autodesk.Revit.DB;
using DxfToRevit.Core.Building;
using DxfToRevit.Core.Openings;

namespace DxfToRevit.Revit;

/// <summary>
/// Resolves levels, wall types, column family types and door/window types in a Revit document,
/// creating new wall, column or opening types (and loading round column families) when needed.
/// </summary>
/// <remarks>
/// The selection logic lives in the API-free planners (<see cref="LevelMatchPlanner"/>,
/// <see cref="WallTypeMatchPlanner"/>, <see cref="ColumnTypeMatchPlanner"/>); this class only reads
/// the document collections and performs the Revit changes. Each change (duplicate type, load
/// family) runs in its own transaction. Results are cached for the lifetime of the resolver so
/// the document is not scanned again for every element.
/// </remarks>
internal sealed class RevitElementResolver
{
    private readonly Document _document;
    private readonly LevelMatchPlanner _levelPlanner = new();
    private readonly WallTypeMatchPlanner _wallTypePlanner;
    private readonly Dictionary<string, WallType> _wallTypeCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ColumnTypeMatchPlanner _columnPlanner;
    private readonly RevitColumnParameterNames _columnParams;
    private readonly Dictionary<string, FamilySymbol> _columnSymbolCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly RevitOpeningParameterNames _openingParams;
    private readonly Dictionary<string, FamilySymbol> _openingSymbolCache = new(StringComparer.OrdinalIgnoreCase);
    private List<Level>? _levels;
    private bool _roundFamilyLoadAttempted;

    /// <summary>Creates the resolver.</summary>
    /// <param name="document">Project document.</param>
    /// <param name="widthToleranceMm">Size tolerance (mm) when matching wall and column types.</param>
    /// <param name="columnParameterNames">Column size parameter names; defaults to <see cref="RevitColumnParameterNames.Default"/>.</param>
    /// <param name="openingParameterNames">Door/window size parameter names; defaults to <see cref="RevitOpeningParameterNames.Default"/>.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public RevitElementResolver(
        Document document,
        double widthToleranceMm = 0.5,
        RevitColumnParameterNames? columnParameterNames = null,
        RevitOpeningParameterNames? openingParameterNames = null)
    {
        _openingParams = openingParameterNames ?? RevitOpeningParameterNames.Default;
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _wallTypePlanner = new WallTypeMatchPlanner(widthToleranceMm);
        _columnPlanner = new ColumnTypeMatchPlanner(widthToleranceMm);
        _columnParams = columnParameterNames ?? RevitColumnParameterNames.Default;
    }

    /// <summary>Returns the level with the given name, or the lowest level when no name is given.</summary>
    /// <param name="name">Level name, or <c>null</c>.</param>
    /// <param name="error">Reason when no level is found.</param>
    public Level? ResolveLevel(string? name, out string? error)
    {
        _levels ??= new FilteredElementCollector(_document)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .ToList();

        var candidates = _levels.Select(l => new LevelCandidate(l.Name, l.Elevation)).ToList();
        int index = _levelPlanner.Plan(candidates, name, out error);
        return index < 0 ? null : _levels[index];
    }

    /// <summary>
    /// Returns a Basic Wall type for a type/family key and thickness. When the family exists but has
    /// no type of that thickness, a new type is created (in its own transaction).
    /// </summary>
    /// <param name="key">Wall type name or Basic Wall family name.</param>
    /// <param name="thicknessMm">Required thickness in millimetres.</param>
    /// <param name="error">Reason when no type could be provided.</param>
    public WallType? ResolveWallType(string key, double thicknessMm, out string? error)
    {
        error = null;
        string cacheKey = $"{key.Trim()}|{thicknessMm.ToString("0.###", CultureInfo.InvariantCulture)}";
        if (_wallTypeCache.TryGetValue(cacheKey, out WallType? cached) && cached.IsValidObject)
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

        WallTypeMatch match = _wallTypePlanner.Plan(candidates, key, thicknessMm);

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
            _wallTypeCache[cacheKey] = result;

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

    /// <summary>
    /// Returns an architectural column (<c>OST_Columns</c>) family type with the required size,
    /// duplicating and resizing a type when no existing type matches.
    /// </summary>
    /// <remarks>
    /// The key can be <see cref="ColumnTypeKeys.AutoRoundFamily"/> (choose or load a round family),
    /// a <c>Family::Type</c> pair selected in the UI (used as the template), or a type/family name.
    /// For circular columns <paramref name="widthMm"/> = <paramref name="depthMm"/> = diameter.
    /// </remarks>
    /// <param name="key">Type key.</param>
    /// <param name="kind">Section kind.</param>
    /// <param name="widthMm">Required width (or diameter) in millimetres.</param>
    /// <param name="depthMm">Required depth (or diameter) in millimetres.</param>
    /// <param name="error">Reason when no type could be provided.</param>
    public FamilySymbol? ResolveColumnSymbol(
        string key, ColumnSectionKind kind, double widthMm, double depthMm, out string? error)
    {
        error = null;
        bool circular = kind == ColumnSectionKind.Circular;
        string cacheKey = $"{key.Trim()}|{kind}|{Fmt(widthMm)}|{Fmt(depthMm)}";
        if (_columnSymbolCache.TryGetValue(cacheKey, out FamilySymbol? cached) && cached.IsValidObject)
            return cached;

        List<FamilySymbol> symbols = CollectColumnSymbols();

        // Automatic round family: a loaded family with a diameter, otherwise load one from disk.
        if (string.Equals(key, ColumnTypeKeys.AutoRoundFamily, StringComparison.Ordinal))
        {
            FamilySymbol? template = PickRoundTemplate(symbols, widthMm);
            string? loadError = null;
            if (template is null && TryLoadRoundColumnFamily(out loadError))
            {
                symbols = CollectColumnSymbols();
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
                _columnSymbolCache[cacheKey] = auto;
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
                _columnSymbolCache[cacheKey] = resolved;
            return resolved;
        }

        if (symbols.Count == 0)
        {
            error = "No architectural column family is loaded. " +
                    "Load one via Insert > Load Family > Architecture > Columns.";
            return null;
        }

        var candidates = symbols.Select(s => ReadColumnCandidate(s, circular)).ToList();
        ColumnTypeMatch match = _columnPlanner.Plan(candidates, key, widthMm, depthMm, circular);

        FamilySymbol? result;
        switch (match.Kind)
        {
            case ColumnTypeMatchKind.ExactName:
            case ColumnTypeMatchKind.FamilySizeMatch:
                result = symbols[match.Index];
                break;

            case ColumnTypeMatchKind.DuplicateFromFamily:
                result = DuplicateColumnType(
                    symbols[match.Index], match.NewTypeName!, circular, widthMm, depthMm, out error);
                break;

            default:
                string families = string.Join(", ",
                    symbols.Select(s => $"'{s.FamilyName}'").Distinct().Take(10));
                error = $"Column family/type '{key}' not found. Available column families: {families}.";
                result = null;
                break;
        }

        if (result is not null)
            _columnSymbolCache[cacheKey] = result;

        return result;
    }

    /// <summary>
    /// Returns a door or window type of the required size. The type referenced by
    /// <paramref name="key"/> (<c>Family::Type</c> or a type name) is the template: it is used as is
    /// when its size matches, otherwise a type of the same family with that size is reused, and
    /// otherwise the template is duplicated as <c>&lt;Family&gt; - &lt;w&gt;x&lt;h&gt;mm</c> and resized.
    /// </summary>
    /// <param name="key">Type key from the opening type map.</param>
    /// <param name="category">Door or window.</param>
    /// <param name="widthMm">Required width in millimetres.</param>
    /// <param name="heightMm">Required height in millimetres.</param>
    /// <param name="error">Reason when no type could be provided.</param>
    public FamilySymbol? ResolveOpeningSymbol(
        string key, OpeningCategory category, double widthMm, double heightMm, out string? error)
    {
        error = null;
        if (!OpeningTypeKey.TryParse(key, out OpeningTypeKey? typeKey) || typeKey is null)
        {
            error = $"Invalid {category} type key '{key}'; expected 'Family::Type'.";
            return null;
        }

        string cacheKey = $"{category}|{typeKey}|{Fmt(widthMm)}|{Fmt(heightMm)}";
        if (_openingSymbolCache.TryGetValue(cacheKey, out FamilySymbol? cached) && cached.IsValidObject)
            return cached;

        List<FamilySymbol> symbols = CollectOpeningSymbols(category);
        FamilySymbol? template = symbols.FirstOrDefault(s => typeKey.Matches(s.FamilyName, s.Name));
        if (template is null)
        {
            error = $"{category} type '{typeKey}' is not loaded in the project.";
            return null;
        }

        FamilySymbol? result = OpeningSizeMatches(template, widthMm, heightMm)
            ? template
            : symbols.FirstOrDefault(s =>
                  string.Equals(s.FamilyName, template.FamilyName, StringComparison.OrdinalIgnoreCase) &&
                  OpeningSizeMatches(s, widthMm, heightMm));

        if (result is null)
        {
            string newName = ColumnTypeMatchPlanner.BuildUniqueName(
                symbols.Select(s => s.Name), template.FamilyName, widthMm, heightMm, circular: false);
            result = DuplicateOpeningType(template, newName, category, widthMm, heightMm, out error);
        }

        if (result is not null)
            _openingSymbolCache[cacheKey] = result;

        return result;
    }

    /// <summary>All door or window types of the document, sorted by family and type name.</summary>
    /// <param name="category">Door or window.</param>
    public List<FamilySymbol> CollectOpeningSymbols(OpeningCategory category) =>
        new FilteredElementCollector(_document)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(category == OpeningCategory.Window ? BuiltInCategory.OST_Windows : BuiltInCategory.OST_Doors)
            .Cast<FamilySymbol>()
            .OrderBy(s => s.FamilyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Whether the type's width and height match within the size tolerance.</summary>
    private bool OpeningSizeMatches(FamilySymbol symbol, double widthMm, double heightMm) =>
        ReadOpeningLengthMm(symbol, _openingParams.Width, BuiltInParameter.FAMILY_WIDTH_PARAM) is double w &&
        ReadOpeningLengthMm(symbol, _openingParams.Height, BuiltInParameter.FAMILY_HEIGHT_PARAM) is double h &&
        Math.Abs(w - widthMm) <= _columnPlanner.SizeToleranceMm &&
        Math.Abs(h - heightMm) <= _columnPlanner.SizeToleranceMm;

    /// <summary>Duplicates a door/window type under a new name and sets its width and height (own transaction).</summary>
    private FamilySymbol? DuplicateOpeningType(
        FamilySymbol source, string newName, OpeningCategory category, double widthMm, double heightMm, out string? error)
    {
        error = null;
        try
        {
            using var tx = RevitChangeScope.Start(_document, $"DxfToRevit Create {category} Type");

            var newSymbol = (FamilySymbol)source.Duplicate(newName);
            Parameter? w = FindOpeningParameter(newSymbol, _openingParams.Width, BuiltInParameter.FAMILY_WIDTH_PARAM);
            Parameter? h = FindOpeningParameter(newSymbol, _openingParams.Height, BuiltInParameter.FAMILY_HEIGHT_PARAM);
            if (w is null || h is null)
            {
                tx.RollBack();
                error = $"Family '{source.FamilyName}' has no writable Width/Height type parameters " +
                        $"(looked for: {string.Join("/", _openingParams.Width)}, {string.Join("/", _openingParams.Height)}). " +
                        $"Writable length parameters: {DescribeWritableLengthParameters(source)}.";
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
    private static Parameter? FindOpeningParameter(Element element, IReadOnlyList<string> names, BuiltInParameter fallback)
    {
        Parameter? p = FindParameter(element, names, writable: true);
        if (p is { StorageType: StorageType.Double })
            return p;

        Parameter? builtIn = element.get_Parameter(fallback);
        return builtIn is { StorageType: StorageType.Double, IsReadOnly: false } ? builtIn : null;
    }

    /// <summary>Reads a size parameter by name, then the built-in parameter, in millimetres.</summary>
    private static double? ReadOpeningLengthMm(Element element, IReadOnlyList<string> names, BuiltInParameter fallback) =>
        ReadLengthMm(element, names) ??
        (element.get_Parameter(fallback) is { StorageType: StorageType.Double } p
            ? LengthUnitConverter.FeetToMm(p.AsDouble())
            : null);

    /// <summary>All architectural column types of the document, sorted by family and type name.</summary>
    private List<FamilySymbol> CollectColumnSymbols() =>
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
        ColumnTypeCandidate candidate = ReadColumnCandidate(template, circular);
        bool sizeMatches = double.IsFinite(candidate.WidthMm) &&
                           double.IsFinite(candidate.DepthMm) &&
                           Math.Abs(candidate.WidthMm - widthMm) <= _columnPlanner.SizeToleranceMm &&
                           Math.Abs(candidate.DepthMm - depthMm) <= _columnPlanner.SizeToleranceMm;
        if (sizeMatches)
            return template;

        string newTypeName = ColumnTypeMatchPlanner.BuildUniqueName(
            symbols.Select(s => s.Name), template.FamilyName, widthMm, depthMm, circular);
        return DuplicateColumnType(template, newTypeName, circular, widthMm, depthMm, out error);
    }

    /// <summary>
    /// Picks a loaded column type with a diameter parameter. Preference: a type that already has
    /// the diameter → a family whose name suggests a round column → the first round family.
    /// </summary>
    private FamilySymbol? PickRoundTemplate(IReadOnlyList<FamilySymbol> symbols, double diameterMm)
    {
        var round = symbols
            .Where(s => FindParameter(s, _columnParams.Diameter) is { StorageType: StorageType.Double })
            .ToList();
        if (round.Count == 0)
            return null;

        FamilySymbol? exact = round.FirstOrDefault(s =>
            ReadLengthMm(s, _columnParams.Diameter) is double d &&
            Math.Abs(d - diameterMm) <= _columnPlanner.SizeToleranceMm);
        if (exact is not null)
            return exact;

        return round.FirstOrDefault(s => _columnParams.RoundFamilyNameHints.Any(
                   h => s.FamilyName.Contains(h, StringComparison.OrdinalIgnoreCase)))
               ?? round[0];
    }

    /// <summary>
    /// Finds a round column family (.rfa) on disk and loads it into the project. Searches the
    /// add-in's <c>Resources/Families</c> folder first, then the Revit library. Only one attempt is
    /// made per resolver, so a missing family does not trigger a disk search for every column.
    /// </summary>
    private bool TryLoadRoundColumnFamily(out string? error)
    {
        error = null;
        if (_roundFamilyLoadAttempted)
        {
            error = "No round column family could be loaded automatically.";
            return false;
        }

        _roundFamilyLoadAttempted = true;

        var app = _document.Application;
        string? addinDir = Path.GetDirectoryName(typeof(RevitElementResolver).Assembly.Location);
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
    private ColumnTypeCandidate ReadColumnCandidate(FamilySymbol symbol, bool circular)
    {
        double w, d;
        if (circular)
        {
            double dia = ReadLengthMm(symbol, _columnParams.Diameter) ?? ReadLengthMm(symbol, _columnParams.Width) ?? double.NaN;
            w = d = dia;
        }
        else
        {
            w = ReadLengthMm(symbol, _columnParams.Width) ?? double.NaN;
            d = ReadLengthMm(symbol, _columnParams.Depth) ?? double.NaN;
        }

        return new ColumnTypeCandidate(symbol.Name, symbol.FamilyName, w, d);
    }

    /// <summary>Duplicates a column type under a new name and sets its size (own transaction).</summary>
    private FamilySymbol? DuplicateColumnType(
        FamilySymbol source, string newName, bool circular, double widthMm, double depthMm, out string? error)
    {
        error = null;
        try
        {
            using var tx = RevitChangeScope.Start(_document, "DxfToRevit Create ColumnType");

            var newSymbol = (FamilySymbol)source.Duplicate(newName);
            if (!TrySetColumnSize(newSymbol, circular, widthMm, depthMm, out error))
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
    private bool TrySetColumnSize(
        FamilySymbol symbol, bool circular, double widthMm, double depthMm, out string? error)
    {
        error = null;
        double widthFt = LengthUnitConverter.MmToFeet(widthMm);
        double depthFt = LengthUnitConverter.MmToFeet(depthMm);

        if (circular)
        {
            Parameter? dia = FindParameter(symbol, _columnParams.Diameter, writable: true);
            if (dia is not null)
                return dia.Set(widthFt) || Fail(out error, symbol, "Diameter");
        }

        Parameter? w = FindParameter(symbol, _columnParams.Width, writable: true);
        Parameter? d = FindParameter(symbol, _columnParams.Depth, writable: true);
        if (w is null || d is null)
        {
            error = $"Family '{symbol.FamilyName}' has no writable " +
                    (circular ? "Diameter or Width/Depth" : "Width/Depth") +
                    " type parameters (looked for: " +
                    string.Join("/", circular ? _columnParams.Diameter : _columnParams.Width) + ", " +
                    string.Join("/", _columnParams.Depth) + "). " +
                    $"Writable length parameters: {DescribeWritableLengthParameters(symbol)}.";
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

    /// <summary>Comma-separated names of the writable length (double) parameters of a type.</summary>
    private static string DescribeWritableLengthParameters(FamilySymbol symbol) =>
        string.Join(", ", symbol.Parameters
            .Cast<Parameter>()
            .Where(p => p.StorageType == StorageType.Double && !p.IsReadOnly)
            .Select(p => p.Definition.Name)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase));

    /// <summary>Returns the first parameter found under any of the names (optionally writable only).</summary>
    private static Parameter? FindParameter(Element element, IReadOnlyList<string> names, bool writable = false)
    {
        foreach (string name in names)
        {
            Parameter? p = element.LookupParameter(name);
            if (p is not null && (!writable || !p.IsReadOnly))
                return p;
        }

        return null;
    }

    /// <summary>Reads a length parameter in millimetres, or <c>null</c> when absent.</summary>
    private static double? ReadLengthMm(Element element, IReadOnlyList<string> names)
    {
        Parameter? p = FindParameter(element, names);
        return p is { StorageType: StorageType.Double }
            ? LengthUnitConverter.FeetToMm(p.AsDouble())
            : null;
    }

    /// <summary>Formats a size for cache keys (three decimals, invariant culture).</summary>
    private static string Fmt(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

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
