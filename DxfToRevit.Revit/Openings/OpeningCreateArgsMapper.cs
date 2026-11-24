using DxfToRevit.Core.Openings;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;

namespace DxfToRevit.Revit.Openings;

/// <summary>Maps an <see cref="OpeningCreationRequest"/> (millimetres) to <see cref="OpeningCreateArgs"/> (feet).</summary>
public sealed class OpeningCreateArgsMapper
{
    /// <summary>Converts one request.</summary>
    /// <param name="request">Opening request.</param>
    /// <returns>Create arguments in feet.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException">The wall direction is zero.</exception>
    public OpeningCreateArgs Map(OpeningCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        double length = Math.Sqrt(request.WallDirection.X * request.WallDirection.X +
                                  request.WallDirection.Y * request.WallDirection.Y);
        if (length < 1e-9)
            throw new ArgumentException("Wall direction cannot be zero.", nameof(request));

        return new OpeningCreateArgs(
            requestId: request.Id,
            category: request.Category,
            creationMode: request.CreationMode,
            typeKey: request.TypeKey,
            locationXFeet: LengthUnitConverter.MmToFeet(request.LocationMm.X),
            locationYFeet: LengthUnitConverter.MmToFeet(request.LocationMm.Y),
            directionX: request.WallDirection.X / length,
            directionY: request.WallDirection.Y / length,
            widthFeet: LengthUnitConverter.MmToFeet(request.WidthMm),
            heightFeet: LengthUnitConverter.MmToFeet(request.HeightMm),
            sillHeightFeet: LengthUnitConverter.MmToFeet(request.SillHeightMm),
            widthMm: request.WidthMm,
            heightMm: request.HeightMm,
            levelName: request.Placement.LevelName,
            handing: request.Handing);
    }
}
