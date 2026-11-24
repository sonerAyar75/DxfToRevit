namespace DxfToRevit.Core.Import;

/// <summary>
/// Vertical placement of a host element: base level, base offset and either a top level
/// (connected) or an explicit height (unconnected). Mirrors Revit's level constraints while
/// staying host-independent.
/// </summary>
public sealed class ElementPlacement
{
    /// <summary>Base level name; <c>null</c> lets the host choose its default (lowest) level.</summary>
    public string? LevelName { get; }

    /// <summary>
    /// Top level name. When set, the element is bound to this level and <see cref="HeightMm"/> is
    /// ignored by the host; <c>null</c> means "Unconnected".
    /// </summary>
    public string? TopLevelName { get; }

    /// <summary>Base offset from the base level, in millimetres.</summary>
    public double BaseOffsetMm { get; }

    /// <summary>
    /// Unconnected height in millimetres. <c>null</c> lets the host apply its default height.
    /// </summary>
    public double? HeightMm { get; }

    /// <summary>Creates a placement.</summary>
    /// <param name="levelName">Base level name, or <c>null</c> for the host default.</param>
    /// <param name="baseOffsetMm">Base offset in millimetres.</param>
    /// <param name="heightMm">Unconnected height in millimetres; must be positive when given.</param>
    /// <param name="topLevelName">Top level name; blank values are treated as "Unconnected".</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="heightMm"/> is zero or negative.</exception>
    public ElementPlacement(
        string? levelName = null,
        double baseOffsetMm = 0,
        double? heightMm = null,
        string? topLevelName = null)
    {
        if (heightMm is <= 0)
            throw new ArgumentOutOfRangeException(nameof(heightMm));

        if (string.IsNullOrWhiteSpace(topLevelName))
            topLevelName = null;

        LevelName = levelName;
        TopLevelName = topLevelName;
        BaseOffsetMm = baseOffsetMm;
        HeightMm = heightMm;
    }
}
