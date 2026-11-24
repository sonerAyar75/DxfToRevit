using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Openings;
using DxfToRevit.Core.Walls;

namespace DxfToRevit.Revit.Common;

/// <summary>
/// A Revit document session that can create walls, columns and openings. Requests are converted to
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

    /// <summary>Creates a door, window or wall opening in an existing wall.</summary>
    /// <param name="request">Opening request.</param>
    /// <param name="hostWallElementId">Element id of the host wall (as returned by <see cref="CreateBasicWall"/>).</param>
    /// <returns>Success with the element id (and an optional note), or a failure message.</returns>
    OpeningCreationResult CreateOpening(OpeningCreationRequest request, string hostWallElementId);
}
