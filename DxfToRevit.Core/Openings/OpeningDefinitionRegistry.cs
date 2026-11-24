namespace DxfToRevit.Core.Openings;

/// <summary>
/// The set of opening kinds the DXF reader recognises. A marker is identified by its block name
/// first; the layer is only a fallback for blocks with another name.
/// </summary>
public sealed class OpeningDefinitionRegistry
{
    /// <summary>Default door height in centimetres.</summary>
    public const double DefaultDoorHeightCm = 210;

    /// <summary>Default window height in centimetres.</summary>
    public const double DefaultWindowHeightCm = 150;

    /// <summary>Default window sill height in centimetres.</summary>
    public const double DefaultWindowSillCm = 90;

    /// <summary>Default empty opening height in centimetres.</summary>
    public const double DefaultEmptyOpeningHeightCm = 210;

    /// <summary>All definitions, in recognition order.</summary>
    public IReadOnlyList<OpeningDefinition> Definitions { get; }

    /// <summary>Creates a registry.</summary>
    /// <param name="definitions">Definitions; names must be unique.</param>
    /// <exception cref="ArgumentException">Two definitions share a name.</exception>
    public OpeningDefinitionRegistry(IEnumerable<OpeningDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        Definitions = definitions.ToList().AsReadOnly();

        var duplicate = Definitions.GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new ArgumentException($"Opening definition '{duplicate.Key}' is defined twice.", nameof(definitions));
    }

    /// <summary>
    /// The standard kinds: <c>door</c> (hinged family), <c>window</c> (family with sill) and
    /// <c>opening</c> (empty wall opening). Block and layer names equal the kind name.
    /// </summary>
    /// <param name="doorHeightCm">Default door height (cm).</param>
    /// <param name="windowHeightCm">Default window height (cm).</param>
    /// <param name="windowSillCm">Default window sill height (cm).</param>
    /// <param name="emptyOpeningHeightCm">Default empty opening height (cm).</param>
    public static OpeningDefinitionRegistry CreateDefault(
        double doorHeightCm = DefaultDoorHeightCm,
        double windowHeightCm = DefaultWindowHeightCm,
        double windowSillCm = DefaultWindowSillCm,
        double emptyOpeningHeightCm = DefaultEmptyOpeningHeightCm) =>
        new(new[]
        {
            new OpeningDefinition("door", OpeningCategory.Door, OpeningCreationMode.HostedFamily,
                OpeningHandingMode.Hinged, new[] { "door" }, new[] { "door" },
                doorHeightCm, 0, usesSillHeight: false, idPrefix: "D"),
            new OpeningDefinition("window", OpeningCategory.Window, OpeningCreationMode.HostedFamily,
                OpeningHandingMode.None, new[] { "window" }, new[] { "window" },
                windowHeightCm, windowSillCm, usesSillHeight: true, idPrefix: "W"),
            new OpeningDefinition("opening", OpeningCategory.EmptyOpening, OpeningCreationMode.WallOpening,
                OpeningHandingMode.None, new[] { "opening" }, new[] { "opening" },
                emptyOpeningHeightCm, 0, usesSillHeight: true, idPrefix: "O")
        });

    /// <summary>
    /// Finds the definition of a marker: by block name, otherwise by layer.
    /// </summary>
    /// <param name="blockName">Block name (true name for dynamic blocks).</param>
    /// <param name="layer">Layer of the block reference.</param>
    /// <returns>The definition, or <c>null</c> when the block is not an opening marker.</returns>
    public OpeningDefinition? Find(string? blockName, string? layer) =>
        Definitions.FirstOrDefault(d => d.MatchesBlock(blockName))
        ?? Definitions.FirstOrDefault(d => d.MatchesLayer(layer));
}
