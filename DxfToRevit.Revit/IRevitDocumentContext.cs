using DxfToRevit.Core.Building;

namespace DxfToRevit.Revit;

/// <summary>
/// A Revit document session that can create walls and columns. Requests are converted to
/// create arguments first and then written to the host document.
/// </summary>
/// <remarks>
/// Implementations: <see cref="RevitApiDocumentContext"/> (real document) and
/// <see cref="NullRevitDocumentContext"/> (no document; every call fails).
/// </remarks>
public interface IRevitDocumentContext
{
    /// <summary>Whether a valid, writable document is bound.</summary>
    bool IsAvailable { get; }

    /// <summary>Creates a Basic Wall from the request.</summary>
    /// <param name="request">Wall request.</param>
    /// <returns>Success with the element id, or a failure message.</returns>
    WallCreationResult CreateBasicWall(WallCreationRequest request);

    /// <summary>Creates a column (family instance or DirectShape) from the request.</summary>
    /// <param name="request">Column request.</param>
    /// <returns>Success with the element id (and an optional note), or a failure message.</returns>
    ColumnCreationResult CreateColumn(ColumnCreationRequest request);
}
