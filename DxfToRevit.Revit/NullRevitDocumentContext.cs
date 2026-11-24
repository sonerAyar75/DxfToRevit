using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Openings;
using DxfToRevit.Core.Walls;

namespace DxfToRevit.Revit;

/// <summary>
/// Document context used when no Revit document is bound: it is never available and every
/// create call fails with an explanatory message.
/// </summary>
public sealed class NullRevitDocumentContext : IRevitDocumentContext
{
    /// <summary>Shared instance.</summary>
    public static NullRevitDocumentContext Instance { get; } = new();

    /// <inheritdoc />
    public bool IsAvailable => false;

    /// <inheritdoc />
    public WallCreationResult CreateBasicWall(WallCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return WallCreationResult.Fail(
            request.Id,
            "No Revit Document is bound (NullRevitDocumentContext).");
    }

    /// <inheritdoc />
    public ColumnCreationResult CreateColumn(ColumnCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ColumnCreationResult.Fail(
            request.Id,
            "No Revit Document is bound (NullRevitDocumentContext).");
    }

    /// <inheritdoc />
    public OpeningCreationResult CreateOpening(OpeningCreationRequest request, string hostWallElementId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return OpeningCreationResult.Fail(
            request.Id,
            "No Revit Document is bound (NullRevitDocumentContext).");
    }
}
