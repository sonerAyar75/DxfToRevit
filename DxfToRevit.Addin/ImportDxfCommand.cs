using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Dxf;
using DxfToRevit.Core.Import;
using DxfToRevit.Core.Openings;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;
using System.Text;

namespace DxfToRevit.Addin;

/// <summary>
/// Ribbon command "Import DXF": imports walls, columns, doors, windows and openings from a DXF drawing
/// into the active project.
/// </summary>
/// <remarks>
/// Flow: DXF file selection → pipeline analysis (no files are written) → settings dialog (the
/// categories of the modules of <see cref="ImportModuleCatalog"/>, levels, height, column options,
/// and the door/window codes found in the DXF mapped to Revit types) → <see cref="ImportCoordinator"/>
/// plans every module (requests, elements already in the model) → creation in module order →
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
        if (!TryAnalyze(path, out DxfDrawing drawing, ref message))
            return Result.Failed;

        var context = new RevitApiDocumentContext(doc);
        var coordinator = new ImportCoordinator(ImportModuleCatalog.Create(context));
        IReadOnlyList<ImportCategoryChoice> choices = ImportModuleCatalog.Choices(coordinator.Modules, drawing);

        if (choices.All(c => c.Count == 0))
        {
            TaskDialog.Show("DxfToRevit",
                $"Nothing to import from this DXF.\n" +
                $"Wall lines: {drawing.Analysis.WallLineCount}, face pairs: {drawing.Analysis.PairCount}, ambiguities: {drawing.Analysis.AmbiguityCount}, columns: {drawing.Analysis.ColumnCount}.");
            return Result.Cancelled;
        }

        using var settingsForm = new ImportSettingsForm(
            doc, choices, System.IO.Path.GetFileName(path), OpeningCodeSummary.Build(drawing.Analysis.Openings));
        if (settingsForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return Result.Cancelled;

        // Default heights only matter for blocks without height attributes; re-read when they changed.
        if (CurrentDefaultHeights() != heightsUsed && !TryAnalyze(path, out drawing, ref message))
            return Result.Failed;

        var session = new ImportSessionOptions(
            baseLevelName: settingsForm.SelectedBaseLevelName,
            defaultHeightMm: settingsForm.UnconnectedHeightMm,
            defaultColumnTypeKey: RectangularColumnFamily,
            columnsAreStructural: false,
            topLevelName: settingsForm.SelectedTopLevelName);

        var options = new ImportBag();
        options.Set(new ColumnImportOptions
        {
            CircularTypeKey = settingsForm.UseRoundSolidForCircularColumns
                ? ColumnTypeKeys.RoundDirectShape
                : settingsForm.SelectedCircularTypeKey ?? ColumnTypeKeys.AutoRoundFamily,
            ConvertCircularToSquare = settingsForm.UseSquareForCircularColumns,
            SquareTypeKey = settingsForm.SelectedSquareTypeKey,
            FreeformTypeKey = settingsForm.FreeformAsFamily ? ColumnTypeKeys.FreeformFamily : "Freeform Column",
            IncludeCircular = settingsForm.IncludeCircularColumns,
            IncludeFreeform = settingsForm.IncludeFreeformColumns
        });
        options.Set(new OpeningImportOptions(OpeningImportPreferences.TypeMap));

        // Compute first: every module builds its requests and sets aside what is already in the
        // model (existing walls also host the new doors and windows). Nothing is written yet.
        coordinator.Plan(new ImportPlanContext(
            drawing, session, new ExistingElementMatcher(RevitExistingElementReader.Read(doc)),
            settingsForm.IncludedCategoryKeys, settingsForm.SkipExisting, options));

        IReadOnlyList<ImportModuleResult> results = Array.Empty<ImportModuleResult>();
        IReadOnlyList<RevitWarningGroup> revitWarnings = Array.Empty<RevitWarningGroup>();

        // Then write: all elements in one batch transaction (one regeneration and redraw, one undo
        // step), rolled back when nothing could be created.
        try
        {
            using RevitImportBatch batch = context.BeginBatch("DxfToRevit Import DXF");
            results = coordinator.Create(batch.Regenerate);

            if (results.Any(r => r.Created > 0))
            {
                RevitBatchResult committed = batch.Commit();
                if (!committed.Committed)
                {
                    message = "Revit could not commit the import; nothing was created.";
                    return Result.Failed;
                }

                revitWarnings = committed.Warnings;
                results = results.Select(r => r.WithCommitOutcome(
                    id => committed.WasRemoved(id, out string reason) ? $"Removed by Revit when committing: {reason}" : null,
                    committed.Notes)).ToList();
            }

            // Disposing an uncommitted batch rolls everything back.
        }
        catch (Exception ex)
        {
            message = $"Import transaction failed: {ex.Message}";
            return Result.Failed;
        }

        ShowSummary(path, drawing.Analysis, results, coordinator.Skipped, revitWarnings);
        bool anySuccess = results.Any(r => r.Created > 0);
        return anySuccess || coordinator.Skipped.Count > 0 ? Result.Succeeded : Result.Failed;
    }

    /// <summary>Runs the pipeline analysis with the current opening defaults; sets <paramref name="message"/> on failure.</summary>
    private static bool TryAnalyze(string path, out DxfDrawing result, ref string message)
    {
        try
        {
            result = new DxfCenterLinePipeline(OpeningImportPreferences.CreateRegistry(), LayerRules.Default).AnalyzeDrawing(path);
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
    /// Shows the import summary: what each module created, elements already in the model, Revit's
    /// overlap warnings, skipped ambiguous wall faces, failures and notes per module, the sections
    /// of the modules (for example opening marker warnings) and the other Revit warnings.
    /// </summary>
    private static void ShowSummary(
        string path,
        DxfCenterLinePipeline.Result pipe,
        IReadOnlyList<ImportModuleResult> results,
        IReadOnlyDictionary<ImportCategory, int> skipped,
        IReadOnlyList<RevitWarningGroup> revitWarnings)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"DXF: {System.IO.Path.GetFileName(path)}");
        sb.AppendLine($"Wall lines: {pipe.WallLineCount}, face pairs: {pipe.PairCount}, wall solids: {pipe.WallSolidCount}");
        foreach (ImportModuleResult r in results.Where(r => r.Included))
            sb.AppendLine($"{r.Name} created: {r.Created} / {r.Entries.Count}{(r.CountSuffix is null ? string.Empty : " " + r.CountSuffix)}");

        if (skipped.Count > 0)
        {
            sb.AppendLine();
            string list = string.Join(", ", skipped.Select(s => $"{s.Value} {s.Key.Name.ToLowerInvariant()}"));
            sb.AppendLine($"Already in the model, not created again: {list}.");
        }

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

        foreach (ImportModuleResult r in results)
        {
            AppendFailures(sb, $"{r.Name} failed", r.Entries.Where(e => !e.Success).Select(e => (e.RequestId, e.Message)).ToList());
            AppendFailures(sb, r.NoteTitle, r.Entries.Where(e => e.Success && e.Message is not null).Select(e => (e.RequestId, e.Message)).ToList());
            foreach (ImportReportSection section in r.Sections)
            {
                sb.AppendLine();
                sb.AppendLine($"{section.Title} ({section.Count}):");
                foreach (string line in section.Lines)
                    sb.AppendLine($"  - {line}");
            }
        }

        AppendRevitWarnings(sb, revitWarnings.Where(w => w.Kind == RevitWarningKind.Other).ToList(),
            "Other Revit warnings");

        bool allOk = !overlaps && results.All(r => r.IsComplete);
        new TaskDialog("DxfToRevit")
        {
            MainInstruction = overlaps
                ? "Import finished: elements overlap existing elements"
                : allOk ? "Import finished" : "Import finished with issues",
            MainContent = sb.ToString()
        }.Show();
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
