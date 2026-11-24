using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DxfToRevit.Core.Building;
using DxfToRevit.Core.Openings;
using DxfToRevit.Core.Services;
using DxfToRevit.Revit;
using System.Text;

namespace DxfToRevit.Addin;

/// <summary>
/// Ribbon command "Import DXF": imports walls, columns, doors, windows and openings from a DXF drawing
/// into the active project.
/// </summary>
/// <remarks>
/// Flow: DXF file selection → pipeline analysis (no files are written) → settings dialog (levels,
/// height, column options, and the door/window codes found in the DXF mapped to Revit types) →
/// wall, column and opening requests → creation through
/// <see cref="BuildingModelService"/> with the Revit creators (openings last, in their host walls) →
/// summary dialog. All elements are written in one batch transaction (<see cref="RevitImportBatch"/>):
/// one sub-transaction per element, one commit, one undo step; rolled back when nothing was created.
/// </remarks>
[Transaction(TransactionMode.Manual)]
public sealed class ImportDxfCommand : IExternalCommand
{
    /// <summary>Default family/type key for rectangular columns.</summary>
    private const string RectangularColumnFamily = "M_Rectangular Column";
    /// <summary>Maximum number of ambiguity and failure groups listed in the summary.</summary>
    private const int MaxListedItems = 8;

    /// <summary>Runs the import.</summary>
    /// <param name="commandData">Revit command context.</param>
    /// <param name="message">Error message shown by Revit when the command fails.</param>
    /// <param name="elements">Unused.</param>
    /// <returns><see cref="Result.Succeeded"/> when at least one element was created.</returns>
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Document? doc = commandData.Application.ActiveUIDocument?.Document;
        if (doc is null || doc.IsFamilyDocument)
        {
            message = "Open a project document first (not a family).";
            return Result.Failed;
        }

        // The DXF is read first, so the settings dialog can list the opening codes it contains.
        string? path = PickDxf();
        if (path is null)
            return Result.Cancelled;

        (double, double, double, double) heightsUsed = CurrentDefaultHeights();
        if (!TryAnalyze(path, out DxfCenterLinePipeline.Result pipe, ref message))
            return Result.Failed;

        if (pipe.WallSolids.Count == 0 && pipe.Columns.Count == 0)
        {
            TaskDialog.Show("DxfToRevit",
                $"Nothing to import from this DXF.\n" +
                $"Wall lines: {pipe.WallLineCount}, face pairs: {pipe.PairCount}, ambiguities: {pipe.AmbiguityCount}, columns: {pipe.ColumnCount}.");
            return Result.Cancelled;
        }

