using DxfToRevit.Addin.Settings;
using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Dxf;
using DxfToRevit.Core.Import;
using DxfToRevit.Core.Openings;
using DxfToRevit.Core.Walls;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
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

    /// <summary>
    /// The settings sections of the modules, in display order (the level section is added by the
    /// dialog itself). A module with options adds its section here.
    /// </summary>
    /// <param name="settings">Session settings the sections read and write.</param>
    /// <param name="drawing">Analysed DXF; <c>null</c> when the dialog is opened without a DXF.</param>
    public static IReadOnlyList<IImportSettingsSection> CreateSettingsSections(ImportSettings settings, DxfDrawing? drawing) =>
        new IImportSettingsSection[]
        {
            new ColumnSettingsSection(settings.Columns),
            new OpeningSettingsSection(settings.Openings,
                drawing is null ? Array.Empty<OpeningCodeUsage>() : OpeningCodeSummary.Build(drawing.Analysis.Openings))
        };

    /// <summary>The options of the modules for the session settings (<see cref="ImportPlanContext.Options"/>).</summary>
    /// <param name="settings">Session settings.</param>
    public static ImportBag CreateModuleOptions(ImportSettings settings)
    {
        var options = new ImportBag();
        options.Set(settings.Columns.ToOptions());
        options.Set(settings.Openings.ToOptions());
        return options;
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
