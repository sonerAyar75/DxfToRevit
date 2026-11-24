using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Dxf;
using DxfToRevit.Core.Import;
using DxfToRevit.Core.Openings;
using DxfToRevit.Core.Walls;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;

namespace DxfToRevit.Addin;

/// <summary>
/// The import modules of the add-in, wired to the Revit creators. A new kind of element (for
/// example floors or rooms) is added here; the settings dialog, the import and the summary pick it
/// up without further changes.
/// </summary>
internal static class ImportModuleCatalog
{
    /// <summary>Creates the modules for one import into the document of <paramref name="context"/>.</summary>
    /// <param name="context">Revit document context used by the creators.</param>
    public static IReadOnlyList<IImportModule> Create(RevitApiDocumentContext context)
    {
        var service = new BuildingModelService(
            new RevitWallCreator(context),
            new RevitColumnCreator(context),
            openingCreator: new RevitOpeningCreator(context));

        return new IImportModule[]
        {
            new WallImportModule(service),
            new ColumnImportModule(service),
            new OpeningImportModule(service)
        };
    }

    /// <summary>The category check boxes of the settings dialog, with the counts of <paramref name="drawing"/>.</summary>
    /// <param name="modules">Modules of the import.</param>
    /// <param name="drawing">Analysed DXF; <c>null</c> when the dialog is opened without a DXF.</param>
    public static IReadOnlyList<ImportCategoryChoice> Choices(
        IEnumerable<IImportModule> modules, DxfDrawing? drawing) =>
        modules.OrderBy(m => m.Order)
            .SelectMany(m => m.Categories.Select(c =>
                new ImportCategoryChoice(c, drawing is null ? null : m.CountInDrawing(c, drawing))))
            .ToList();
}