        using var settingsForm = new ColumnImportSettingsForm(
            doc, System.IO.Path.GetFileName(path), OpeningCodeSummary.Build(pipe.Openings));
        if (settingsForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return Result.Cancelled;

        // Default heights only matter for blocks without height attributes; re-read when they changed.
        if (CurrentDefaultHeights() != heightsUsed && !TryAnalyze(path, out pipe, ref message))
            return Result.Failed;

        var session = new ImportSessionOptions(
            baseLevelName: settingsForm.SelectedBaseLevelName,
            defaultHeightMm: settingsForm.UnconnectedHeightMm,
            defaultColumnTypeKey: RectangularColumnFamily,
            columnsAreStructural: false,
            topLevelName: settingsForm.SelectedTopLevelName);

        IReadOnlyList<WallCreationRequest> wallRequests =
            new WallCreationRequestFactory(session).FromWallSolids(pipe.WallSolids);

        var columnFactory = new ColumnCreationRequestFactory(session)
        {
            CircularColumnTypeKey = settingsForm.UseRoundSolidForCircularColumns
                ? ColumnTypeKeys.RoundDirectShape
                : settingsForm.SelectedCircularTypeKey ?? ColumnTypeKeys.AutoRoundFamily,
            ConvertCircularToSquare = settingsForm.UseSquareForCircularColumns,
            SquareColumnTypeKey = settingsForm.SelectedSquareTypeKey,
            FreeformColumnTypeKey = settingsForm.FreeformAsFamily
                ? ColumnTypeKeys.FreeformFamily
                : "Freeform Column"
        };

        IReadOnlyList<ColumnCreationRequest> columnRequests = columnFactory
            .FromColumnModels(pipe.Columns)
            .Where(request => request.Type.SectionKind switch
            {
                ColumnSectionKind.Circular => settingsForm.IncludeCircularColumns,
                ColumnSectionKind.Freeform => settingsForm.IncludeFreeformColumns,
                _ => true
            })
            .ToList();

        var context = new RevitApiDocumentContext(doc);
        var service = new BuildingModelService(
            new RevitWallCreator(context),
            new RevitColumnCreator(context),
            openingCreator: new RevitOpeningCreator(context));

        IReadOnlyList<OpeningCreationRequest> openingRequests = settingsForm.IncludeOpenings
            ? new OpeningCreationRequestFactory(session, OpeningImportPreferences.TypeMap).FromOpeningModels(pipe.Openings)
            : Array.Empty<OpeningCreationRequest>();

        IReadOnlyList<WallCreationResult> wallResults = Array.Empty<WallCreationResult>();
        IReadOnlyList<ColumnCreationResult> columnResults = Array.Empty<ColumnCreationResult>();
        IReadOnlyList<OpeningCreationResult> openingResults = Array.Empty<OpeningCreationResult>();
        IReadOnlyList<RevitWarningGroup> revitWarnings = Array.Empty<RevitWarningGroup>();

        // One transaction group: a single undo step, rolled back when nothing could be created.
        // Unit of work: everything was computed above; now all elements are written in one batch
        // transaction (one regeneration and redraw, one undo step) instead of one transaction each.
        try
        {
            using RevitImportBatch batch = context.BeginBatch("DxfToRevit Import DXF");

            wallResults = service.CreateWalls(wallRequests);
            batch.Regenerate();   // walls must exist geometrically before columns join and openings are hosted
            columnResults = service.CreateColumns(columnRequests);

            // Openings need their host walls, so they are created last.
            openingResults = service.CreateOpenings(openingRequests, MapHostWallIds(wallResults));

            bool hasSuccess = wallResults.Any(r => r.Success) || columnResults.Any(r => r.Success) ||
                              openingResults.Any(r => r.Success);
            if (hasSuccess)
            {
                RevitBatchResult committed = batch.Commit();
                if (!committed.Committed)
                {
                    message = "Revit could not commit the import; nothing was created.";
                    return Result.Failed;
                }

                revitWarnings = committed.Warnings;
                wallResults = ApplyBatchOutcome(wallResults, committed,
                    r => r.HostElementId, r => r.RequestId, (r, why) => WallCreationResult.Fail(r.RequestId, why), (r, _) => r);
                columnResults = ApplyBatchOutcome(columnResults, committed,
                    r => r.HostElementId, r => r.RequestId, (r, why) => ColumnCreationResult.Fail(r.RequestId, why), (r, _) => r);
                openingResults = ApplyBatchOutcome(openingResults, committed,
                    r => r.HostElementId, r => r.RequestId, (r, why) => OpeningCreationResult.Fail(r.RequestId, why),
                    (r, note) => new OpeningCreationResult(r.RequestId, true, r.HostElementId,
                        r.Message is null ? note : $"{r.Message} {note}"));
            }

            // Disposing an uncommitted batch rolls everything back.
        }
        catch (Exception ex)
        {
            message = $"Import transaction failed: {ex.Message}";
            return Result.Failed;
        }

        ShowSummary(path, pipe, wallResults, columnResults, openingResults, settingsForm.IncludeOpenings, revitWarnings);
        bool anySuccess = wallResults.Any(r => r.Success) || columnResults.Any(r => r.Success) ||
                          openingResults.Any(r => r.Success);
        return anySuccess ? Result.Succeeded : Result.Failed;
    }

    /// <summary>
    /// Updates the creation results after the batch commit: elements that Revit removed while
    /// resolving errors become failures with Revit's reason, and notes from the commit (door flips)
    /// are attached to the successful results.
    /// </summary>
    private static IReadOnlyList<T> ApplyBatchOutcome<T>(
        IReadOnlyList<T> results,
        RevitBatchResult batch,
        Func<T, string?> elementId,
        Func<T, string> requestId,
        Func<T, string, T> toFailure,
        Func<T, string, T> withNote)
    {
        return results.Select(r =>
        {
            if (batch.WasRemoved(elementId(r), out string reason))
                return toFailure(r, $"Removed by Revit when committing: {reason}");
            return elementId(r) is not null && batch.Notes.TryGetValue(requestId(r), out string? note)
                ? withNote(r, note)
                : r;
        }).ToList();
    }

