using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows.Forms;

namespace DxfToRevit.Addin;

/// <summary>
/// Ribbon command "Column Settings": opens the import settings dialog on its own so the column
/// options can be chosen (and families loaded) before importing. Choices are remembered for the session.
/// </summary>
[Transaction(TransactionMode.Manual)]
public sealed class ColumnSettingsCommand : IExternalCommand
{
    /// <summary>Shows the settings dialog.</summary>
    /// <param name="commandData">Revit command context.</param>
    /// <param name="message">Error message shown by Revit when the command fails.</param>
    /// <param name="elements">Unused.</param>
    /// <returns><see cref="Result.Succeeded"/> when the dialog was confirmed, otherwise <see cref="Result.Cancelled"/>.</returns>
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Document? document = commandData.Application.ActiveUIDocument?.Document;
        if (document is null || document.IsFamilyDocument)
        {
            message = "Open a project document first (not a family).";
            return Result.Failed;
        }

        using var form = new ColumnImportSettingsForm(document);
        return form.ShowDialog() == DialogResult.OK ? Result.Succeeded : Result.Cancelled;
    }
}
