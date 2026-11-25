using Autodesk.Revit.DB;
using Control = System.Windows.Forms.Control;
using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Import;
using DxfToRevit.Revit.Columns;
using System.Windows.Forms;

namespace DxfToRevit.Addin.Settings;

/// <summary>
/// The column settings of <see cref="ColumnImportModule"/>: the "Circular columns" group (circular
/// family, round solids, square columns or skip, with the type lists and family loading) and the
/// "Other columns" group (freeform columns). Disabled when columns are not imported.
/// </summary>
internal sealed class ColumnSettingsSection : IImportSettingsSection
{
    private readonly ColumnImportSettings _settings;

    private readonly RadioButton _useCircularFamily = new() { Text = "Use a circular column family", AutoSize = true };
    private readonly RadioButton _useRoundSolid = new()
    {
        Text = "Model circular columns as round solids (DirectShape, no family needed)",
        AutoSize = true
    };
    private readonly RadioButton _useSquareFallback = new() { Text = "Replace circular columns with square columns", AutoSize = true };
    private readonly RadioButton _skipCircular = new() { Text = "Do not import circular columns", AutoSize = true };
    private readonly ComboBox _circularTypes = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 390 };
    private readonly ComboBox _squareTypes = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 390 };
    private readonly Label _circularInfo = new() { AutoSize = true };
    private readonly Label _squareInfo = new() { AutoSize = true };
    private readonly CheckBox _includeFreeform = new() { Text = "Import freeform columns", AutoSize = true };
    private readonly CheckBox _freeformAsFamily = new()
    {
        Text = "Create freeform columns as column families (bound to levels)\n" +
               "Unchecked: create them as DirectShape solids.",
        AutoSize = true,
        Margin = new Padding(20, 3, 3, 3)
    };

    private GroupBox? _circularGroup;
    private GroupBox? _freeformGroup;
    private Control? _owner;

    /// <summary>Creates the section.</summary>
    /// <param name="settings">Session column settings, read for the initial state and written by <see cref="Save"/>.</param>
    public ColumnSettingsSection(ColumnImportSettings settings)
    {
        _settings = settings;
        _includeFreeform.Checked = settings.IncludeFreeform;
        _freeformAsFamily.Checked = settings.FreeformAsFamily;
    }

    private string? SelectedCircularTypeKey => (_circularTypes.SelectedItem as ColumnTypeItem)?.Key;
    private string? SelectedSquareTypeKey => (_squareTypes.SelectedItem as ColumnTypeItem)?.Key;

    /// <inheritdoc />
    public IReadOnlyList<Control> CreateControls(ImportSettingsContext context)
    {
        Document document = context.Document;

        _circularGroup = Group("Circular columns");
        var circularLayout = new TableLayoutPanel { ColumnCount = 2, RowCount = 8, AutoSize = true, Dock = DockStyle.Fill };
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
        _circularGroup.Controls.Add(circularLayout);

        _freeformGroup = Group("Other columns");
        var freeformLayout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Dock = DockStyle.Fill,
            WrapContents = false
        };
        freeformLayout.Controls.Add(_includeFreeform);
        freeformLayout.Controls.Add(_freeformAsFamily);
        _freeformGroup.Controls.Add(freeformLayout);
        _owner = _circularGroup;

        PopulateCircularTypes(document);
        PopulateSquareTypes(document);

        loadCircular.Click += (_, _) => LoadFamily(document, circular: true);
        loadSquare.Click += (_, _) => LoadFamily(document, circular: false);
        foreach (RadioButton radio in new[] { _useCircularFamily, _useSquareFallback, _useRoundSolid, _skipCircular })
            radio.CheckedChanged += (_, _) => UpdateEnabledState();
        _includeFreeform.CheckedChanged += (_, _) => UpdateEnabledState();

        SetInitialMode();
        UpdateEnabledState();
        Refresh(context);

        return new Control[] { _circularGroup, _freeformGroup };
    }

    /// <inheritdoc />
    public void Refresh(ImportSettingsContext context)
    {
        bool columns = context.IsIncluded(ImportCategories.Columns);
        if (_circularGroup is not null)
            _circularGroup.Enabled = columns;
        if (_freeformGroup is not null)
            _freeformGroup.Enabled = columns;
    }

    /// <inheritdoc />
    public bool Confirm(IWin32Window owner, ImportSettingsContext context)
    {
        if (!context.IsIncluded(ImportCategories.Columns))
            return true;

        if (_useCircularFamily.Checked && SelectedCircularTypeKey is null)
        {
            MessageBox.Show(owner, "Load or select a circular column family, or choose another option.",
                "DxfToRevit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (_useSquareFallback.Checked && SelectedSquareTypeKey is null)
        {
            MessageBox.Show(owner, "Load or select a square/rectangular column family, or choose another option.",
                "DxfToRevit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public void Save()
    {
        _settings.CircularMode =
            _useCircularFamily.Checked ? CircularColumnMode.CircularFamily
            : _useRoundSolid.Checked ? CircularColumnMode.RoundSolid
            : _useSquareFallback.Checked ? CircularColumnMode.SquareFallback
            : CircularColumnMode.Skip;
        _settings.IncludeFreeform = _includeFreeform.Checked;
        _settings.FreeformAsFamily = _freeformAsFamily.Checked;
        _settings.CircularTypeKey = SelectedCircularTypeKey;
        _settings.SquareTypeKey = SelectedSquareTypeKey;
    }

    /// <summary>A group box of the section.</summary>
    private static GroupBox Group(string text) => new()
    {
        Text = text,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Padding = new Padding(10),
        Dock = DockStyle.Top
    };

    /// <summary>Enables only the controls that apply to the current choices.</summary>
    private void UpdateEnabledState()
    {
        _circularTypes.Enabled = _useCircularFamily.Checked;
        _squareTypes.Enabled = _useSquareFallback.Checked;
        _freeformAsFamily.Enabled = _includeFreeform.Checked;
    }

    /// <summary>Restores the remembered circular column mode.</summary>
    private void SetInitialMode()
    {
        switch (_settings.CircularMode)
        {
            case CircularColumnMode.CircularFamily:
                _useCircularFamily.Checked = true;
                break;
            case CircularColumnMode.RoundSolid:
                _useRoundSolid.Checked = true;
                break;
            case CircularColumnMode.SquareFallback when _squareTypes.Items.Count > 0:
                _useSquareFallback.Checked = true;
                break;
            case CircularColumnMode.Skip:
                _skipCircular.Checked = true;
                break;
            default:
                // The automatic option is always available, so the circular family mode is the default.
                _useCircularFamily.Checked = true;
                break;
        }
    }

    /// <summary>Fills the circular type list: the automatic option first, then every loaded column type with a diameter parameter.</summary>
    private void PopulateCircularTypes(Document document, string? preferredFamilyName = null)
    {
        string? previousKey = SelectedCircularTypeKey ?? _settings.CircularTypeKey;
        _circularTypes.Items.Clear();

        List<FamilySymbol> symbols = ColumnSymbols(document).Where(HasDiameterParameter).ToList();

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
        string? previousKey = SelectedSquareTypeKey ?? _settings.SquareTypeKey;
        _squareTypes.Items.Clear();

        List<FamilySymbol> symbols = ColumnSymbols(document).Where(HasWidthAndDepthParameters).ToList();
        foreach (FamilySymbol symbol in symbols)
            _squareTypes.Items.Add(new ColumnTypeItem(
                $"{symbol.FamilyName}::{symbol.Name}", $"{symbol.FamilyName} : {symbol.Name}"));

        SelectItem(_squareTypes, previousKey, preferredFamilyName);
        _squareInfo.Text = symbols.Count == 0
            ? "No square/rectangular column type is loaded. Use the button to load an .rfa family."
            : $"{symbols.Count} square/rectangular type(s) available. DXF diameter will be used for both width and depth.";
    }

    /// <summary>The architectural column types of the document, sorted by family and type name.</summary>
    private static IEnumerable<FamilySymbol> ColumnSymbols(Document document) =>
        new FilteredElementCollector(document)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_Columns)
            .Cast<FamilySymbol>()
            .OrderBy(s => s.FamilyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase);

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

    /// <summary>Loads a column family (.rfa) chosen by the user and refreshes the type lists.</summary>
    private void LoadFamily(Document document, bool circular)
    {
        IWin32Window? owner = _owner?.FindForm();
        using var dialog = new OpenFileDialog
        {
            Title = circular ? "Select circular column family" : "Select square/rectangular column family",
            Filter = "Revit Family files (*.rfa)|*.rfa",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(owner) != DialogResult.OK)
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
                MessageBox.Show(owner,
                    "The family was not newly loaded. It may already exist in the project, or it may not contain a supported column type.",
                    "DxfToRevit", MessageBoxButtons.OK, MessageBoxIcon.Information);

            UpdateEnabledState();
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner, $"Could not load family:\n{ex.Message}", "DxfToRevit",
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
    private static bool HasDoubleParameter(FamilySymbol symbol, IEnumerable<string> names) =>
        names.Any(name => symbol.LookupParameter(name) is { StorageType: StorageType.Double });

    /// <summary>Column type entry of the type combo boxes.</summary>
    private sealed record ColumnTypeItem(string Key, string DisplayName)
    {
        /// <inheritdoc />
        public override string ToString() => DisplayName;
    }
}