    /// <summary>Runs the pipeline analysis with the current opening defaults; sets <paramref name="message"/> on failure.</summary>
    private static bool TryAnalyze(string path, out DxfCenterLinePipeline.Result result, ref string message)
    {
        try
        {
            result = new DxfCenterLinePipeline(OpeningImportPreferences.CreateRegistry()).Analyze(path);
            return true;
        }
        catch (Exception ex)
        {
            result = null!;
            message = $"DXF processing failed: {ex.Message}";
            return false;
        }
    }

    /// <summary>The default opening heights currently chosen (door, window, sill, empty opening; cm).</summary>
    private static (double, double, double, double) CurrentDefaultHeights() =>
        (OpeningImportPreferences.DoorHeightCm, OpeningImportPreferences.WindowHeightCm,
         OpeningImportPreferences.WindowSillCm, OpeningImportPreferences.EmptyOpeningHeightCm);

    /// <summary>Asks the user for a DXF file; returns <c>null</c> when cancelled.</summary>
    private static string? PickDxf()
    {
        using var dialog = new System.Windows.Forms.OpenFileDialog
        {
            Title = "Select DXF file",
            Filter = "DXF files (*.dxf)|*.dxf|All files (*.*)|*.*",
            CheckFileExists = true
        };

        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.FileName : null;
    }

    /// <summary>
    /// Shows the import summary: counts, skipped ambiguous pairs, grouped failures and informational
    /// notes (for example columns that were created as DirectShapes).
    /// </summary>
    private static void ShowSummary(
        string path,
        DxfCenterLinePipeline.Result pipe,
        IReadOnlyList<WallCreationResult> walls,
        IReadOnlyList<ColumnCreationResult> columns,
        IReadOnlyList<OpeningCreationResult> openings,
        bool openingsIncluded,
        IReadOnlyList<RevitWarningGroup> revitWarnings)
    {
        int wallsOk = walls.Count(r => r.Success);
        int columnsOk = columns.Count(r => r.Success);
        int openingsOk = openings.Count(r => r.Success);

        var sb = new StringBuilder();
        sb.AppendLine($"DXF: {System.IO.Path.GetFileName(path)}");
        sb.AppendLine($"Wall lines: {pipe.WallLineCount}, face pairs: {pipe.PairCount}, wall solids: {pipe.WallSolidCount}");
        sb.AppendLine($"Walls created: {wallsOk} / {walls.Count}");
        sb.AppendLine($"Columns created: {columnsOk} / {columns.Count}");
        if (openingsIncluded)
            sb.AppendLine($"Openings created: {openingsOk} / {openings.Count} (markers in DXF: {pipe.OpeningMarkerCount})");

        IReadOnlyList<RevitWarningGroup> overlapWarnings =
            revitWarnings.Where(w => w.Kind == RevitWarningKind.Overlap).ToList();
        bool overlaps = overlapWarnings.Count > 0;
        if (overlaps)
        {
            sb.AppendLine();
            sb.AppendLine("Revit reports overlapping or duplicate elements. The drawing may already have been " +
                          "imported at this place. Undo (Ctrl+Z) removes this whole import in one step.");
            AppendRevitWarnings(sb, overlapWarnings, "Overlaps");
        }

        if (pipe.AmbiguityCount > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"Ambiguous face pairs skipped ({pipe.AmbiguityCount}):");
            foreach (string a in pipe.Ambiguities.Take(MaxListedItems))
                sb.AppendLine($"  - {a}");
        }

        AppendFailures(sb, "Walls failed", walls.Where(r => !r.Success).Select(r => (r.RequestId, r.Message)).ToList());
        AppendFailures(sb, "Columns failed", columns.Where(r => !r.Success).Select(r => (r.RequestId, r.Message)).ToList());
        AppendFailures(sb, "Column notes", columns.Where(r => r.Success && r.Message is not null).Select(r => (r.RequestId, r.Message)).ToList());

