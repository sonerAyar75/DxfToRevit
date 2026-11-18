using DxfToRevit.Core.Building;

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
}
