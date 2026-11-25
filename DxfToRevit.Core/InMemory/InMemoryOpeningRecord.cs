using DxfToRevit.Core.Openings;

namespace DxfToRevit.Core.InMemory;

/// <summary>An opening stored in an <see cref="InMemoryBuildingModel"/>.</summary>
/// <param name="HostElementId">Host id assigned by the model (e.g. <c>O-0001</c>).</param>
/// <param name="HostWallElementId">Host id of the wall the opening is in.</param>
/// <param name="Request">The request the opening was created from.</param>
public sealed record InMemoryOpeningRecord(string HostElementId, string HostWallElementId, OpeningCreationRequest Request);
