#if REVIT_API_AVAILABLE

using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;

namespace DxfToRevit.Revit.Import;

/// <summary>Outcome of <see cref="RevitImportBatch.Commit"/>.</summary>
/// <param name="Committed">Whether the batch transaction was committed.</param>
/// <param name="Notes">Notes for created elements by request id (for example a door that cannot flip).</param>
/// <param name="RemovedElements">Elements Revit could not keep at commit, by element id, with the reason.</param>
/// <param name="Warnings">Warnings Revit raised at commit (suppressed in the UI), grouped; overlaps first.</param>
public sealed record RevitBatchResult(
    bool Committed,
    IReadOnlyDictionary<string, string> Notes,
    IReadOnlyDictionary<string, string> RemovedElements,
    IReadOnlyList<RevitWarningGroup> Warnings)
{
    /// <summary>Whether Revit reported overlapping or duplicate elements (for example the drawing imported twice).</summary>
    public bool HasOverlaps => Warnings.Any(w => w.Kind == RevitWarningKind.Overlap);

    /// <summary>Whether the element with this id was removed at commit, and why.</summary>
    /// <param name="elementId">Element id as returned in a creation result.</param>
    /// <param name="reason">Revit's failure text.</param>
    public bool WasRemoved(string? elementId, out string reason)
    {
        reason = string.Empty;
        if (elementId is null)
            return false;
        if (RemovedElements.TryGetValue(elementId, out string? text))
        {
            reason = text;
            return true;
        }

        return false;
    }
}
#endif
