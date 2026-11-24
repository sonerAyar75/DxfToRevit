using Autodesk.Revit.DB;
using System.Windows.Forms;
using DxfToRevit.Core.Building;
using DxfToRevit.Core.Openings;
using DxfToRevit.Revit;

namespace DxfToRevit.Addin;

/// <summary>
/// Import settings dialog: base level, top constraint or unconnected height, how circular and
/// freeform columns are imported, which categories are imported (and whether elements already in
/// the model are skipped), and how doors and windows are mapped (with the
/// type map editor). Families can be loaded from disk directly from the dialog. Confirmed choices
/// are stored in <see cref="ColumnImportPreferences"/> and <see cref="OpeningImportPreferences"/>
/// for the rest of the session.
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

    private readonly CheckBox _includeWalls = CreateCategoryBox(ImportSelectionPreferences.IncludeWalls);
    private readonly CheckBox _includeColumns = CreateCategoryBox(ImportSelectionPreferences.IncludeColumns);
    private readonly CheckBox _includeDoors = CreateCategoryBox(ImportSelectionPreferences.IncludeDoors);
    private readonly CheckBox _includeWindows = CreateCategoryBox(ImportSelectionPreferences.IncludeWindows);
    private readonly CheckBox _includeEmptyOpenings = CreateCategoryBox(ImportSelectionPreferences.IncludeEmptyOpenings);

    private readonly CheckBox _skipExisting = new()
    {
        Text = "Skip elements that already exist in the model\n" +
               "(same kind at the same place on the same level; doors and windows are placed in existing walls)",
        AutoSize = true,
        Margin = new Padding(3, 8, 3, 3),
        Checked = ImportSelectionPreferences.SkipExisting
    };

    private readonly Label _openingInfo = new() { AutoSize = true, Margin = new Padding(3, 3, 3, 3) };
    private Button? _openingTypes;
    private GroupBox? _circularGroup;
    private GroupBox? _freeformGroup;
    private readonly bool _dxfLoaded;
    private readonly IReadOnlyList<OpeningCodeUsage> _openingUsages;

    /// <summary>Whether walls are imported.</summary>
    public bool IncludeWalls => _includeWalls.Checked;
    /// <summary>Whether columns are imported.</summary>
    public bool IncludeColumns => _includeColumns.Checked;
    /// <summary>Whether doors are imported.</summary>
    public bool IncludeDoors => _includeDoors.Checked;
    /// <summary>Whether windows are imported.</summary>
    public bool IncludeWindows => _includeWindows.Checked;
    /// <summary>Whether empty wall openings are imported.</summary>
    public bool IncludeEmptyOpenings => _includeEmptyOpenings.Checked;
    /// <summary>Whether doors, windows or empty openings are imported.</summary>
    public bool IncludeAnyOpening => IncludeDoors || IncludeWindows || IncludeEmptyOpenings;
    /// <summary>Whether elements already in the model are skipped.</summary>
    public bool SkipExisting => _skipExisting.Checked;

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
    /// <param name="dxfFileName">Name of the DXF being imported; <c>null</c> when the dialog is opened on its own.</param>
    /// <param name="openingUsages">Door, window and opening codes found in the DXF, listed in the type map editor.</param>
    /// <param name="counts">Elements per category in the DXF, shown next to the check boxes; <c>null</c> without a DXF.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public ColumnImportSettingsForm(
        Document document,
        string? dxfFileName = null,
        IReadOnlyList<OpeningCodeUsage>? openingUsages = null,
        ImportCounts? counts = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        _dxfLoaded = dxfFileName is not null;
        _openingUsages = openingUsages ?? Array.Empty<OpeningCodeUsage>();

        _includeWalls.Text = CategoryText("Walls", counts?.Walls);
        _includeColumns.Text = CategoryText("Columns", counts?.Columns);
        _includeDoors.Text = CategoryText("Doors", counts?.Doors);
        _includeWindows.Text = CategoryText("Windows", counts?.Windows);
        _includeEmptyOpenings.Text = CategoryText("Empty openings", counts?.EmptyOpenings);

        Text = dxfFileName is null ? "DxfToRevit - Import Settings" : $"DxfToRevit - Import Settings - {dxfFileName}";
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
            Text = "Choose what to import, the level, how columns are imported and how doors and windows are mapped.",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };

        var importGroup = new GroupBox
        {
            Text = "Import",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Dock = DockStyle.Top
        };
        var importLayout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Dock = DockStyle.Fill,
            WrapContents = false
        };
        var categories = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        categories.Controls.AddRange(new System.Windows.Forms.Control[] { _includeWalls, _includeColumns, _includeDoors, _includeWindows, _includeEmptyOpenings });
        importLayout.Controls.Add(categories);
        importLayout.Controls.Add(_skipExisting);
        importGroup.Controls.Add(importLayout);
        foreach (CheckBox box in new[] { _includeWalls, _includeColumns, _includeDoors, _includeWindows, _includeEmptyOpenings })
            box.CheckedChanged += (_, _) => UpdateEnabledState();

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

        var openingGroup = new GroupBox
        {
            Text = "Doors, windows and openings",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Dock = DockStyle.Top
        };
        var openingLayout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Dock = DockStyle.Fill,
            WrapContents = false
        };
        var openingTypes = new Button { Text = "Opening types and heights...", AutoSize = true };
        openingLayout.Controls.Add(openingTypes);
        openingLayout.Controls.Add(_openingInfo);
        openingGroup.Controls.Add(openingLayout);
        _circularGroup = circularGroup;
        _freeformGroup = freeformGroup;
        openingTypes.Click += (_, _) => EditOpeningTypes(document);
        _openingTypes = openingTypes;

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
            RowCount = 7,
            AutoSize = true,
            Dock = DockStyle.Fill
        };
        layout.Controls.Add(description, 0, 0);
        layout.Controls.Add(importGroup, 0, 1);
        layout.Controls.Add(levelGroup, 0, 2);
        layout.Controls.Add(circularGroup, 0, 3);
        layout.Controls.Add(freeformGroup, 0, 4);
        layout.Controls.Add(openingGroup, 0, 5);
        layout.Controls.Add(buttons, 0, 6);
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
        UpdateOpeningInfo();
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

                if (_dxfLoaded && !IncludeWalls && !IncludeColumns && !IncludeAnyOpening)
                {
                    e.Cancel = true;
                    MessageBox.Show(this, "Choose at least one category to import.",
                        "DxfToRevit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (IncludeDoors || IncludeWindows)
                {
                    IReadOnlyList<OpeningCodeUsage> unmapped =
                        OpeningTypeMapRows.Unmapped(OpeningImportPreferences.TypeMap, _openingUsages)
                            .Where(u => u.Category == OpeningCategory.Door ? IncludeDoors : IncludeWindows)
                            .ToList();
                    if (unmapped.Count > 0 && MessageBox.Show(this,
                            "These DXF codes have no Revit type and will not be created:\n" +
                            OpeningTypeMapForm.DescribeUsages(unmapped) +
                            "\n\nContinue anyway? Choose No to map them with \"Opening types and heights...\".",
                            "DxfToRevit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                if (IncludeColumns && _useCircularFamily.Checked && SelectedCircularTypeKey is null)
                {
                    e.Cancel = true;
                    MessageBox.Show(this, "Load or select a circular column family, or choose another option.",
                        "DxfToRevit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (IncludeColumns && _useSquareFallback.Checked && SelectedSquareTypeKey is null)
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
                ImportSelectionPreferences.IncludeWalls = IncludeWalls;
                ImportSelectionPreferences.IncludeColumns = IncludeColumns;
                ImportSelectionPreferences.IncludeDoors = IncludeDoors;
                ImportSelectionPreferences.IncludeWindows = IncludeWindows;
                ImportSelectionPreferences.IncludeEmptyOpenings = IncludeEmptyOpenings;
                ImportSelectionPreferences.SkipExisting = SkipExisting;
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
        if (_openingTypes is not null)
            _openingTypes.Enabled = IncludeDoors || IncludeWindows || IncludeEmptyOpenings;
        if (_circularGroup is not null)
            _circularGroup.Enabled = IncludeColumns;
        if (_freeformGroup is not null)
            _freeformGroup.Enabled = IncludeColumns;

        // Height is editable only when Top Constraint is Unconnected.
        _unconnectedHeight.Enabled = SelectedTopLevelName is null;
    }

    /// <summary>A category check box.</summary>
    private static CheckBox CreateCategoryBox(bool isChecked) =>
        new() { AutoSize = true, Checked = isChecked, Margin = new Padding(3, 3, 14, 3) };

    /// <summary>Category label with the number found in the DXF, for example <c>Doors (11)</c>.</summary>
    private static string CategoryText(string name, int? count) =>
        count is int n ? $"{name} ({n})" : name;

    /// <summary>Opens the opening type map editor and stores the result in <see cref="OpeningImportPreferences"/>.</summary>
    private void EditOpeningTypes(Document document)
    {
        using var form = new OpeningTypeMapForm(
            CollectTypeKeys(document, BuiltInCategory.OST_Doors),
            CollectTypeKeys(document, BuiltInCategory.OST_Windows),
            _openingUsages);
        if (form.ShowDialog(this) != DialogResult.OK)
            return;

        OpeningImportPreferences.TypeMap = form.TypeMap;
        OpeningImportPreferences.DoorHeightCm = form.DoorHeightCm;
        OpeningImportPreferences.WindowHeightCm = form.WindowHeightCm;
        OpeningImportPreferences.WindowSillCm = form.WindowSillCm;
        OpeningImportPreferences.EmptyOpeningHeightCm = form.EmptyOpeningHeightCm;
        UpdateOpeningInfo();
    }

    /// <summary>
    /// Shows what the loaded DXF contains and how many of its codes still have no Revit type, or,
    /// without a DXF, how many codes are mapped.
    /// </summary>
    private void UpdateOpeningInfo()
    {
        if (_openingUsages.Count > 0)
        {
            int Count(OpeningCategory category) => _openingUsages.Where(u => u.Category == category).Sum(u => u.Count);
            int unmapped = OpeningTypeMapRows.Unmapped(OpeningImportPreferences.TypeMap, _openingUsages).Count;
            _openingInfo.Text =
                $"In the DXF: {Count(OpeningCategory.Door)} door(s), {Count(OpeningCategory.Window)} window(s), " +
                $"{Count(OpeningCategory.EmptyOpening)} empty opening(s).\n" +
                (unmapped == 0
                    ? "Every door and window code has a Revit type."
                    : $"{unmapped} door/window code(s) have no Revit type yet - use the button above.");
            _openingInfo.ForeColor = unmapped == 0 ? System.Drawing.SystemColors.ControlText : System.Drawing.Color.Firebrick;
            return;
        }

        var entries = OpeningImportPreferences.TypeMap.Entries;
        int doors = entries.Count(e => e.Category == OpeningCategory.Door);
        int windows = entries.Count(e => e.Category == OpeningCategory.Window);
        _openingInfo.Text = _dxfLoaded
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
