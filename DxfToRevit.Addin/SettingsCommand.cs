using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DxfToRevit.Revit;
using System.Windows.Forms;

namespace DxfToRevit.Addin;

/// <summary>
/// Ribbon command "Settings": opens the import settings dialog on its own so the import
/// options can be chosen (and families loaded) before importing. Choices are remembered for the session.
/// </summary>
[Transaction(TransactionMode.Manual)]
public sealed class SettingsCommand : IExternalCommand
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

        var context = new RevitApiDocumentContext(document);
        using var form = new ImportSettingsForm(
            document, ImportModuleCatalog.Choices(ImportModuleCatalog.Create(context), drawing: null));
        return form.ShowDialog() == DialogResult.OK ? Result.Succeeded : Result.Cancelled;
    }
}
