namespace DxfToRevit.Core.Building;

/// <summary>
/// Host-independent contract for creating walls.
/// </summary>
/// <remarks>
/// <para><b>Implementations:</b></para>
/// <list type="bullet">
/// <item><description><see cref="InMemory.InMemoryWallCreator"/> — in-memory host for tests and simulation.</description></item>
/// <item><description><c>DxfToRevit.Revit.RevitWallCreator</c> — Revit <c>Wall.Create</c> (Basic Wall) on an open document.</description></item>
/// </list>
/// <para>
/// Request model: <see cref="WallCreationRequest"/> (centerline in millimetres, <see cref="WallTypeProperties"/>
/// and <see cref="ElementPlacement"/>). Result: <see cref="WallCreationResult"/>.
/// </para>
/// </remarks>
public interface IWallCreator
{
    /// <summary>Creates a single wall (a Basic Wall or the host equivalent).</summary>
    /// <param name="request">Centerline, thickness, type and placement.</param>
    /// <returns>Success with the host element id, or a failure message.</returns>
    WallCreationResult CreateWall(WallCreationRequest request);

    /// <summary>Creates several walls in order.</summary>
    /// <param name="requests">Wall requests.</param>
    /// <returns>One result per request, in the same order.</returns>
    IReadOnlyList<WallCreationResult> CreateWalls(IEnumerable<WallCreationRequest> requests);
}
