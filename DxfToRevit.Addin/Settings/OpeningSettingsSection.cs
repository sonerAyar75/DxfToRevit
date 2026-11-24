using Autodesk.Revit.DB;
using Control = System.Windows.Forms.Control;
using DxfToRevit.Core.Import;
using DxfToRevit.Core.Openings;
using DxfToRevit.Revit.Openings;
using System.Windows.Forms;

namespace DxfToRevit.Addin.Settings;

/// <summary>
/// The door, window and opening settings of <see cref="OpeningImportModule"/>: the
/// "Opening types and heights..." button (type map and default heights) and a line that tells how
/// many doors, windows and openings the DXF contains and how many codes still have no Revit type.
/// Only the codes of the chosen categories count.
/// </summary>
internal sealed class OpeningSettingsSection : IImportSettingsSection
{
    private readonly OpeningImportSettings _settings;
    private readonly IReadOnlyList<OpeningCodeUsage> _usages;
    private readonly Label _info = new() { AutoSize = true, Margin = new Padding(3, 3, 3, 3) };
    private Button? _typesButton;

    /// <summary>Creates the section.</summary>
    /// <param name="settings">Session opening settings; the type map dialog writes to it.</param>
    /// <param name="usages">Door, window and opening codes found in the DXF; empty without a DXF.</param>
    public OpeningSettingsSection(OpeningImportSettings settings, IReadOnlyList<OpeningCodeUsage> usages)
    {
        _settings = settings;
        _usages = usages;
    }

    /// <inheritdoc />
    public IReadOnlyList<Control> CreateControls(ImportSettingsContext context)
    {
        var group = new GroupBox
        {
            Text = "Doors, windows and openings",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Dock = DockStyle.Top
        };
        var layout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Dock = DockStyle.Fill,
            WrapContents = false
        };
        _typesButton = new Button { Text = "Opening types and heights...", AutoSize = true };
        layout.Controls.Add(_typesButton);
        layout.Controls.Add(_info);
        group.Controls.Add(layout);

        _typesButton.Click += (_, _) => EditOpeningTypes(group.FindForm(), context);
        Refresh(context);
        return new Control[] { group };
    }

    /// <inheritdoc />
    public void Refresh(ImportSettingsContext context)
    {
        if (_typesButton is not null)
        {
            _typesButton.Enabled = context.IsIncluded(ImportCategories.Doors) ||
                                   context.IsIncluded(ImportCategories.Windows) ||
                                   context.IsIncluded(ImportCategories.EmptyOpenings);
        }

        UpdateInfo(context);
    }

    /// <inheritdoc />
    public bool Confirm(IWin32Window owner, ImportSettingsContext context)
    {
        if (!context.IsIncluded(ImportCategories.Doors) && !context.IsIncluded(ImportCategories.Windows))
            return true;

        IReadOnlyList<OpeningCodeUsage> unmapped = OpeningTypeMapRows.Unmapped(_settings.TypeMap, SelectedUsages(context));
        return unmapped.Count == 0 || MessageBox.Show(owner,
                   "These DXF codes have no Revit type and will not be created:\n" +
                   OpeningTypeMapForm.DescribeUsages(unmapped) +
                   "\n\nContinue anyway? Choose No to map them with \"Opening types and heights...\".",
                   "DxfToRevit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
    }

    /// <inheritdoc />
    /// <remarks>The type map and heights are stored when their dialog is confirmed.</remarks>
    public void Save()
    {
    }

    /// <summary>
    /// The DXF codes of the categories chosen for import: doors and windows that are not imported
    /// need no Revit type, so they are not listed in the type map editor nor checked for a type.
    /// </summary>
    private IReadOnlyList<OpeningCodeUsage> SelectedUsages(ImportSettingsContext context) =>
        _usages.Where(u => context.IsIncluded(OpeningImportModule.CategoryOf(u.Category))).ToList();

    /// <summary>Opens the type map editor; its result is stored in the session settings.</summary>
    private void EditOpeningTypes(IWin32Window? owner, ImportSettingsContext context)
    {
        using var form = new OpeningTypeMapForm(
            _settings,
            CollectTypeKeys(context.Document, BuiltInCategory.OST_Doors),
            CollectTypeKeys(context.Document, BuiltInCategory.OST_Windows),
            SelectedUsages(context));
        if (form.ShowDialog(owner) != DialogResult.OK)
            return;

        _settings.TypeMap = form.TypeMap;
        _settings.DoorHeightCm = form.DoorHeightCm;
        _settings.WindowHeightCm = form.WindowHeightCm;
        _settings.WindowSillCm = form.WindowSillCm;
        _settings.EmptyOpeningHeightCm = form.EmptyOpeningHeightCm;
        UpdateInfo(context);
    }

    /// <summary>
    /// Shows what the loaded DXF contains and how many of its codes still have no Revit type, or,
    /// without a DXF, how many codes are mapped.
    /// </summary>
    private void UpdateInfo(ImportSettingsContext context)
    {
        if (_usages.Count > 0)
        {
            int Count(OpeningCategory category) => _usages.Where(u => u.Category == category).Sum(u => u.Count);
            int unmapped = OpeningTypeMapRows.Unmapped(_settings.TypeMap, SelectedUsages(context)).Count;
            _info.Text =
                $"In the DXF: {Count(OpeningCategory.Door)} door(s), {Count(OpeningCategory.Window)} window(s), " +
                $"{Count(OpeningCategory.EmptyOpening)} empty opening(s).\n" +
                (unmapped == 0
                    ? "Every door and window code has a Revit type."
                    : $"{unmapped} door/window code(s) have no Revit type yet - use the button above.");
            _info.ForeColor = unmapped == 0 ? System.Drawing.SystemColors.ControlText : System.Drawing.Color.Firebrick;
            return;
        }

        var entries = _settings.TypeMap.Entries;
        int doors = entries.Count(e => e.Category == OpeningCategory.Door);
        int windows = entries.Count(e => e.Category == OpeningCategory.Window);
        _info.Text = context.DxfLoaded
            ? "The DXF contains no door, window or opening blocks."
            : entries.Count == 0
                ? "No door or window types are mapped yet."
                : $"Mapped codes: {doors} door, {windows} window.";
    }

    /// <summary>Loaded family types of a category as <c>Family::Type</c> keys, sorted by name.</summary>
    private static IReadOnlyList<string> CollectTypeKeys(Document document, BuiltInCategory category) =>
        new FilteredElementCollector(document)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(category)
            .Cast<FamilySymbol>()
            .OrderBy(s => s.FamilyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .Select(s => OpeningTypeKey.Format(s.FamilyName, s.Name))
            .ToList();
}
