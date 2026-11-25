namespace DxfToRevit.Core.Openings;

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
