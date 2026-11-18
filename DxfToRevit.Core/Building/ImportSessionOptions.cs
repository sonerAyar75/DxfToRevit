namespace DxfToRevit.Core.Building;

/// <summary>
/// Host policy for one DXF import session: which levels to use, the default height, the
/// thickness → wall type mapping and the default column type. It collects the context that does
/// not come from the drawing itself.
/// </summary>
/// <remarks>
/// Typical use for a single floor plan: <c>ImportSessionOptions.ForLevel("Level 1", heightMm: 3000)</c>.
/// Plans of other floors are imported in separate sessions.
/// </remarks>
public sealed class ImportSessionOptions
{
    /// <summary>Base level name; <c>null</c> lets the host choose its default level.</summary>
    public string? BaseLevelName { get; }

    /// <summary>Top level name that walls and columns are bound to; <c>null</c> means "Unconnected".</summary>
    public string? TopLevelName { get; }

    /// <summary>Unconnected / default element height in millimetres.</summary>
    public double DefaultHeightMm { get; }

    /// <summary>Base offset in millimetres (default 0).</summary>
    public double BaseOffsetMm { get; }

    /// <summary>
    /// Wall thickness in millimetres → wall type key. Thicknesses without a mapping use
    /// <see cref="DefaultWallTypeKey"/>.
    /// </summary>
    public IReadOnlyDictionary<double, string> WallTypeByThicknessMm { get; }

    /// <summary>Wall type key used when a thickness has no mapping.</summary>
    public string DefaultWallTypeKey { get; }

    /// <summary>Column family/type key used for rectangular columns.</summary>
    public string DefaultColumnTypeKey { get; }

    /// <summary>Whether walls are created as structural (default <c>false</c>).</summary>
    public bool WallsAreStructural { get; }

    /// <summary>Whether columns are created as structural (default <c>true</c>).</summary>
    public bool ColumnsAreStructural { get; }

    /// <summary>Tolerance in millimetres when matching a thickness against <see cref="WallTypeByThicknessMm"/>.</summary>
    public double ThicknessMatchToleranceMm { get; }

    /// <summary>Creates the session options.</summary>
    /// <param name="baseLevelName">Base level name, or <c>null</c> for the host default.</param>
    /// <param name="defaultHeightMm">Default (unconnected) height in millimetres; must be positive.</param>
    /// <param name="baseOffsetMm">Base offset in millimetres.</param>
    /// <param name="wallTypeByThicknessMm">Optional thickness → wall type key mapping.</param>
    /// <param name="defaultWallTypeKey">Fallback wall type key.</param>
    /// <param name="defaultColumnTypeKey">Default column type key.</param>
    /// <param name="wallsAreStructural">Whether walls are structural.</param>
    /// <param name="columnsAreStructural">Whether columns are structural.</param>
    /// <param name="thicknessMatchToleranceMm">Thickness matching tolerance in millimetres; must not be negative.</param>
    /// <param name="topLevelName">Top level name; blank values mean "Unconnected".</param>
    /// <exception cref="ArgumentOutOfRangeException">The height is not positive or the tolerance is negative.</exception>
    /// <exception cref="ArgumentException">A default type key is empty.</exception>
    public ImportSessionOptions(
        string? baseLevelName = null,
        double defaultHeightMm = 3000,
        double baseOffsetMm = 0,
        IReadOnlyDictionary<double, string>? wallTypeByThicknessMm = null,
        string defaultWallTypeKey = "Basic Wall",
        string defaultColumnTypeKey = "Rectangular Column",
        bool wallsAreStructural = false,
        bool columnsAreStructural = true,
        double thicknessMatchToleranceMm = 0.5,
        string? topLevelName = null)
    {
        if (defaultHeightMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(defaultHeightMm));
        if (string.IsNullOrWhiteSpace(defaultWallTypeKey))
            throw new ArgumentException("Default wall type key required.", nameof(defaultWallTypeKey));
        if (string.IsNullOrWhiteSpace(defaultColumnTypeKey))
            throw new ArgumentException("Default column type key required.", nameof(defaultColumnTypeKey));
        if (thicknessMatchToleranceMm < 0)
            throw new ArgumentOutOfRangeException(nameof(thicknessMatchToleranceMm));

        BaseLevelName = baseLevelName;
        TopLevelName = string.IsNullOrWhiteSpace(topLevelName) ? null : topLevelName;
        DefaultHeightMm = defaultHeightMm;
        BaseOffsetMm = baseOffsetMm;
        WallTypeByThicknessMm = wallTypeByThicknessMm ?? new Dictionary<double, string>();
        DefaultWallTypeKey = defaultWallTypeKey;
        DefaultColumnTypeKey = defaultColumnTypeKey;
        WallsAreStructural = wallsAreStructural;
        ColumnsAreStructural = columnsAreStructural;
        ThicknessMatchToleranceMm = thicknessMatchToleranceMm;
    }

    /// <summary>Shortcut for importing a single floor plan onto one level.</summary>
    /// <param name="levelName">Base level name.</param>
    /// <param name="heightMm">Default height in millimetres.</param>
    /// <param name="wallTypeByThicknessMm">Optional thickness → wall type key mapping.</param>
    /// <returns>Session options with all other values at their defaults.</returns>
    public static ImportSessionOptions ForLevel(
        string levelName,
        double heightMm = 3000,
        IReadOnlyDictionary<double, string>? wallTypeByThicknessMm = null) =>
        new(baseLevelName: levelName, defaultHeightMm: heightMm, wallTypeByThicknessMm: wallTypeByThicknessMm);

    /// <summary>
    /// Returns the wall type key for a thickness: the first mapping within
    /// <see cref="ThicknessMatchToleranceMm"/>, otherwise <see cref="DefaultWallTypeKey"/>.
    /// </summary>
    /// <param name="thicknessMm">Wall thickness in millimetres.</param>
    public string ResolveWallTypeKey(double thicknessMm)
    {
        foreach (var kv in WallTypeByThicknessMm)
        {
            if (Math.Abs(kv.Key - thicknessMm) <= ThicknessMatchToleranceMm)
                return kv.Value;
        }

        return DefaultWallTypeKey;
    }

    /// <summary>Creates the <see cref="ElementPlacement"/> shared by every element of this session.</summary>
    public ElementPlacement CreatePlacement() =>
        new(
            levelName: BaseLevelName,
            baseOffsetMm: BaseOffsetMm,
            heightMm: DefaultHeightMm,
            topLevelName: TopLevelName);
}
