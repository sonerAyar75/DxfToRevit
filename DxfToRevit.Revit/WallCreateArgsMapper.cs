using DxfToRevit.Core.Walls;

namespace DxfToRevit.Revit;

/// <summary>
/// Maps a <see cref="WallCreationRequest"/> (millimetres) to <see cref="WallCreateArgs"/>
/// (feet plus Revit level and type names).
/// </summary>
public sealed class WallCreateArgsMapper
{
    private readonly PlacementCreateArgsMapper _placementMapper;

    /// <summary>Height in millimetres used when a request has none.</summary>
    public double DefaultHeightMm => _placementMapper.DefaultHeightMm;

    /// <summary>Creates the mapper with its own placement mapper.</summary>
    /// <param name="defaultHeightMm">Default height in millimetres.</param>
    public WallCreateArgsMapper(double defaultHeightMm = 3000.0)
        : this(new PlacementCreateArgsMapper(defaultHeightMm))
    {
    }

    /// <summary>Creates the mapper with a shared placement mapper.</summary>
    /// <param name="placementMapper">Placement mapper.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="placementMapper"/> is <c>null</c>.</exception>
    public WallCreateArgsMapper(PlacementCreateArgsMapper placementMapper)
    {
        _placementMapper = placementMapper ?? throw new ArgumentNullException(nameof(placementMapper));
    }

    /// <summary>Converts one request.</summary>
    /// <param name="request">Wall request.</param>
    /// <returns>Create arguments in feet.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException">The centerline is degenerate.</exception>
    public WallCreateArgs Map(WallCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CenterLine.IsDegenerate || request.LengthMm <= 0)
            throw new ArgumentException("Centerline must not be degenerate.", nameof(request));

        PlacementCreateArgs placement = _placementMapper.Map(request.Placement);

        double startX = LengthUnitConverter.MmToFeet(request.StartMm.X);
        double startY = LengthUnitConverter.MmToFeet(request.StartMm.Y);
        double endX = LengthUnitConverter.MmToFeet(request.EndMm.X);
        double endY = LengthUnitConverter.MmToFeet(request.EndMm.Y);
        double lengthFt = LengthUnitConverter.MmToFeet(request.LengthMm);

        return new WallCreateArgs(
            requestId: request.Id,
            startXFeet: startX,
            startYFeet: startY,
            endXFeet: endX,
            endYFeet: endY,
            heightFeet: placement.HeightFeet,
            baseOffsetFeet: placement.BaseOffsetFeet,
            levelName: placement.LevelName,
            wallTypeKey: request.WallTypeKey,
            isStructural: request.Type.IsStructural,
            thicknessMm: request.ThicknessMm,
            lengthFeet: lengthFt,
            topLevelName: placement.TopLevelName);
    }

    /// <summary>Converts several requests.</summary>
    /// <param name="requests">Wall requests.</param>
    /// <returns>Create arguments in the same order.</returns>
    public IReadOnlyList<WallCreateArgs> MapMany(IEnumerable<WallCreationRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        return requests.Select(Map).ToList();
    }
}
