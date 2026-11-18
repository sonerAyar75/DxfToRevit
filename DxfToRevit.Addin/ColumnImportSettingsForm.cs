using Autodesk.Revit.DB;
using System.Windows.Forms;
using DxfToRevit.Core.Building;
using DxfToRevit.Revit;

namespace DxfToRevit.Addin;

/// <summary>
/// Import settings dialog: base level, top constraint or unconnected height, and how circular and
/// freeform columns are imported. Families can be loaded from disk directly from the dialog.
/// Confirmed choices are stored in <see cref="ColumnImportPreferences"/> for the rest of the session.
/// </summary>
internal sealed class ColumnImportSettingsForm : System.Windows.Forms.Form
{
    private readonly RadioButton _useCircularFamily = new()
    {
        Text = "Use a circular column family",
        AutoSize = true
    };

    private readonly RadioButton _useRoundSolid = new()
    {
        Text = "Model circular columns as round solids (DirectShape, no family needed)",
        AutoSize = true
    };

    private readonly RadioButton _useSquareFallback = new()
    {
        Text = "Replace circular columns with square columns",
        AutoSize = true
    };

    private readonly RadioButton _skipCircular = new()
    {
        Text = "Do not import circular columns",
        AutoSize = true
    };

    private readonly ComboBox _circularTypes = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 390
    };

    private readonly ComboBox _squareTypes = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 390
    };

    private readonly ComboBox _baseLevels = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 390
    };

    private readonly ComboBox _topLevels = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 390
    };

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
    private readonly CheckBox _includeFreeform = new()
    {
        Text = "Import freeform columns",
        AutoSize = true,
        Checked = ColumnImportPreferences.IncludeFreeformColumns
    };

    private readonly CheckBox _freeformAsFamily = new()
    {
        Text = "Create freeform columns as column families (bound to levels)\n" +
               "Unchecked: create them as DirectShape solids.",
        AutoSize = true,
        Margin = new Padding(20, 3, 3, 3),
        Checked = ColumnImportPreferences.FreeformAsFamily
    };

    private readonly Label _circularInfo = new() { AutoSize = true };
    private readonly Label _squareInfo = new() { AutoSize = true };

    /// <summary>Whether circular columns are imported at all.</summary>
    public bool IncludeCircularColumns => !_skipCircular.Checked;
    /// <summary>Whether circular columns are replaced by square columns.</summary>
    public bool UseSquareForCircularColumns => _useSquareFallback.Checked;
    /// <summary>Whether circular columns are created as cylindrical DirectShapes.</summary>
    public bool UseRoundSolidForCircularColumns => _useRoundSolid.Checked;
    /// <summary>Whether freeform columns are imported.</summary>
    public bool IncludeFreeformColumns => _includeFreeform.Checked;
    /// <summary>Whether freeform columns are built as level-bound column families.</summary>
    public bool FreeformAsFamily => _freeformAsFamily.Checked;
    /// <summary>Selected circular type key (a <c>Family::Type</c> pair or <see cref="ColumnTypeKeys.AutoRoundFamily"/>).</summary>
    public string? SelectedCircularTypeKey => (_circularTypes.SelectedItem as ColumnTypeItem)?.Key;
    /// <summary>Selected square/rectangular type key (<c>Family::Type</c>).</summary>
    public string? SelectedSquareTypeKey => (_squareTypes.SelectedItem as ColumnTypeItem)?.Key;
    /// <summary>Selected base level name.</summary>
    public string? SelectedBaseLevelName => (_baseLevels.SelectedItem as LevelItem)?.Name;
    /// <summary>Selected top level name; <c>null</c> means "Unconnected".</summary>
    public string? SelectedTopLevelName =>
    (_topLevels.SelectedItem as LevelItem)?.Name;

    /// <summary>Unconnected height in millimetres (used when no top level is selected).</summary>
    public double UnconnectedHeightMm =>
        (double)_unconnectedHeight.Value;

    /// <summary>Builds the dialog and fills the level and column type lists from the document.</summary>
    /// <param name="document">Project document.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public ColumnImportSettingsForm(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        Text = "DxfToRevit - Column Import Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);

        var description = new Label
        {
            Text = "Choose how circular columns detected in the DXF should be imported.",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };

        var levelGroup = new GroupBox
        {
            Text = "Import Level",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Dock = DockStyle.Top
        };

        var levelLayout = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = true,
            Dock = DockStyle.Fill,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        levelLayout.Controls.Add(new Label
        {
            Text = "Base Level:",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 7, 10, 3)
        }, 0, 0);

        levelLayout.Controls.Add(_baseLevels, 1, 0);

        levelLayout.Controls.Add(new Label
        {
            Text = "Top Constraint:",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 7, 10, 3)
        }, 0, 1);

        levelLayout.Controls.Add(_topLevels, 1, 1);

        levelLayout.Controls.Add(new Label
        {
            Text = "Unconnected Height (mm):",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 7, 10, 3)
        }, 0, 2);

        levelLayout.Controls.Add(_unconnectedHeight, 1, 2);

        levelGroup.Controls.Add(levelLayout);

        var circularGroup = new GroupBox
        {
            Text = "Circular columns",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Dock = DockStyle.Top
        };
        var circularLayout = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 8,
            AutoSize = true,
            Dock = DockStyle.Fill
        };
        circularLayout.Controls.Add(_useCircularFamily, 0, 0);
        circularLayout.Controls.Add(_circularTypes, 0, 1);
        var loadCircular = new Button { Text = "Load circular Family (.rfa)...", AutoSize = true };
        circularLayout.Controls.Add(loadCircular, 1, 1);
        circularLayout.Controls.Add(_circularInfo, 0, 2);
        circularLayout.Controls.Add(_useRoundSolid, 0, 3);
        circularLayout.Controls.Add(_useSquareFallback, 0, 4);
        circularLayout.Controls.Add(_squareTypes, 0, 5);
        var loadSquare = new Button { Text = "Load square Family (.rfa)...", AutoSize = true };
        circularLayout.Controls.Add(loadSquare, 1, 5);
        circularLayout.Controls.Add(_squareInfo, 0, 6);
        circularLayout.Controls.Add(_skipCircular, 0, 7);
        circularGroup.Controls.Add(circularLayout);

        var freeformGroup = new GroupBox
        {
            Text = "Other columns",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Dock = DockStyle.Top
        };
        var freeformLayout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Dock = DockStyle.Fill,
            WrapContents = false
        };
        freeformLayout.Controls.Add(_includeFreeform);
        freeformLayout.Controls.Add(_freeformAsFamily);
        freeformGroup.Controls.Add(freeformLayout);
        _includeFreeform.CheckedChanged += (_, _) => UpdateEnabledState();

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

        var layout = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 5,
            AutoSize = true,
            Dock = DockStyle.Fill
        };
        layout.Controls.Add(description, 0, 0);
        layout.Controls.Add(levelGroup, 0, 1);
        layout.Controls.Add(circularGroup, 0, 2);
        layout.Controls.Add(freeformGroup, 0, 3);
        layout.Controls.Add(buttons, 0, 4);
        Controls.Add(layout);

        AcceptButton = ok;
        CancelButton = cancel;

        PopulateLevels(document);
        PopulateCircularTypes(document);
        PopulateSquareTypes(document);

        loadCircular.Click += (_, _) => LoadFamily(document, true);
        loadSquare.Click += (_, _) => LoadFamily(document, false);
        _useCircularFamily.CheckedChanged += (_, _) => UpdateEnabledState();
        _useSquareFallback.CheckedChanged += (_, _) => UpdateEnabledState();
        _useRoundSolid.CheckedChanged += (_, _) => UpdateEnabledState();
        _skipCircular.CheckedChanged += (_, _) => UpdateEnabledState();

        _baseLevels.SelectedIndexChanged += (_, _) =>
        {
            PopulateTopLevels();
            UpdateEnabledState();
        };

        _topLevels.SelectedIndexChanged += (_, _) =>
            UpdateEnabledState();

        SetInitialMode();
        UpdateEnabledState();

        FormClosing += (_, e) =>
        {
            if (DialogResult == DialogResult.OK)
            {
                if (SelectedBaseLevelName is null)
                {
                    e.Cancel = true;
                    MessageBox.Show(this,
                        "No project Level is available. Create or select a Level before importing.",
                        "DxfToRevit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_useCircularFamily.Checked && SelectedCircularTypeKey is null)
                {
                    e.Cancel = true;
                    MessageBox.Show(this, "Load or select a circular column family, or choose another option.",
                        "DxfToRevit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_useSquareFallback.Checked && SelectedSquareTypeKey is null)
                {
                    e.Cancel = true;
                    MessageBox.Show(this, "Load or select a square/rectangular column family, or choose another option.",
                        "DxfToRevit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ColumnImportPreferences.CircularMode =
                    _useCircularFamily.Checked ? CircularColumnImportMode.CircularFamily
                    : _useRoundSolid.Checked ? CircularColumnImportMode.RoundSolid
                    : _useSquareFallback.Checked ? CircularColumnImportMode.SquareFallback
                    : CircularColumnImportMode.Skip;
                ColumnImportPreferences.IncludeFreeformColumns = IncludeFreeformColumns;
                ColumnImportPreferences.FreeformAsFamily = FreeformAsFamily;
                ColumnImportPreferences.CircularTypeKey = SelectedCircularTypeKey;
                ColumnImportPreferences.SquareTypeKey = SelectedSquareTypeKey;
            }
        };
    }

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
                    string.Equals(
                        item.Name,
                        previousSelection,
                        StringComparison.OrdinalIgnoreCase))
                {
                    _topLevels.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    /// <summary>Fills the circular type list: the automatic option first, then every loaded column type with a diameter parameter.</summary>
    private void PopulateCircularTypes(Document document, string? preferredFamilyName = null)
    {
        string? previousKey = SelectedCircularTypeKey ?? ColumnImportPreferences.CircularTypeKey;
        _circularTypes.Items.Clear();

        var symbols = new FilteredElementCollector(document)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_Columns)
            .Cast<FamilySymbol>()
            .Where(HasDiameterParameter)
            .OrderBy(s => s.FamilyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // The automatic option always comes first: loaded family → add-in/Revit library → cylinder.
        _circularTypes.Items.Add(new ColumnTypeItem(
            ColumnTypeKeys.AutoRoundFamily,
            symbols.Count > 0
                ? "Automatic (use a loaded round family)"
                : "Automatic (load 'M_Round Column' from the Revit library)"));

        foreach (FamilySymbol symbol in symbols)
            _circularTypes.Items.Add(new ColumnTypeItem(
                $"{symbol.FamilyName}::{symbol.Name}", $"{symbol.FamilyName} : {symbol.Name}"));

        SelectItem(_circularTypes, previousKey, preferredFamilyName);
        _circularInfo.Text = symbols.Count == 0
            ? "No circular family is loaded. Automatic mode loads one from the Revit library;\n" +
              "if none is found, columns are created as round solids (DirectShape)."
            : $"{symbols.Count} circular type(s) loaded. Diameters are set from the DXF.";
    }

    /// <summary>Fills the square type list with every loaded column type that has width and depth parameters.</summary>
    private void PopulateSquareTypes(Document document, string? preferredFamilyName = null)
    {
        string? previousKey = SelectedSquareTypeKey ?? ColumnImportPreferences.SquareTypeKey;
        _squareTypes.Items.Clear();

        var symbols = new FilteredElementCollector(document)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_Columns)
            .Cast<FamilySymbol>()
            .Where(HasWidthAndDepthParameters)
            .OrderBy(s => s.FamilyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (FamilySymbol symbol in symbols)
            _squareTypes.Items.Add(new ColumnTypeItem(
                $"{symbol.FamilyName}::{symbol.Name}", $"{symbol.FamilyName} : {symbol.Name}"));

        SelectItem(_squareTypes, previousKey, preferredFamilyName);
        _squareInfo.Text = symbols.Count == 0
            ? "No square/rectangular column type is loaded. Use the button to load an .rfa family."
            : $"{symbols.Count} square/rectangular type(s) available. DXF diameter will be used for both width and depth.";
    }

    /// <summary>Selects the item with <paramref name="preferredKey"/>, else the first item of <paramref name="preferredFamilyName"/>, else the first item.</summary>
    private static void SelectItem(ComboBox combo, string? preferredKey, string? preferredFamilyName)
    {
        int index = -1;
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is not ColumnTypeItem item)
                continue;
            if (!string.IsNullOrWhiteSpace(preferredKey) &&
                string.Equals(item.Key, preferredKey, StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
            if (index < 0 && !string.IsNullOrWhiteSpace(preferredFamilyName) &&
                item.Key.StartsWith(preferredFamilyName + "::", StringComparison.OrdinalIgnoreCase))
                index = i;
        }

        if (combo.Items.Count > 0)
            combo.SelectedIndex = index >= 0 ? index : 0;
    }

    /// <summary>Restores the circular column mode remembered in <see cref="ColumnImportPreferences"/>.</summary>
    private void SetInitialMode()
    {
        switch (ColumnImportPreferences.CircularMode)
        {
            case CircularColumnImportMode.CircularFamily:
                _useCircularFamily.Checked = true;
                break;
            case CircularColumnImportMode.RoundSolid:
                _useRoundSolid.Checked = true;
                break;
            case CircularColumnImportMode.SquareFallback when _squareTypes.Items.Count > 0:
                _useSquareFallback.Checked = true;
                break;
            case CircularColumnImportMode.Skip:
                _skipCircular.Checked = true;
                break;
            default:
                // The automatic option is always available, so the circular family mode is the default.
                _useCircularFamily.Checked = true;
                break;
        }
    }

    /// <summary>Enables only the controls that apply to the current choices.</summary>
    private void UpdateEnabledState()
    {
        _circularTypes.Enabled = _useCircularFamily.Checked;
        _squareTypes.Enabled = _useSquareFallback.Checked;
        _freeformAsFamily.Enabled = _includeFreeform.Checked;

        // Height is editable only when Top Constraint is Unconnected.
        _unconnectedHeight.Enabled = SelectedTopLevelName is null;
    }

    /// <summary>Loads a column family (.rfa) chosen by the user and refreshes the type lists.</summary>
    private void LoadFamily(Document document, bool circular)
    {
        using var dialog = new OpenFileDialog
        {
            Title = circular ? "Select circular column family" : "Select square/rectangular column family",
            Filter = "Revit Family files (*.rfa)|*.rfa",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            bool loaded = document.LoadFamily(dialog.FileName, out Family loadedFamily);
            string? familyName = loadedFamily?.Name;

            // Refresh both lists. Revit may report false when the family is already loaded.
            PopulateCircularTypes(document, familyName);
            PopulateSquareTypes(document, familyName);

            bool selectedFamilyIsCircular = !string.IsNullOrWhiteSpace(familyName) &&
                _circularTypes.Items.Cast<ColumnTypeItem>().Any(i =>
                    i.Key.StartsWith(familyName + "::", StringComparison.OrdinalIgnoreCase));
            bool selectedFamilyIsSquare = !string.IsNullOrWhiteSpace(familyName) &&
                _squareTypes.Items.Cast<ColumnTypeItem>().Any(i =>
                    i.Key.StartsWith(familyName + "::", StringComparison.OrdinalIgnoreCase));

            if (circular && selectedFamilyIsCircular)
                _useCircularFamily.Checked = true;
            else if (!circular && selectedFamilyIsSquare)
                _useSquareFallback.Checked = true;
            else if (!loaded)
                MessageBox.Show(this,
                    "The family was not newly loaded. It may already exist in the project, or it may not contain a supported column type.",
                    "DxfToRevit", MessageBoxButtons.OK, MessageBoxIcon.Information);

            UpdateEnabledState();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not load family:\n{ex.Message}", "DxfToRevit",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>Whether the type has a diameter parameter (see <see cref="RevitColumnParameterNames"/>).</summary>
    private static bool HasDiameterParameter(FamilySymbol symbol) =>
        HasDoubleParameter(symbol, RevitColumnParameterNames.Default.Diameter);

    /// <summary>Whether the type has width and depth parameters (see <see cref="RevitColumnParameterNames"/>).</summary>
    private static bool HasWidthAndDepthParameters(FamilySymbol symbol) =>
        HasDoubleParameter(symbol, RevitColumnParameterNames.Default.Width) &&
        HasDoubleParameter(symbol, RevitColumnParameterNames.Default.Depth);

    /// <summary>Whether the type has a numeric (double) parameter under any of the names.</summary>
    private static bool HasDoubleParameter(FamilySymbol symbol, IEnumerable<string> names)
    {
        foreach (string name in names)
        {
            Parameter? parameter = symbol.LookupParameter(name);
            if (parameter is not null && parameter.StorageType == StorageType.Double)
                return true;
        }
        return false;
    }

    /// <summary>Level entry of the level combo boxes.</summary>
    private sealed class LevelItem
    {
        public string Name { get; }
        public double Elevation { get; }

        public LevelItem(string name, double elevation)
        {
            Name = name;
            Elevation = elevation;
        }

        public override string ToString() => Name;
    }

    /// <summary>Column type entry of the type combo boxes.</summary>
    private sealed class ColumnTypeItem
    {
        public string Key { get; }
        public string DisplayName { get; }

        public ColumnTypeItem(string key, string displayName)
        {
            Key = key;
            DisplayName = displayName;
        }

        public override string ToString() => DisplayName;
    }
}
