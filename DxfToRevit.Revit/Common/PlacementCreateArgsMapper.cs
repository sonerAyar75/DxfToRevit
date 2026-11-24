using DxfToRevit.Core.Import;

namespace DxfToRevit.Revit.Common;

/// <summary>
/// Maps an <see cref="ElementPlacement"/> (millimetres) to <see cref="PlacementCreateArgs"/> (feet).
/// Shared by the wall and column mappers.
/// </summary>
public sealed class PlacementCreateArgsMapper
{
    /// <summary>Height in millimetres used when the placement has none.</summary>
    public double DefaultHeightMm { get; }

    /// <summary>Creates the mapper.</summary>
    /// <param name="defaultHeightMm">Default height in millimetres; must be positive.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="defaultHeightMm"/> is not positive.</exception>
    public PlacementCreateArgsMapper(double defaultHeightMm = 3000.0)
    {
        if (defaultHeightMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(defaultHeightMm));
        DefaultHeightMm = defaultHeightMm;
    }

    /// <summary>Converts a placement to feet, applying the default height when needed.</summary>
    /// <param name="placement">Placement in millimetres.</param>
    /// <returns>Placement arguments in feet.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="placement"/> is <c>null</c>.</exception>
    public PlacementCreateArgs Map(ElementPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);

        double heightMm = placement.HeightMm ?? DefaultHeightMm;
        double baseMm = placement.BaseOffsetMm;

        return new PlacementCreateArgs(
            levelName: placement.LevelName,
            heightFeet: LengthUnitConverter.MmToFeet(heightMm),
            baseOffsetFeet: LengthUnitConverter.MmToFeet(baseMm),
            heightMm: heightMm,
            baseOffsetMm: baseMm,
            topLevelName: placement.TopLevelName);
    }
}
