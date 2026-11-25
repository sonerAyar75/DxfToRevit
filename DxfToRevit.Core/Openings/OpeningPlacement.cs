using netDxf;

namespace DxfToRevit.Core.Openings;

/// <summary>
/// Where an opening sits on its host wall: the wall footprint it belongs to, the stretch it
/// occupies along the wall centerline and the wall thickness.
/// </summary>
public sealed class OpeningPlacement
{
    /// <summary>Index of the host wall in the pipeline's wall footprint list.</summary>
    public int HostWallIndex { get; }

    /// <summary>Opening centre projected onto the wall centerline, in millimetres.</summary>
    public Vector2 CenterOnWallMm { get; }

    /// <summary>Unit direction of the wall centerline (start → end).</summary>
    public Vector2 WallDirection { get; }

    /// <summary>Distance of the opening start from the wall centerline start, in millimetres.</summary>
    public double StartOffsetMm { get; }

    /// <summary>Distance of the opening end from the wall centerline start, in millimetres.</summary>
    public double EndOffsetMm { get; }

    /// <summary>Opening width along the wall, in millimetres.</summary>
    public double WidthMm => EndOffsetMm - StartOffsetMm;

    /// <summary>Wall thickness in millimetres (the opening depth).</summary>
    public double ThicknessMm { get; }

    /// <summary>Marker rectangle depth across the wall, in millimetres (only checked, not used).</summary>
    public double MarkerDepthMm { get; }

    /// <summary>Creates a placement.</summary>
    /// <param name="hostWallIndex">Host wall index; must not be negative.</param>
    /// <param name="centerOnWallMm">Centre on the wall centerline.</param>
    /// <param name="wallDirection">Unit wall direction.</param>
    /// <param name="startOffsetMm">Start offset along the wall.</param>
    /// <param name="endOffsetMm">End offset along the wall; must be greater than the start.</param>
    /// <param name="thicknessMm">Wall thickness; must be positive.</param>
    /// <param name="markerDepthMm">Marker depth.</param>
    /// <exception cref="ArgumentOutOfRangeException">A value is out of range.</exception>
    public OpeningPlacement(
        int hostWallIndex,
        Vector2 centerOnWallMm,
        Vector2 wallDirection,
        double startOffsetMm,
        double endOffsetMm,
        double thicknessMm,
        double markerDepthMm)
    {
        if (hostWallIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(hostWallIndex));
        if (endOffsetMm <= startOffsetMm)
            throw new ArgumentOutOfRangeException(nameof(endOffsetMm));
        if (thicknessMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(thicknessMm));

        HostWallIndex = hostWallIndex;
        CenterOnWallMm = centerOnWallMm;
        WallDirection = wallDirection;
        StartOffsetMm = startOffsetMm;
        EndOffsetMm = endOffsetMm;
        ThicknessMm = thicknessMm;
        MarkerDepthMm = markerDepthMm;
    }
}
