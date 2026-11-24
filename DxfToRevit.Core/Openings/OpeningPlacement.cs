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

/// <summary>
/// Orientation of a door or window, relative to the host wall direction, as read from the one
/// circle of its marker. What the orientation means for a double, sliding or multi-panel leaf is
/// up to the Revit family mapped to the <c>TYPE</c> code.
/// </summary>
/// <param name="HingeEnd">
/// End of the opening on which the circle lies; <c>null</c> when the circle is in the middle of the
/// opening (left/right is then left as Revit places it).
/// </param>
/// <param name="SwingSide">Wall face to which the leaf swings (the face holding the circle).</param>
public sealed record OpeningHanding(HingeEnd? HingeEnd, SwingSide SwingSide);

/// <summary>
/// An interpreted opening, ready to be turned into a creation request: its kind, marker, place on
/// the host wall, heights, type code and (for doors and windows) orientation.
/// </summary>
public sealed class OpeningModel
{
    /// <summary>Opening id: kind prefix plus number, e.g. <c>D001</c>, <c>W002</c>, <c>O001</c>.</summary>
    public string Id { get; }

    /// <summary>Kind of opening.</summary>
    public OpeningDefinition Definition => Marker.Definition;

    /// <summary>Source marker.</summary>
    public OpeningMarker Marker { get; }

    /// <summary>Place on the host wall.</summary>
    public OpeningPlacement Placement { get; }

    /// <summary>Type code and heights.</summary>
    public OpeningAttributes Attributes { get; }

    /// <summary>
    /// Orientation for doors and windows; <c>null</c> for empty openings and for markers without a
    /// usable circle (the family is then left as Revit places it).
    /// </summary>
    public OpeningHanding? Handing { get; }

    /// <summary>Creates an opening model.</summary>
    /// <param name="id">Opening id.</param>
    /// <param name="marker">Source marker.</param>
    /// <param name="placement">Place on the host wall.</param>
    /// <param name="attributes">Type code and heights.</param>
    /// <param name="handing">Orientation, for doors and windows.</param>
    /// <exception cref="ArgumentException"><paramref name="id"/> is empty.</exception>
    /// <exception cref="ArgumentNullException">A reference argument is <c>null</c>.</exception>
    public OpeningModel(
        string id,
        OpeningMarker marker,
        OpeningPlacement placement,
        OpeningAttributes attributes,
        OpeningHanding? handing)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        Id = id;
        Marker = marker ?? throw new ArgumentNullException(nameof(marker));
        Placement = placement ?? throw new ArgumentNullException(nameof(placement));
        Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
        Handing = handing;
    }
}
