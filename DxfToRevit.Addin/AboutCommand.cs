using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace DxfToRevit.Addin;

/// <summary>
/// Ribbon command "About": shows a short description of the add-in.
/// </summary>
[Transaction(TransactionMode.ReadOnly)]
public sealed class AboutCommand : IExternalCommand
{
    /// <summary>Shows the about dialog.</summary>
    /// <param name="commandData">Revit command context.</param>
    /// <param name="message">Unused.</param>
    /// <param name="elements">Unused.</param>
    /// <returns>Always <see cref="Result.Succeeded"/>.</returns>
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        TaskDialog.Show("About DxfToRevit",
            "DxfToRevit\nDXF geometry import for Autodesk Revit.\n\n" +
            "Imports recognized wall and column geometry into the active Revit project.");
        return Result.Succeeded;
    }
}
