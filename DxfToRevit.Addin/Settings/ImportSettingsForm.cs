using Autodesk.Revit.DB;
using DxfToRevit.Core.Import;
using System.Windows.Forms;

namespace DxfToRevit.Addin.Settings;

/// <summary>
/// Import settings dialog. It shows the "Import" group (one check box per category of the modules,
/// and whether elements already in the model are skipped), then the level section and the settings
/// sections of the modules, which it only arranges (Composite). On confirmation every section
/// checks its choices, then the choices are stored in the session <see cref="ImportSettings"/>.
/// </summary>
internal sealed class ImportSettingsForm : System.Windows.Forms.Form
{
    private readonly ImportSettings _settings;
    private readonly ImportSettingsContext _context;
    private readonly IReadOnlyList<IImportSettingsSection> _sections;

    /// <summary>One check box per import category of the modules, by category key.</summary>
    private readonly Dictionary<string, (ImportCategory Category, CheckBox Box)> _categoryBoxes = new(StringComparer.Ordinal);

    private readonly CheckBox _skipExisting = new()
    {
        Text = "Skip elements that already exist in the model\n" +
               "(same kind at the same place on the same level; doors and windows are placed in existing walls)",
        AutoSize = true,
        Margin = new Padding(3, 8, 3, 3)
    };

    /// <summary>Builds the dialog.</summary>
    /// <param name="settings">Session settings: initial state, and where the confirmed choices are stored.</param>
    /// <param name="document">Project document.</param>
    /// <param name="categories">The import categories of the modules, with their counts in the DXF; one check box each.</param>
    /// <param name="moduleSections">The settings sections of the modules, in display order.</param>
    /// <param name="dxfFileName">Name of the DXF being imported; <c>null</c> when the dialog is opened on its own.</param>
    /// <exception cref="System.ArgumentNullException">An argument other than <paramref name="dxfFileName"/> is <c>null</c>.</exception>
    public ImportSettingsForm(
        ImportSettings settings,
        Document document,
        IReadOnlyList<ImportCategoryChoice> categories,
        IReadOnlyList<IImportSettingsSection> moduleSections,
        string? dxfFileName = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(moduleSections);

        _context = new ImportSettingsContext(document, dxfFileName is not null, IsIncluded);
        Levels = new LevelSettingsSection();
        _sections = new IImportSettingsSection[] { Levels }.Concat(moduleSections).ToList();

        foreach (ImportCategoryChoice choice in categories)
        {
            var box = new CheckBox
            {
                AutoSize = true,
                Margin = new Padding(3, 3, 14, 3),
                Text = choice.Count is int n ? $"{choice.Category.Name} ({n})" : choice.Category.Name,
                Checked = settings.Selection.IsIncluded(choice.Category)
            };
            _categoryBoxes[choice.Category.Key] = (choice.Category, box);
        }

        _skipExisting.Checked = settings.Selection.SkipExisting;

        Text = dxfFileName is null ? "DxfToRevit - Import Settings" : $"DxfToRevit - Import Settings - {dxfFileName}";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);

        var layout = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Dock = DockStyle.Fill };
        layout.Controls.Add(new Label
        {
            Text = "Choose what to import, the level, how columns are imported and how doors and windows are mapped.",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        });
        layout.Controls.Add(CreateImportGroup());
        foreach (IImportSettingsSection section in _sections)
        {
            foreach (System.Windows.Forms.Control control in section.CreateControls(_context))
                layout.Controls.Add(control);
        }

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Bottom,
            Margin = new Padding(0, 16, 0, 0)
        };
        var ok = new Button { Text = "Continue", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons);
        Controls.Add(layout);

        AcceptButton = ok;
        CancelButton = cancel;
        FormClosing += OnFormClosing;
    }

    /// <summary>The level section: base level, top constraint and unconnected height.</summary>
    public LevelSettingsSection Levels { get; }

    /// <summary>Keys of the categories chosen for import.</summary>
    public IReadOnlyList<string> IncludedCategoryKeys =>
        _categoryBoxes.Values.Where(c => c.Box.Checked).Select(c => c.Category.Key).ToList();

    /// <summary>Whether elements already in the model are skipped.</summary>
    public bool SkipExisting => _skipExisting.Checked;

    /// <summary>Whether a category is chosen for import; <c>false</c> when the dialog does not offer it.</summary>
    /// <param name="category">Import category.</param>
    public bool IsIncluded(ImportCategory category) =>
        _categoryBoxes.TryGetValue(category.Key, out var entry) && entry.Box.Checked;

    /// <summary>The "Import" group: the category check boxes and the skip option.</summary>
    private GroupBox CreateImportGroup()
    {
        var group = new GroupBox
        {
            Text = "Import",
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
        var categoryPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        categoryPanel.Controls.AddRange(_categoryBoxes.Values.Select(c => (System.Windows.Forms.Control)c.Box).ToArray());
        layout.Controls.Add(categoryPanel);
        layout.Controls.Add(_skipExisting);
        group.Controls.Add(layout);

        foreach (CheckBox box in _categoryBoxes.Values.Select(c => c.Box))
        {
            box.CheckedChanged += (_, _) =>
            {
                foreach (IImportSettingsSection section in _sections)
                    section.Refresh(_context);
            };
        }

        return group;
    }

    /// <summary>On confirmation: every section checks its choices, then they are stored in the session settings.</summary>
    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (DialogResult != DialogResult.OK)
            return;

        if (!Levels.Confirm(this, _context))
        {
            e.Cancel = true;
            return;
        }

        if (_context.DxfLoaded && IncludedCategoryKeys.Count == 0)
        {
            e.Cancel = true;
            MessageBox.Show(this, "Choose at least one category to import.",
                "DxfToRevit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_sections.Skip(1).Any(section => !section.Confirm(this, _context)))
        {
            e.Cancel = true;
            return;
        }

        foreach (IImportSettingsSection section in _sections)
            section.Save();
        foreach ((ImportCategory category, CheckBox box) in _categoryBoxes.Values)
            _settings.Selection.SetIncluded(category, box.Checked);
        _settings.Selection.SkipExisting = SkipExisting;
    }
}
