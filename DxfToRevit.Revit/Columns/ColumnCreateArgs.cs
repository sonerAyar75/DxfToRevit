using DxfToRevit.Core.Columns;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;

namespace DxfToRevit.Revit.Columns;

/// <summary>
/// Inputs for creating a Revit column (family instance or DirectShape), expressed in Revit
/// internal units (feet). Produced without the Revit API so it can be unit-tested; the host
/// resolves <see cref="FamilyTypeKey"/> to a <c>FamilySymbol</c>.
/// </summary>
public sealed class ColumnCreateArgs
{
    /// <summary>Identifier of the originating request.</summary>
    public string RequestId { get; }

    /// <summary>Placement point X in feet (the section centroid).</summary>
    public double LocationXFeet { get; }

    /// <summary>Placement point Y in feet.</summary>
    public double LocationYFeet { get; }

    /// <summary>Level, height and offset placement.</summary>
    public PlacementCreateArgs Placement { get; }

    /// <summary>Family/type key (a name, a <c>Family::Type</c> pair or a <see cref="ColumnTypeKeys"/> value).</summary>
    public string FamilyTypeKey { get; }

    /// <summary>Section width in feet; <c>null</c> when unknown.</summary>
    public double? WidthFeet { get; }

    /// <summary>Section depth in feet; <c>null</c> when unknown.</summary>
    public double? DepthFeet { get; }

    /// <summary>Section width in millimetres (kept for type naming and messages).</summary>
    public double? WidthMm { get; }

    /// <summary>Section depth in millimetres.</summary>
    public double? DepthMm { get; }

    /// <summary>Whether the column is created as structural.</summary>
    public bool IsStructural { get; }

    /// <summary>Section vertices in feet (world coordinates), used for freeform geometry.</summary>
    public IReadOnlyList<(double X, double Y)> OutlineFeet { get; }

    /// <summary>Source DXF layer.</summary>
    public string? SourceLayer { get; }

    /// <summary>Section classification.</summary>
    public ColumnSectionKind SectionKind { get; }

    /// <summary>Diameter in millimetres for circular columns; <c>null</c> otherwise.</summary>
    public double? DiameterMm { get; }

    /// <summary>Plan rotation in radians, counter-clockwise. For rectangular columns the width runs along this direction.</summary>
    public double RotationRadians { get; }

    /// <summary>Creates the arguments.</summary>
    /// <param name="requestId">Identifier of the originating request.</param>
    /// <param name="locationXFeet">Placement X in feet.</param>
    /// <param name="locationYFeet">Placement Y in feet.</param>
    /// <param name="placement">Level and height placement.</param>
    /// <param name="familyTypeKey">Family/type key.</param>
    /// <param name="widthFeet">Section width in feet.</param>
    /// <param name="depthFeet">Section depth in feet.</param>
    /// <param name="widthMm">Section width in millimetres.</param>
    /// <param name="depthMm">Section depth in millimetres.</param>
    /// <param name="isStructural">Whether the column is structural.</param>
    /// <param name="outlineFeet">Section vertices in feet.</param>
    /// <param name="sourceLayer">Source DXF layer.</param>
    /// <param name="sectionKind">Section classification.</param>
    /// <param name="diameterMm">Diameter in millimetres for circular columns.</param>
    /// <param name="rotationRadians">Plan rotation in radians.</param>
    /// <exception cref="System.ArgumentException">The request id or type key is empty.</exception>
    /// <exception cref="System.ArgumentNullException">The placement or outline is <c>null</c>.</exception>
    public ColumnCreateArgs(
        string requestId,
        double locationXFeet,
        double locationYFeet,
        PlacementCreateArgs placement,
        string familyTypeKey,
        double? widthFeet,
        double? depthFeet,
        double? widthMm,
        double? depthMm,
        bool isStructural,
        IReadOnlyList<(double X, double Y)> outlineFeet,
        string? sourceLayer,
        ColumnSectionKind sectionKind = ColumnSectionKind.Rectangular,
        double? diameterMm = null,
        double rotationRadians = 0)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request id cannot be empty.", nameof(requestId));
        if (string.IsNullOrWhiteSpace(familyTypeKey))
            throw new ArgumentException("Family type key cannot be empty.", nameof(familyTypeKey));
        ArgumentNullException.ThrowIfNull(placement);
        ArgumentNullException.ThrowIfNull(outlineFeet);

        RequestId = requestId;
        LocationXFeet = locationXFeet;
        LocationYFeet = locationYFeet;
        Placement = placement;
        FamilyTypeKey = familyTypeKey;
        WidthFeet = widthFeet;
        DepthFeet = depthFeet;
        WidthMm = widthMm;
        DepthMm = depthMm;
        IsStructural = isStructural;
        OutlineFeet = outlineFeet.ToList().AsReadOnly();
        SourceLayer = sourceLayer;
        SectionKind = sectionKind;
        DiameterMm = diameterMm;
        RotationRadians = rotationRadians;
    }
}
