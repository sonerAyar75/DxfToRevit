using DxfToRevit.Core.Columns;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;

namespace DxfToRevit.Revit.Columns;

/// <summary>
/// Maps a <see cref="ColumnCreationRequest"/> (millimetres) to <see cref="ColumnCreateArgs"/> (feet).
/// Does not call the Revit API, so level, height, location and section inputs can be unit-tested.
/// </summary>
public sealed class ColumnCreateArgsMapper
{
    private readonly PlacementCreateArgsMapper _placementMapper;

    /// <summary>Creates the mapper with its own placement mapper.</summary>
    /// <param name="defaultHeightMm">Height used when a request has no height.</param>
    public ColumnCreateArgsMapper(double defaultHeightMm = 3000.0)
        : this(new PlacementCreateArgsMapper(defaultHeightMm))
    {
    }

    /// <summary>Creates the mapper with a shared placement mapper.</summary>
    /// <param name="placementMapper">Placement mapper.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="placementMapper"/> is <c>null</c>.</exception>
    public ColumnCreateArgsMapper(PlacementCreateArgsMapper placementMapper)
    {
        _placementMapper = placementMapper ?? throw new ArgumentNullException(nameof(placementMapper));
    }

    /// <summary>Converts one request.</summary>
    /// <param name="request">Column request.</param>
    /// <returns>Create arguments in feet.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException">The outline has fewer than three vertices.</exception>
    public ColumnCreateArgs Map(ColumnCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.OutlineMm.Count < 3)
            throw new ArgumentException("Column outline needs at least 3 vertices.", nameof(request));

        PlacementCreateArgs placement = _placementMapper.Map(request.Placement);

        double locX = LengthUnitConverter.MmToFeet(request.CentroidMm.X);
        double locY = LengthUnitConverter.MmToFeet(request.CentroidMm.Y);

        double? widthFt = request.Type.WidthMm is double w
            ? LengthUnitConverter.MmToFeet(w)
            : null;
        double? depthFt = request.Type.DepthMm is double d
            ? LengthUnitConverter.MmToFeet(d)
            : null;

        var outlineFt = request.OutlineMm
            .Select(p => (LengthUnitConverter.MmToFeet(p.X), LengthUnitConverter.MmToFeet(p.Y)))
            .ToList();

        return new ColumnCreateArgs(
            requestId: request.Id,
            locationXFeet: locX,
            locationYFeet: locY,
            placement: placement,
            familyTypeKey: request.Type.TypeKey,
            widthFeet: widthFt,
            depthFeet: depthFt,
            widthMm: request.Type.WidthMm,
            depthMm: request.Type.DepthMm,
            isStructural: request.Type.IsStructural,
            outlineFeet: outlineFt,
            sourceLayer: request.SourceLayer,
            sectionKind: request.Type.SectionKind,
            diameterMm: request.Type.DiameterMm,
            rotationRadians: request.RotationRadians);
    }

    /// <summary>Converts several requests.</summary>
    /// <param name="requests">Column requests.</param>
    /// <returns>Create arguments in the same order.</returns>
    public IReadOnlyList<ColumnCreateArgs> MapMany(IEnumerable<ColumnCreationRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        return requests.Select(Map).ToList();
    }
}
