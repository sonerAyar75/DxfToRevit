#if REVIT_API_AVAILABLE
using Autodesk.Revit.DB;

namespace DxfToRevit.Revit;

/// <summary>
/// Failure preprocessor of an import batch: records and deletes warnings (so Revit shows no dialog
/// for each element; the summary lists them, overlaps first) and resolves errors by their current
/// (default) resolution, or else by deleting the failing elements, recording which elements were
/// removed and why.
/// </summary>
internal sealed class RevitBatchFailureHandler : IFailuresPreprocessor
{
    /// <summary>Warnings that mean new elements overlap or duplicate existing ones.</summary>
    private static readonly FailureDefinitionId[] OverlapWarnings =
    {
        BuiltInFailures.OverlapFailures.WallsOverlap,
        BuiltInFailures.OverlapFailures.DuplicateInstances,
        BuiltInFailures.OverlapFailures.FloorsOverlap,
        BuiltInFailures.OverlapFailures.OverlappingElementsTest
    };

    private readonly Dictionary<string, string> _removed = new(StringComparer.Ordinal);

    /// <summary>Elements deleted to resolve errors, by element id.</summary>
    public IReadOnlyDictionary<string, string> RemovedElements => _removed;

    /// <summary>Warnings raised at commit, before they were deleted.</summary>
    public RevitWarningLog Warnings { get; } = new();

    /// <inheritdoc />
    public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
    {
        bool resolved = false;
        foreach (FailureMessageAccessor failure in failuresAccessor.GetFailureMessages())
        {
            if (failure.GetSeverity() == FailureSeverity.Warning)
            {
                FailureDefinitionId id = failure.GetFailureDefinitionId();
                RevitWarningKind kind = OverlapWarnings.Any(o => o.Guid == id.Guid)
                    ? RevitWarningKind.Overlap
                    : RevitWarningKind.Other;
                Warnings.Add(kind, failure.GetDescriptionText(),
                    failure.GetFailingElementIds().Select(e => e.Value.ToString()));
                failuresAccessor.DeleteWarning(failure);
                continue;
            }

            if (failure.HasResolutions() &&
                failuresAccessor.IsFailureResolutionPermitted(failure, failure.GetCurrentResolutionType()))
            {
                failuresAccessor.ResolveFailure(failure);
                resolved = true;
                continue;
            }

            ICollection<ElementId> failing = failure.GetFailingElementIds();
            if (failing.Count == 0)
                continue;

            string text = failure.GetDescriptionText();
            foreach (ElementId id in failing)
                _removed[id.Value.ToString()] = text;

            failuresAccessor.DeleteElements(failing.ToList());
            resolved = true;
        }

        return resolved ? FailureProcessingResult.ProceedWithCommit : FailureProcessingResult.Continue;
    }
}
#endif
