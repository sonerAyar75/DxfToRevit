namespace DxfToRevit.Core.Building.InMemory;

/// <summary>
/// A column stored in an <see cref="InMemoryBuildingModel"/>.
/// </summary>
public sealed class InMemoryColumnRecord
{
    /// <summary>Host id assigned by the model (e.g. <c>C-0001</c>).</summary>
    public string HostElementId { get; }

    /// <summary>The request the column was created from.</summary>
    public ColumnCreationRequest Request { get; }

    /// <summary>Creates the record.</summary>
    /// <param name="hostElementId">Host id assigned by the model.</param>
    /// <param name="request">Originating request.</param>
    public InMemoryColumnRecord(string hostElementId, ColumnCreationRequest request)
    {
        HostElementId = hostElementId;
        Request = request;
    }
}
