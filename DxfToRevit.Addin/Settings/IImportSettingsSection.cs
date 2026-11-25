using System.Windows.Forms;

namespace DxfToRevit.Addin.Settings;

/// <summary>
/// One part of the import settings dialog, usually the settings of one import module (for example
/// the column options). The dialog only arranges the sections (Composite): a new module brings its
/// own section through <see cref="ImportModuleCatalog"/>, without changes to the dialog.
/// </summary>
internal interface IImportSettingsSection
{
    /// <summary>Creates the controls of the section (usually group boxes), in display order.</summary>
    /// <param name="context">Shared dialog context.</param>
    IReadOnlyList<Control> CreateControls(ImportSettingsContext context);

    /// <summary>Updates the section after the chosen categories changed (for example disables it).</summary>
    /// <param name="context">Shared dialog context.</param>
    void Refresh(ImportSettingsContext context);

    /// <summary>Checks the choices when the user confirms; may ask or warn the user.</summary>
    /// <param name="owner">Dialog, for message boxes.</param>
    /// <param name="context">Shared dialog context.</param>
    /// <returns><c>false</c> to keep the dialog open.</returns>
    bool Confirm(IWin32Window owner, ImportSettingsContext context);

    /// <summary>Stores the choices in the session settings after the dialog was confirmed.</summary>
    void Save();
}
