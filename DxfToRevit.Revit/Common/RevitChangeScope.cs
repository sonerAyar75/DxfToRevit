#if REVIT_API_AVAILABLE
using Autodesk.Revit.DB;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;

namespace DxfToRevit.Revit.Common;

/// <summary>
/// One undoable change to a project document. It starts its own <see cref="Autodesk.Revit.DB.Transaction"/> when no
/// transaction is open, and a <see cref="Autodesk.Revit.DB.SubTransaction"/> when the caller already runs a batch
/// transaction (the document is modifiable). Code that creates one element therefore works both on
/// its own and inside a batch, where each element still rolls back on its own but the costly
/// commit (regeneration, view redraw) happens once for the whole batch.
/// </summary>
internal sealed class RevitChangeScope : IDisposable
{
    private readonly Transaction? _transaction;
    private readonly SubTransaction? _subTransaction;

    private RevitChangeScope(Transaction transaction) => _transaction = transaction;

    private RevitChangeScope(SubTransaction subTransaction) => _subTransaction = subTransaction;

    /// <summary>Whether the scope is a sub-transaction of a batch.</summary>
    public bool IsInBatch => _subTransaction is not null;

    /// <summary>Starts a scope on the document.</summary>
    /// <param name="document">Document to change.</param>
    /// <param name="name">Transaction name (used when no batch is open).</param>
    public static RevitChangeScope Start(Document document, string name)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.IsModifiable)
        {
            var sub = new SubTransaction(document);
            sub.Start();
            return new RevitChangeScope(sub);
        }

        var transaction = new Transaction(document, name);
        transaction.Start();
        return new RevitChangeScope(transaction);
    }

    /// <summary>Commits the change.</summary>
    public void Commit()
    {
        if (_subTransaction is not null)
            _subTransaction.Commit();
        else
            _transaction!.Commit();
    }

    /// <summary>Rolls the change back.</summary>
    public void RollBack()
    {
        if (_subTransaction is not null)
            _subTransaction.RollBack();
        else
            _transaction!.RollBack();
    }

    /// <summary>Rolls back an uncommitted change and releases the transaction.</summary>
    public void Dispose()
    {
        _subTransaction?.Dispose();
        _transaction?.Dispose();
    }
}
#endif
