namespace DxfToRevit.Core.Openings;

/// <summary>
/// Host-independent contract for creating openings in existing walls.
/// </summary>
/// <remarks>
/// Implementations: <see cref="DxfToRevit.Core.InMemory.InMemoryOpeningCreator"/> (tests) and
/// <c>DxfToRevit.Revit.RevitOpeningCreator</c> (Revit doors, windows and wall openings).
/// </remarks>
public interface IOpeningCreator
{
    /// <summary>Creates one opening in the given host wall.</summary>
    /// <param name="request">Opening request.</param>
    /// <param name="hostWallElementId">Host element id of the wall created for <see cref="OpeningCreationRequest.HostWallRequestId"/>.</param>
    /// <returns>Success with the element id, or a failure message.</returns>
    OpeningCreationResult CreateOpening(OpeningCreationRequest request, string hostWallElementId);
}
