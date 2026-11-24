namespace DxfToRevit.Revit.Common;

/// <summary>
/// Placement inputs for creating a Revit element, in feet (testable without the API).
/// Level names stay strings; the host resolves them to <c>Level</c> elements.
/// </summary>
public sealed class PlacementCreateArgs
{
    /// <summary>Base level name; <c>null</c> selects the host default (lowest) level.</summary>
    public string? LevelName { get; }

    /// <summary>Top level name; <c>null</c> means "Unconnected".</summary>
    public string? TopLevelName { get; }

    /// <summary>Unconnected height in feet (also the DirectShape height when no top level is set).</summary>
    public double HeightFeet { get; }

    /// <summary>Base offset in feet.</summary>
    public double BaseOffsetFeet { get; }

    /// <summary>Source height in millimetres (for messages and traceability).</summary>
    public double HeightMm { get; }

    /// <summary>Source base offset in millimetres.</summary>
    public double BaseOffsetMm { get; }

    /// <summary>Creates the placement arguments.</summary>
    /// <param name="levelName">Base level name.</param>
    /// <param name="heightFeet">Height in feet; must be positive.</param>
    /// <param name="baseOffsetFeet">Base offset in feet.</param>
    /// <param name="heightMm">Height in millimetres; must be positive.</param>
    /// <param name="baseOffsetMm">Base offset in millimetres.</param>
    /// <param name="topLevelName">Top level name; blank means "Unconnected".</param>
    /// <exception cref="System.ArgumentOutOfRangeException">A height is not positive.</exception>
    public PlacementCreateArgs(
        string? levelName,
        double heightFeet,
        double baseOffsetFeet,
        double heightMm,
        double baseOffsetMm,
        string? topLevelName = null)
    {
        if (heightFeet <= 0)
            throw new ArgumentOutOfRangeException(nameof(heightFeet));
        if (heightMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(heightMm));

        LevelName = levelName;
        TopLevelName = string.IsNullOrWhiteSpace(topLevelName)
            ? null
            : topLevelName;

        HeightFeet = heightFeet;
        BaseOffsetFeet = baseOffsetFeet;
        HeightMm = heightMm;
        BaseOffsetMm = baseOffsetMm;
    }
}