        if (openingsIncluded)
        {
            AppendFailures(sb, "Openings failed", openings.Where(r => !r.Success).Select(r => (r.RequestId, r.Message)).ToList());
            AppendFailures(sb, "Opening notes", openings.Where(r => r.Success && r.Message is not null).Select(r => (r.RequestId, r.Message)).ToList());
            AppendOpeningIssues(sb, "Opening markers not imported", pipe.OpeningIssues.Where(i => i.Severity == OpeningIssueSeverity.Error).ToList());
            AppendOpeningIssues(sb, "Opening warnings", pipe.OpeningIssues.Where(i => i.Severity == OpeningIssueSeverity.Warning).ToList());
        }

        AppendRevitWarnings(sb, revitWarnings.Where(w => w.Kind == RevitWarningKind.Other).ToList(),
            "Other Revit warnings");

        bool allOk = !overlaps &&
                     wallsOk == walls.Count && columnsOk == columns.Count &&
                     (!openingsIncluded || (openingsOk == openings.Count &&
                                            pipe.OpeningIssues.All(i => i.Severity != OpeningIssueSeverity.Error)));
        new TaskDialog("DxfToRevit")
        {
            MainInstruction = overlaps
                ? "Import finished: elements overlap existing elements"
                : allOk ? "Import finished" : "Import finished with issues",
            MainContent = sb.ToString()
        }.Show();
    }

    /// <summary>
    /// Maps each wall request id to the element id of the wall created for it, so openings can find
    /// their host walls.
    /// </summary>
    private static IReadOnlyDictionary<string, string> MapHostWallIds(IReadOnlyList<WallCreationResult> walls) =>
        walls.Where(r => r.Success && r.HostElementId is not null)
             .ToDictionary(r => r.RequestId, r => r.HostElementId!, StringComparer.Ordinal);

    /// <summary>
    /// Appends a titled section of opening issues grouped by code, with up to five DXF locations each.
    /// </summary>
    private static void AppendOpeningIssues(StringBuilder sb, string title, IReadOnlyList<OpeningIssue> issues)
    {
        if (issues.Count == 0)
            return;

        sb.AppendLine();
        sb.AppendLine($"{title} ({issues.Count}):");

        foreach (var group in issues.GroupBy(i => i.Code).Take(MaxListedItems))
        {
            string where = string.Join(", ", group.Take(5).Select(i => $"({i.LocationMm.X:F0}, {i.LocationMm.Y:F0})"));
            string more = group.Count() > 5 ? $", +{group.Count() - 5} more" : string.Empty;
            sb.AppendLine($"  - {group.Key} at {where}{more}: {group.First().Message}");
        }
    }

    /// <summary>Appends a titled section of Revit warnings: text, how often, and how many elements.</summary>
    private static void AppendRevitWarnings(StringBuilder sb, IReadOnlyList<RevitWarningGroup> warnings, string title)
    {
        if (warnings.Count == 0)
            return;

        sb.AppendLine();
        sb.AppendLine($"{title} ({warnings.Sum(w => w.Count)}):");
        foreach (RevitWarningGroup w in warnings.Take(MaxListedItems))
            sb.AppendLine($"  - {w.Text} ({w.Count}x, {w.ElementCount} elements)");
        if (warnings.Count > MaxListedItems)
            sb.AppendLine($"  - +{warnings.Count - MaxListedItems} more kinds");
    }

    /// <summary>
    /// Appends a titled section that groups results by message and lists up to five request ids per message.
    /// </summary>
    private static void AppendFailures(
        StringBuilder sb, string title, IReadOnlyList<(string Id, string? Message)> failures)
    {
        if (failures.Count == 0)
            return;

        sb.AppendLine();
        sb.AppendLine($"{title} ({failures.Count}):");

        foreach (var group in failures.GroupBy(f => f.Message ?? "(no message)").Take(MaxListedItems))
        {
            string ids = string.Join(", ", group.Select(g => g.Id).Take(5));
            string more = group.Count() > 5 ? $", +{group.Count() - 5} more" : string.Empty;
            sb.AppendLine($"  - [{ids}{more}] {group.Key}");
        }
    }
}
