using Autodesk.Revit.DB;
using Control = System.Windows.Forms.Control;
using System.Windows.Forms;

namespace DxfToRevit.Addin.Settings;

/// <summary>
/// The "Import Level" section: base level, top constraint (a level above the base, or
/// <c>Unconnected</c>) and the unconnected height. Shared by every module.
/// </summary>
internal sealed class LevelSettingsSection : IImportSettingsSection
{
    private readonly ComboBox _baseLevels = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 390 };
    private readonly ComboBox _topLevels = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 390 };

    private readonly NumericUpDown _unconnectedHeight = new()
    {
        Minimum = 1,
        Maximum = 100000,
        Increment = 50,
        Value = 3000,
        DecimalPlaces = 0,
        ThousandsSeparator = true,
        Width = 120
    };

    private readonly List<LevelItem> _availableLevels = new();

    /// <summary>Selected base level name.</summary>
    public string? SelectedBaseLevelName => (_baseLevels.SelectedItem as LevelItem)?.Name;

    /// <summary>Selected top level name; <c>null</c> means "Unconnected".</summary>
    public string? SelectedTopLevelName => (_topLevels.SelectedItem as LevelItem)?.Name;

    /// <summary>Unconnected height in millimetres (used when no top level is selected).</summary>
    public double UnconnectedHeightMm => (double)_unconnectedHeight.Value;

    /// <inheritdoc />
    public IReadOnlyList<Control> CreateControls(ImportSettingsContext context)
    {
        var group = new GroupBox
        {
            Text = "Import Level",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Dock = DockStyle.Top
        };

        var layout = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = true,
            Dock = DockStyle.Fill,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        layout.Controls.Add(Caption("Base Level:"), 0, 0);
        layout.Controls.Add(_baseLevels, 1, 0);
        layout.Controls.Add(Caption("Top Constraint:"), 0, 1);
        layout.Controls.Add(_topLevels, 1, 1);
        layout.Controls.Add(Caption("Unconnected Height (mm):"), 0, 2);
        layout.Controls.Add(_unconnectedHeight, 1, 2);
        group.Controls.Add(layout);

        PopulateLevels(context.Document);

        _baseLevels.SelectedIndexChanged += (_, _) =>
        {
            PopulateTopLevels();
            UpdateEnabledState();
        };
        _topLevels.SelectedIndexChanged += (_, _) => UpdateEnabledState();
        UpdateEnabledState();

        return new Control[] { group };
    }

    /// <inheritdoc />
    public void Refresh(ImportSettingsContext context)
    {
        // The levels apply to every category.
    }

    /// <inheritdoc />
    public bool Confirm(IWin32Window owner, ImportSettingsContext context)
    {
        if (SelectedBaseLevelName is not null)
            return true;

        MessageBox.Show(owner,
            "No project Level is available. Create or select a Level before importing.",
            "DxfToRevit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }

    /// <inheritdoc />
    /// <remarks>The levels are chosen for each import and not remembered.</remarks>
    public void Save()
    {
    }

    /// <summary>A label of the level table.</summary>
    private static Label Caption(string text) =>
        new() { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 7, 10, 3) };

    /// <summary>Height is editable only when Top Constraint is Unconnected.</summary>
    private void UpdateEnabledState() => _unconnectedHeight.Enabled = SelectedTopLevelName is null;

    /// <summary>Fills the base level list (sorted by elevation) and selects the lowest level.</summary>
    private void PopulateLevels(Document document)
    {
        _baseLevels.Items.Clear();
        _availableLevels.Clear();

        var levels = new FilteredElementCollector(document)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .OrderBy(level => level.Elevation)
            .ThenBy(level => level.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (Level level in levels)
        {
            var item = new LevelItem(level.Name, level.Elevation);
            _baseLevels.Items.Add(item);
            _availableLevels.Add(item);
        }

        if (_baseLevels.Items.Count > 0)
            _baseLevels.SelectedIndex = 0;

        PopulateTopLevels();
    }

    /// <summary>Fills the top constraint list with "Unconnected" and the levels above the base level, keeping the previous choice.</summary>
    private void PopulateTopLevels()
    {
        string? previousSelection = SelectedTopLevelName;

        _topLevels.Items.Clear();

        // Null selection represents Unconnected.
        _topLevels.Items.Add("Unconnected");

        if (_baseLevels.SelectedItem is LevelItem baseLevel)
        {
            foreach (LevelItem level in _availableLevels)
            {
                if (level.Elevation > baseLevel.Elevation)
                    _topLevels.Items.Add(level);
            }
        }

        _topLevels.SelectedIndex = 0;

        if (!string.IsNullOrWhiteSpace(previousSelection))
        {
            for (int i = 1; i < _topLevels.Items.Count; i++)
            {
                if (_topLevels.Items[i] is LevelItem item &&
                    string.Equals(item.Name, previousSelection, StringComparison.OrdinalIgnoreCase))
                {
                    _topLevels.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    /// <summary>Level entry of the level combo boxes.</summary>
    private sealed record LevelItem(string Name, double Elevation)
    {
        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
