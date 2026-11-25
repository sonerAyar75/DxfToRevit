using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Openings;
using DxfToRevit.Core.Walls;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;
#if REVIT_API_AVAILABLE
using DxfToRevit.Revit.Import;
#endif

namespace DxfToRevit.Revit.Common;

/// <summary>
/// <see cref="IRevitDocumentContext"/> bound to a real Revit document: a facade over one writer per
/// kind of element (<c>RevitWallWriter</c>, <c>RevitColumnWriter</c>, <c>RevitOpeningWriter</c>),
/// which share the level resolver of the document. It also runs the import batch
/// (<c>BeginBatch</c>) and lets writers defer work to the end of it.
/// </summary>
/// <remarks>
/// Without the <c>REVIT_API_AVAILABLE</c> symbol the class compiles as a stub that is never available
/// and reports the planned values in its failure messages.
/// </remarks>
public sealed class RevitApiDocumentContext : IRevitDocumentContext
{
    private readonly RevitElementCreatePlanner _planner = new();
    private readonly OpeningCreateArgsMapper _openingMapper = new();

#if REVIT_API_AVAILABLE
    private readonly Autodesk.Revit.DB.Document _document;
    private readonly RevitWallWriter _walls;
    private readonly RevitColumnWriter _columns;
    private readonly RevitOpeningWriter _openings;
    private readonly IReadOnlyList<IRevitBatchParticipant> _batchParticipants;
    private RevitImportBatch? _batch;

    /// <summary>Binds the context to a project document.</summary>
    /// <param name="document">Open Revit project document.</param>
    /// <param name="orientationPlanner">Door and window flip conventions; the standard conventions when <c>null</c>.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public RevitApiDocumentContext(Autodesk.Revit.DB.Document document, OpeningOrientationPlanner? orientationPlanner = null)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));

        var levels = new RevitLevelResolver(document);
        _walls = new RevitWallWriter(document, _planner, levels, new RevitWallTypeResolver(document));
        _columns = new RevitColumnWriter(document, _planner, levels, new RevitColumnTypeResolver(document),
            new RevitColumnSolidWriter(document, levels));
        _openings = new RevitOpeningWriter(document, _openingMapper, levels, new RevitOpeningTypeResolver(document),
            orientationPlanner ?? new OpeningOrientationPlanner());
        _batchParticipants = new IRevitBatchParticipant[] { _openings };
    }

    /// <summary>
    /// Starts a batch: one transaction for the whole import in which every element is created in its
    /// own sub-transaction (so a failing element still rolls back alone), Revit warnings are
    /// suppressed and failing elements are removed at commit instead of failing the batch.
    /// The document regenerates and redraws once, at <see cref="RevitImportBatch.Commit"/>.
    /// </summary>
    /// <param name="name">Transaction name shown in the undo list.</param>
    /// <exception cref="System.InvalidOperationException">A batch or another transaction is already open.</exception>
    public RevitImportBatch BeginBatch(string name)
    {
        if (_batch is not null || _document.IsModifiable)
            throw new InvalidOperationException("A transaction is already open on the document.");

        foreach (IRevitBatchParticipant participant in _batchParticipants)
            participant.OnBatchStarted();
        _batch = new RevitImportBatch(this, _document, name);
        return _batch;
    }

    /// <summary>Lets the writers apply their deferred work before the batch commits; returns their notes by request id.</summary>
    internal IReadOnlyDictionary<string, string> BeforeCommit()
    {
        var notes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (IRevitBatchParticipant participant in _batchParticipants)
        {
            foreach (KeyValuePair<string, string> note in participant.BeforeCommit())
                notes[note.Key] = note.Value;
        }

        return notes;
    }

    /// <summary>Called by the batch when it ends.</summary>
    internal void EndBatch(RevitImportBatch batch)
    {
        if (!ReferenceEquals(_batch, batch))
            return;

        _batch = null;
        foreach (IRevitBatchParticipant participant in _batchParticipants)
            participant.OnBatchEnded();
    }

    /// <inheritdoc />
    public bool IsAvailable => _document is { IsValidObject: true } && !_document.IsReadOnly;

    /// <inheritdoc />
    public WallCreationResult CreateBasicWall(WallCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return IsAvailable
            ? _walls.Create(request)
            : WallCreationResult.Fail(request.Id, "Revit Document is not available or is read-only.");
    }

    /// <inheritdoc />
    public ColumnCreationResult CreateColumn(ColumnCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return IsAvailable
            ? _columns.Create(request)
            : ColumnCreationResult.Fail(request.Id, "Revit Document is not available or is read-only.");
    }

    /// <inheritdoc />
    public OpeningCreationResult CreateOpening(OpeningCreationRequest request, string hostWallElementId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return IsAvailable
            ? _openings.Create(request, hostWallElementId)
            : OpeningCreationResult.Fail(request.Id, "Revit Document is not available or is read-only.");
    }
#else
    /// <summary>Stub constructor used when the Revit API is not available; the context is never available.</summary>
    /// <param name="document">Ignored.</param>
    public RevitApiDocumentContext(object? document = null) { _ = document; }

    /// <inheritdoc />
    public bool IsAvailable => false;

    /// <summary>Fails and reports the planned wall values (no Revit API in this build).</summary>
    /// <param name="request">Wall request.</param>
    public WallCreationResult CreateBasicWall(WallCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            WallCreateArgs args = _planner.PlanWall(request);
            return WallCreationResult.Fail(
                request.Id,
                "No REVIT_API_AVAILABLE. " +
                $"Planned wall: Type={args.WallTypeKey}, Level={args.LevelName ?? "(default)"}, " +
                $"H={args.HeightFeet:F4} ft, L={args.LengthFeet:F4} ft.");
        }
        catch (ArgumentException ex)
        {
            return WallCreationResult.Fail(request.Id, ex.Message);
        }
    }

    /// <summary>Fails and reports the planned column values (no Revit API in this build).</summary>
    /// <param name="request">Column request.</param>
    public ColumnCreationResult CreateColumn(ColumnCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            ColumnCreateArgs args = _planner.PlanColumn(request);
            return ColumnCreationResult.Fail(
                request.Id,
                "No REVIT_API_AVAILABLE. " +
                $"Planned column: Type={args.FamilyTypeKey}, Level={args.Placement.LevelName ?? "(default)"}, " +
                $"H={args.Placement.HeightFeet:F4} ft, W/D={args.WidthMm}/{args.DepthMm} mm.");
        }
        catch (ArgumentException ex)
        {
            return ColumnCreationResult.Fail(request.Id, ex.Message);
        }
    }

    /// <summary>Fails and reports the planned opening values (no Revit API in this build).</summary>
    /// <param name="request">Opening request.</param>
    /// <param name="hostWallElementId">Host wall element id.</param>
    public OpeningCreationResult CreateOpening(OpeningCreationRequest request, string hostWallElementId)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            OpeningCreateArgs args = _openingMapper.Map(request);
            return OpeningCreationResult.Fail(
                request.Id,
                "No REVIT_API_AVAILABLE. " +
                $"Planned {args.Category}: Type={args.TypeKey ?? "(wall opening)"}, Host={hostWallElementId}, " +
                $"W/H={args.WidthMm}/{args.HeightMm} mm.");
        }
        catch (ArgumentException ex)
        {
            return OpeningCreationResult.Fail(request.Id, ex.Message);
        }
    }
#endif
}
