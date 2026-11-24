#if REVIT_API_AVAILABLE
using DxfToRevit.Revit.Common;

namespace DxfToRevit.Revit.Import;

/// <summary>
/// A writer that defers part of its work to the end of an import batch, so it is done once for
/// all elements instead of once per element (for example flipping doors and windows, which needs
/// a model regeneration). <see cref="RevitApiDocumentContext"/> notifies its participants when a
/// batch starts and ends, and before it is committed.
/// </summary>
internal interface IRevitBatchParticipant
{
    /// <summary>A batch has started; deferred work is collected from now on.</summary>
    void OnBatchStarted();

    /// <summary>Applies the deferred work before the commit.</summary>
    /// <returns>Notes for created elements, by request id.</returns>
    IReadOnlyDictionary<string, string> BeforeCommit();

    /// <summary>The batch has ended (committed or rolled back); deferred work is dropped.</summary>
    void OnBatchEnded();
}
#endif
