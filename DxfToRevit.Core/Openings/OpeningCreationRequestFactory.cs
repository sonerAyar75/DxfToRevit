using DxfToRevit.Core.Import;
using DxfToRevit.Core.Walls;

namespace DxfToRevit.Core.Openings;

/// <summary>
/// Converts interpreted openings into <see cref="OpeningCreationRequest"/>s: resolves the Revit type
/// from the type map, links each opening to the request id of its host wall and applies the
/// session's level.
/// </summary>
public sealed class OpeningCreationRequestFactory
{
    private readonly ImportSessionOptions _session;
    private readonly OpeningTypeMap _typeMap;

    /// <summary>Creates the factory.</summary>
    /// <param name="session">Import policy (base level); default options when <c>null</c>.</param>
    /// <param name="typeMap">Type code → Revit type map; empty when <c>null</c>.</param>
    public OpeningCreationRequestFactory(ImportSessionOptions? session = null, OpeningTypeMap? typeMap = null)
    {
        _session = session ?? new ImportSessionOptions();
        _typeMap = typeMap ?? OpeningTypeMap.Empty;
    }

    /// <summary>Creates one request per opening.</summary>
    /// <param name="openings">Openings produced by the pipeline.</param>
    /// <returns>Requests in the same order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="openings"/> is <c>null</c>.</exception>
    public IReadOnlyList<OpeningCreationRequest> FromOpeningModels(IEnumerable<OpeningModel> openings)
    {
        ArgumentNullException.ThrowIfNull(openings);

        // Openings sit on the base level; the wall's base offset does not move the sill.
        var placement = new ElementPlacement(levelName: _session.BaseLevelName);

        return openings.Select(o =>
        {
            OpeningDefinition def = o.Definition;
            string? typeKey = def.CreationMode == OpeningCreationMode.HostedFamily
                ? _typeMap.Resolve(def.Category, o.Attributes.TypeCode)
                : null;

            return new OpeningCreationRequest(
                id: o.Id,
                category: def.Category,
                creationMode: def.CreationMode,
                typeKey: typeKey,
                typeCode: o.Attributes.TypeCode,
                hostWallRequestId: WallCreationRequestFactory.WallIdForIndex(o.Placement.HostWallIndex),
                locationMm: o.Placement.CenterOnWallMm,
                wallDirection: o.Placement.WallDirection,
                widthMm: o.Placement.WidthMm,
                heightMm: o.Attributes.HeightMm,
                sillHeightMm: o.Attributes.SillHeightMm,
                handing: o.Handing,
                placement: placement,
                sourceHandle: o.Marker.Handle);
        }).ToList();
    }
}
