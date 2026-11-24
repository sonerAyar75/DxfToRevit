#if REVIT_API_AVAILABLE
using Autodesk.Revit.DB;

namespace DxfToRevit.Revit;

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

/// <summary>
/// Unit of work for an import: a single Revit transaction that collects all element changes and
/// commits them once. Elements are written in sub-transactions (<see cref="RevitChangeScope"/>),
/// door flips are applied together before the commit, warnings are suppressed and elements that
/// cause errors are deleted by <see cref="RevitBatchFailureHandler"/> instead of rolling back the
/// whole batch. Disposing without <see cref="Commit"/> rolls everything back.
/// </summary>
public sealed class RevitImportBatch : IDisposable
{
    private readonly RevitApiDocumentContext _context;
    private readonly Document _document;
    private readonly Transaction _transaction;
    private readonly RevitBatchFailureHandler _failures = new();
    private bool _finished;

    /// <summary>Starts the batch transaction (use <see cref="RevitApiDocumentContext.BeginBatch"/>).</summary>
    internal RevitImportBatch(RevitApiDocumentContext context, Document document, string name)
    {
        _context = context;
        _document = document;
        _transaction = new Transaction(document, name);

        FailureHandlingOptions options = _transaction.GetFailureHandlingOptions();
        options.SetFailuresPreprocessor(_failures);
        options.SetClearAfterRollback(true);
        options.SetForcedModalHandling(false);
        _transaction.SetFailureHandlingOptions(options);
        _transaction.Start();
    }

    /// <summary>
    /// Regenerates the model between phases (for example after the walls, so doors and wall
    /// openings can be hosted). One regeneration per phase, instead of one per element.
    /// </summary>
    public void Regenerate() => _document.Regenerate();

    /// <summary>Applies the pending door flips and commits all changes in one step.</summary>
    /// <exception cref="System.InvalidOperationException">The batch was already committed or rolled back.</exception>
    public RevitBatchResult Commit()
    {
        if (_finished)
            throw new InvalidOperationException("The batch has already finished.");

        IReadOnlyDictionary<string, string> notes = _context.ApplyPendingHandings();
        TransactionStatus status = _transaction.Commit();
        _finished = true;
        _context.EndBatch(this);

        return new RevitBatchResult(
            status == TransactionStatus.Committed, notes, _failures.RemovedElements, _failures.Warnings.Groups);
    }

    /// <summary>Rolls back an uncommitted batch.</summary>
    public void Dispose()
    {
        if (!_finished && _transaction.HasStarted() && !_transaction.HasEnded())
            _transaction.RollBack();

        _transaction.Dispose();
        _finished = true;
        _context.EndBatch(this);
    }
}

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
