using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DxfToRevit.Core.Building;
using DxfToRevit.Core.Services;
using DxfToRevit.Revit;
using System.Text;

namespace DxfToRevit.Addin;

/// <summary>
/// Ribbon command "Import DXF": imports walls and columns from a DXF drawing into the active project.
/// </summary>
/// <remarks>
/// Flow: settings dialog (levels, height, column options) → DXF file selection → pipeline analysis
/// (no files are written) → wall and column requests → creation through <see cref="BuildingModelService"/>
/// with the Revit creators → summary dialog. All element transactions run inside one transaction group,
/// which is assimilated into a single undo step, or rolled back when nothing was created.
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

        using var settingsForm = new ColumnImportSettingsForm(doc);
        if (settingsForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return Result.Cancelled;

        string? path = PickDxf();
        if (path is null)
            return Result.Cancelled;

        DxfCenterLinePipeline.Result pipe;
        try
        {
            pipe = new DxfCenterLinePipeline().Analyze(path);
        }
        catch (Exception ex)
        {
            message = $"DXF processing failed: {ex.Message}";
            return Result.Failed;
        }

        if (pipe.WallSolids.Count == 0 && pipe.Columns.Count == 0)
        {
            TaskDialog.Show("DxfToRevit",
                $"Nothing to import from this DXF.\n" +
                $"Wall lines: {pipe.WallLineCount}, face pairs: {pipe.PairCount}, ambiguities: {pipe.AmbiguityCount}, columns: {pipe.ColumnCount}.");
            return Result.Cancelled;
        }

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
        var service = new BuildingModelService(new RevitWallCreator(context), new RevitColumnCreator(context));

        IReadOnlyList<WallCreationResult> wallResults = Array.Empty<WallCreationResult>();
        IReadOnlyList<ColumnCreationResult> columnResults = Array.Empty<ColumnCreationResult>();

        // One transaction group: a single undo step, rolled back when nothing could be created.
        using (var group = new TransactionGroup(doc, "DxfToRevit Import DXF"))
        {
            group.Start();

            try
            {
                wallResults = service.CreateWalls(wallRequests);
                columnResults = service.CreateColumns(columnRequests);

                bool hasSuccess = wallResults.Any(r => r.Success) || columnResults.Any(r => r.Success);

                if (hasSuccess)
                {
                    group.Assimilate();
                }
                else
                {
                    group.RollBack();
                }
            }
            catch (Exception ex)
            {
                group.RollBack();
                message = $"Import transaction failed: {ex.Message}";
                return Result.Failed;
            }
        }

        ShowSummary(path, pipe, wallResults, columnResults);
        bool anySuccess = wallResults.Any(r => r.Success) || columnResults.Any(r => r.Success);
        return anySuccess ? Result.Succeeded : Result.Failed;
    }

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
        IReadOnlyList<ColumnCreationResult> columns)
    {
        int wallsOk = walls.Count(r => r.Success);
        int columnsOk = columns.Count(r => r.Success);

        var sb = new StringBuilder();
        sb.AppendLine($"DXF: {System.IO.Path.GetFileName(path)}");
        sb.AppendLine($"Wall lines: {pipe.WallLineCount}, face pairs: {pipe.PairCount}, wall solids: {pipe.WallSolidCount}");
        sb.AppendLine($"Walls created: {wallsOk} / {walls.Count}");
        sb.AppendLine($"Columns created: {columnsOk} / {columns.Count}");

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

        bool allOk = wallsOk == walls.Count && columnsOk == columns.Count;
        new TaskDialog("DxfToRevit")
        {
            MainInstruction = allOk ? "Import finished" : "Import finished with issues",
            MainContent = sb.ToString()
        }.Show();
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
