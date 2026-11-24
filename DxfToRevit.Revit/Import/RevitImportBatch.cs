#if REVIT_API_AVAILABLE
using Autodesk.Revit.DB;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;

namespace DxfToRevit.Revit.Import;

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

        IReadOnlyDictionary<string, string> notes = _context.BeforeCommit();
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
#endif
