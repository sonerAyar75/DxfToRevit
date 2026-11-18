namespace DxfToRevit.Core.Building.InMemory;

/// <summary>
/// A wall stored in an <see cref="InMemoryBuildingModel"/>.
/// </summary>
public sealed class InMemoryWallRecord
{
    /// <summary>Host id assigned by the model (e.g. <c>W-0001</c>).</summary>
    public string HostElementId { get; }

    /// <summary>The request the wall was created from.</summary>
    public WallCreationRequest Request { get; }

    /// <summary>Creates the record.</summary>
    /// <param name="hostElementId">Host id assigned by the model.</param>
    /// <param name="request">Originating request.</param>
    public InMemoryWallRecord(string hostElementId, WallCreationRequest request)
    {
        HostElementId = hostElementId;
        Request = request;
    }
}
