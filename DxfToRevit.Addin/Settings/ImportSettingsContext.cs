using Autodesk.Revit.DB;
using DxfToRevit.Core.Import;

namespace DxfToRevit.Addin.Settings;

/// <summary>What the sections of the settings dialog share: the document, whether a DXF is loaded, and the chosen categories.</summary>
internal sealed class ImportSettingsContext
{
    private readonly Func<ImportCategory, bool> _isIncluded;

    /// <summary>Creates the context.</summary>
    /// <param name="document">Project document.</param>
    /// <param name="dxfLoaded">Whether the dialog is opened for a DXF (not on its own).</param>
    /// <param name="isIncluded">Whether a category is currently chosen in the dialog.</param>
    public ImportSettingsContext(Document document, bool dxfLoaded, Func<ImportCategory, bool> isIncluded)
    {
        Document = document;
        DxfLoaded = dxfLoaded;
        _isIncluded = isIncluded;
    }

    /// <summary>Project document.</summary>
    public Document Document { get; }

    /// <summary>Whether the dialog is opened for a DXF (not on its own).</summary>
    public bool DxfLoaded { get; }

    /// <summary>Whether a category is currently chosen in the dialog.</summary>
    /// <param name="category">Import category.</param>
    public bool IsIncluded(ImportCategory category) => _isIncluded(category);
}
