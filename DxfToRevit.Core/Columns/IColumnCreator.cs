namespace DxfToRevit.Core.Columns;

/// <summary>
/// Host-independent contract for creating columns.
/// </summary>
/// <remarks>
/// <para><b>Implementations:</b></para>
/// <list type="bullet">
/// <item><description><see cref="DxfToRevit.Core.InMemory.InMemoryColumnCreator"/> — in-memory host for tests and simulation.</description></item>
/// <item><description><c>DxfToRevit.Revit.RevitColumnCreator</c> — creates Revit column family instances or DirectShapes.</description></item>
/// </list>
/// <para>
/// Data flow: closed polyline or circle on a COLUMN* layer → <see cref="ColumnModel"/> →
/// <see cref="ColumnCreationRequestFactory"/> → <see cref="ColumnCreationRequest"/> → this interface.
/// </para>
/// </remarks>
public interface IColumnCreator
{
    /// <summary>Creates a single column.</summary>
    /// <param name="request">Section outline, type (dimensions/kind) and placement.</param>
    /// <returns>Success with the host element id, or a failure message.</returns>
    ColumnCreationResult CreateColumn(ColumnCreationRequest request);

    /// <summary>Creates several columns in order.</summary>
    /// <param name="requests">Column requests.</param>
    /// <returns>One result per request, in the same order.</returns>
    IReadOnlyList<ColumnCreationResult> CreateColumns(IEnumerable<ColumnCreationRequest> requests);
}
