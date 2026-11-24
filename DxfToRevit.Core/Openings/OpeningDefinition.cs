namespace DxfToRevit.Core.Openings;

/// <summary>
/// Data-driven description of one kind of opening (door, window, empty opening, ...): how its
/// marker block is recognised in the DXF and how the host creates it. A new kind of opening is a
/// new definition, not a new class.
/// </summary>
public sealed class OpeningDefinition
{
    /// <summary>Name of the kind, e.g. <c>door</c>; also used in reports.</summary>
    public string Name { get; }

    /// <summary>Host category.</summary>
    public OpeningCategory Category { get; }

    /// <summary>How the host creates the opening.</summary>
    public OpeningCreationMode CreationMode { get; }

    /// <summary>How the swing is read from the marker.</summary>
    public OpeningHandingMode HandingMode { get; }

    /// <summary>Marker block names (case-insensitive) that identify this kind.</summary>
    public IReadOnlyList<string> BlockNames { get; }

    /// <summary>Layer names (case-insensitive) used when the block name is not recognised.</summary>
    public IReadOnlyList<string> Layers { get; }

    /// <summary>Height in centimetres used when the <c>HEIGHT</c> attribute is missing.</summary>
    public double DefaultHeightCm { get; }

    /// <summary>Sill height in centimetres used when the <c>SILL_HEIGHT</c> attribute is missing.</summary>
    public double DefaultSillHeightCm { get; }

    /// <summary>Whether the <c>SILL_HEIGHT</c> attribute is expected (windows and empty openings).</summary>
    public bool UsesSillHeight { get; }

    /// <summary>Prefix of the generated opening ids, e.g. <c>D</c> → <c>D001</c>.</summary>
    public string IdPrefix { get; }

    /// <summary>Creates a definition.</summary>
    /// <param name="name">Kind name.</param>
    /// <param name="category">Host category.</param>
    /// <param name="creationMode">How the host creates the opening.</param>
    /// <param name="handingMode">How the swing is read.</param>
    /// <param name="blockNames">Marker block names.</param>
    /// <param name="layers">Fallback layer names.</param>
    /// <param name="defaultHeightCm">Default height in centimetres; must be positive.</param>
    /// <param name="defaultSillHeightCm">Default sill height in centimetres; must not be negative.</param>
    /// <param name="usesSillHeight">Whether a sill height is expected.</param>
    /// <param name="idPrefix">Id prefix.</param>
    /// <exception cref="ArgumentException">A text value is empty or no block name is given.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A default is out of range.</exception>
    public OpeningDefinition(
        string name,
        OpeningCategory category,
        OpeningCreationMode creationMode,
        OpeningHandingMode handingMode,
        IEnumerable<string> blockNames,
        IEnumerable<string> layers,
        double defaultHeightCm,
        double defaultSillHeightCm,
        bool usesSillHeight,
        string idPrefix)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(idPrefix))
            throw new ArgumentException("Id prefix cannot be empty.", nameof(idPrefix));
        ArgumentNullException.ThrowIfNull(blockNames);
        ArgumentNullException.ThrowIfNull(layers);
        if (defaultHeightCm <= 0)
            throw new ArgumentOutOfRangeException(nameof(defaultHeightCm));
        if (defaultSillHeightCm < 0)
            throw new ArgumentOutOfRangeException(nameof(defaultSillHeightCm));

        Name = name;
        Category = category;
        CreationMode = creationMode;
        HandingMode = handingMode;
        BlockNames = Clean(blockNames);
        Layers = Clean(layers);
        DefaultHeightCm = defaultHeightCm;
        DefaultSillHeightCm = defaultSillHeightCm;
        UsesSillHeight = usesSillHeight;
        IdPrefix = idPrefix;

        if (BlockNames.Count == 0)
            throw new ArgumentException("At least one block name is required.", nameof(blockNames));
    }

    /// <summary>Returns a copy with other default heights (e.g. values from the settings dialog).</summary>
    /// <param name="defaultHeightCm">Default height in centimetres.</param>
    /// <param name="defaultSillHeightCm">Default sill height in centimetres.</param>
    public OpeningDefinition WithDefaults(double defaultHeightCm, double defaultSillHeightCm) =>
        new(Name, Category, CreationMode, HandingMode, BlockNames, Layers,
            defaultHeightCm, defaultSillHeightCm, UsesSillHeight, IdPrefix);

    /// <summary>Whether <paramref name="blockName"/> identifies this kind.</summary>
    /// <param name="blockName">Block name (true name of a dynamic block).</param>
    public bool MatchesBlock(string? blockName) => Matches(BlockNames, blockName);

    /// <summary>Whether <paramref name="layer"/> identifies this kind.</summary>
    /// <param name="layer">Layer name.</param>
    public bool MatchesLayer(string? layer) => Matches(Layers, layer);

    /// <inheritdoc />
    public override string ToString() => Name;

    private static bool Matches(IReadOnlyList<string> names, string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        names.Any(n => string.Equals(n, value.Trim(), StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<string> Clean(IEnumerable<string> names) =>
        names.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).ToList().AsReadOnly();
}
