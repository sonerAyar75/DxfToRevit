using DxfToRevit.Core.Openings;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;
using System.Drawing;
using System.Windows.Forms;

namespace DxfToRevit.Addin;

/// <summary>
/// Editor of the opening type map. When a DXF is loaded, every door and window <c>TYPE</c> code
/// found in it is listed first (with its count and sizes), so each code can be given a Revit type;
/// rows of the saved map that are not in the DXF (for example <c>*</c>) follow. The Revit type is
/// picked by right-clicking (or double-clicking) the cell, which lists the types loaded in the
/// project; the map can also be loaded from and saved to a file (<see cref="OpeningTypeMapFile"/>).
/// Also edits the default heights used when a marker has no height attribute.
/// </summary>
internal sealed class OpeningTypeMapForm : Form
{
    private const int CategoryColumn = 0;
    private const int CodeColumn = 1;
    private const int CountColumn = 2;
    private const int SizesColumn = 3;
    private const int TypeColumn = 4;
    private const string NoCodeText = "(no TYPE)";

    private readonly IReadOnlyList<string> _doorTypes;
    private readonly IReadOnlyList<string> _windowTypes;
    private readonly IReadOnlyList<OpeningCodeUsage> _usages;
    private readonly DataGridView _grid = new()
    {
        Width = 820,
        Height = 300,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.CellSelect,
        RowHeadersWidth = 28
    };

    private readonly ContextMenuStrip _typeMenu = new();

    /// <summary>Map the rows were filled from; keeps the entries of codes not in this DXF.</summary>
    private OpeningTypeMap _baseMap = OpeningTypeMap.Empty;
    private readonly NumericUpDown _doorHeight = CreateCmInput(1);
    private readonly NumericUpDown _windowHeight = CreateCmInput(1);
    private readonly NumericUpDown _windowSill = CreateCmInput(0);
    private readonly NumericUpDown _openingHeight = CreateCmInput(1);

    /// <summary>The edited type map (valid after the dialog returns OK).</summary>
    public OpeningTypeMap TypeMap { get; private set; } = OpeningTypeMap.Empty;

    /// <summary>Default door height in centimetres.</summary>
    public double DoorHeightCm => (double)_doorHeight.Value;

    /// <summary>Default window height in centimetres.</summary>
    public double WindowHeightCm => (double)_windowHeight.Value;

    /// <summary>Default window sill height in centimetres.</summary>
    public double WindowSillCm => (double)_windowSill.Value;

    /// <summary>Default empty opening height in centimetres.</summary>
    public double EmptyOpeningHeightCm => (double)_openingHeight.Value;

    /// <summary>Builds the dialog with the current preferences.</summary>
    /// <param name="doorTypes">Loaded door types as <c>Family::Type</c> keys.</param>
    /// <param name="windowTypes">Loaded window types as <c>Family::Type</c> keys.</param>
    /// <param name="usages">Codes found in the loaded DXF; empty when no DXF is loaded.</param>
    public OpeningTypeMapForm(
        IReadOnlyList<string> doorTypes, IReadOnlyList<string> windowTypes, IReadOnlyList<OpeningCodeUsage> usages)
    {
        _doorTypes = doorTypes ?? throw new ArgumentNullException(nameof(doorTypes));
        _windowTypes = windowTypes ?? throw new ArgumentNullException(nameof(windowTypes));
        _usages = usages ?? throw new ArgumentNullException(nameof(usages));

        Text = "DxfToRevit - Opening Types";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);

        int dxfOpenings = _usages.Sum(u => u.Count);
        var description = new Label
        {
            Text = (_usages.Count > 0
                       ? $"The DXF contains {dxfOpenings} opening(s). Each door and window TYPE code found in it is listed below.\n"
                       : "No door or window codes to map: no DXF is loaded, or doors and windows are not chosen for import.\n") +
                   "Right-click (or double-click) a Revit type cell to pick from the loaded types.\n" +
                   "When the DXF size differs, the type is duplicated and resized.",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };

        var category = new DataGridViewComboBoxColumn
        {
            HeaderText = "Category",
            FillWeight = 12,
            FlatStyle = FlatStyle.Flat
        };
        category.Items.AddRange(nameof(OpeningCategory.Door), nameof(OpeningCategory.Window));
        _grid.Columns.Add(category);
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Code (TYPE)", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "In DXF", FillWeight = 8, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Sizes in DXF (mm)", FillWeight = 22, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Revit type (Family::Type)", FillWeight = 46 });
        _grid.CellMouseDown += OnCellMouseDown;
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == TypeColumn)
                ShowTypeMenu(e.RowIndex, _grid.PointToClient(Cursor.Position));
        };
        _grid.CellFormatting += OnCellFormatting;
        _grid.CellPainting += OnCellPainting;
        _grid.CellValueChanged += (_, _) => _grid.Invalidate(); // a changed (no TYPE) row changes the hints
        _grid.UserDeletingRow += (_, e) =>
        {
            // The rows are the codes of the drawing: deleting one only clears its type.
            if (e.Row is not null)
                e.Row.Cells[TypeColumn].Value = string.Empty;
            e.Cancel = true;
        };
        _grid.DataError += (_, e) => e.ThrowException = false;

        var fileButtons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        var load = new Button { Text = "Load from file...", AutoSize = true };
        var save = new Button { Text = "Save to file...", AutoSize = true };
        fileButtons.Controls.Add(load);
        fileButtons.Controls.Add(save);
        load.Click += (_, _) => LoadFromFile();
        save.Click += (_, _) => SaveToFile();

        var heights = new GroupBox
        {
            Text = "Default heights when the block has no attribute (cm)",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Dock = DockStyle.Top
        };
        var heightLayout = new TableLayoutPanel { ColumnCount = 4, RowCount = 2, AutoSize = true, Dock = DockStyle.Fill };
        AddLabeled(heightLayout, "Door height:", _doorHeight, 0, 0);
        AddLabeled(heightLayout, "Opening height:", _openingHeight, 2, 0);
        AddLabeled(heightLayout, "Window height:", _windowHeight, 0, 1);
        AddLabeled(heightLayout, "Window sill height:", _windowSill, 2, 1);
        heights.Controls.Add(heightLayout);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Bottom,
            Margin = new Padding(0, 12, 0, 0)
        };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        var layout = new TableLayoutPanel { ColumnCount = 1, RowCount = 5, AutoSize = true, Dock = DockStyle.Fill };
        layout.Controls.Add(description, 0, 0);
        layout.Controls.Add(_grid, 0, 1);
        layout.Controls.Add(fileButtons, 0, 2);
        layout.Controls.Add(heights, 0, 3);
        layout.Controls.Add(buttons, 0, 4);
        Controls.Add(layout);

        AcceptButton = ok;
        CancelButton = cancel;

        _doorHeight.Value = ClampCm(_doorHeight, OpeningImportPreferences.DoorHeightCm);
        _windowHeight.Value = ClampCm(_windowHeight, OpeningImportPreferences.WindowHeightCm);
        _windowSill.Value = ClampCm(_windowSill, OpeningImportPreferences.WindowSillCm);
        _openingHeight.Value = ClampCm(_openingHeight, OpeningImportPreferences.EmptyOpeningHeightCm);
        FillRows(OpeningImportPreferences.TypeMap);

        FormClosing += (_, e) =>
        {
            if (DialogResult != DialogResult.OK)
                return;

            if (!TryReadMap(out OpeningTypeMap map, out string? error))
            {
                e.Cancel = true;
                MessageBox.Show(this, error, "DxfToRevit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            IReadOnlyList<OpeningCodeUsage> unmapped = OpeningTypeMapRows.Unmapped(map, _usages);
            if (unmapped.Count > 0 && MessageBox.Show(this,
                    "These DXF codes have no Revit type and will not be created:\n" +
                    DescribeUsages(unmapped) + "\n\nContinue anyway?",
                    "DxfToRevit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            TypeMap = map;
        };
    }

    /// <summary>One line per usage, for example <c>Door 2 (3 pcs)</c>.</summary>
    internal static string DescribeUsages(IEnumerable<OpeningCodeUsage> usages) =>
        string.Join("\n", usages.Select(u =>
            $"  {u.Category} {(u.Code.Length == 0 ? NoCodeText : u.Code)} ({u.Count} pcs)"));

    /// <summary>Replaces the grid rows: one row per door and window code of the DXF, typed from <paramref name="map"/>.</summary>
    private void FillRows(OpeningTypeMap map)
    {
        _baseMap = map;
        _grid.Rows.Clear();
        foreach (OpeningTypeMapRow row in OpeningTypeMapRows.Build(map, _usages))
        {
            int index = _grid.Rows.Add(
                row.Category.ToString(),
                row.Code,
                row.FromDxf ? row.CountInDxf.ToString() : string.Empty,
                row.Sizes,
                row.TypeKey ?? string.Empty);

            DataGridViewRow gridRow = _grid.Rows[index];
            gridRow.Tag = row;
            // The rows are the codes of the drawing: only the Revit type is edited.
            gridRow.Cells[CategoryColumn].ReadOnly = true;
            gridRow.Cells[CodeColumn].ReadOnly = true;
            gridRow.Cells[CategoryColumn].Style.BackColor = SystemColors.Control;
            gridRow.Cells[CodeColumn].Style.BackColor = SystemColors.Control;
        }
    }

    /// <summary>
    /// Reads the rows into a map, merged with the entries of the codes that are not listed (see
    /// <see cref="OpeningTypeMapRows.Merge"/>). Rows without a Revit type are skipped; a
    /// <c>(no TYPE)</c> row is stored as the <c>*</c> entry of its category.
    /// </summary>
    private bool TryReadMap(out OpeningTypeMap map, out string? error)
    {
        _grid.EndEdit();
        var entries = new List<OpeningTypeMapEntry>();
        error = null;
        map = OpeningTypeMap.Empty;

        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow)
                continue;

            string type = CellText(row, TypeColumn);
            if (type.Length == 0)
                continue;

            string categoryText = CellText(row, CategoryColumn);
            string code = CellText(row, CodeColumn);
            int line = row.Index + 1;
            if (!Enum.TryParse(categoryText, ignoreCase: true, out OpeningCategory category) ||
                category == OpeningCategory.EmptyOpening)
            {
                error = $"Row {line}: choose Door or Window.";
                return false;
            }

            if (!OpeningTypeKey.TryParse(type, out _))
            {
                error = $"Row {line}: choose a Revit type (right-click the cell).";
                return false;
            }

            entries.Add(new OpeningTypeMapEntry(category, OpeningTypeMapRows.MapCode(code), type));
        }

        map = OpeningTypeMapRows.Merge(_baseMap, _usages, entries);
        return true;
    }

    /// <summary>Selects the clicked cell and opens the type menu for a right-click on the type column.</summary>
    private void OnCellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right || e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        _grid.CurrentCell = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        if (e.ColumnIndex != TypeColumn)
            return;

        Rectangle cell = _grid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
        ShowTypeMenu(e.RowIndex, new Point(cell.Left + e.X, cell.Top + e.Y));
    }

    /// <summary>
    /// Shows the loaded types of the row's category, grouped by family; choosing one writes its key
    /// into the cell.
    /// </summary>
    private void ShowTypeMenu(int rowIndex, Point location)
    {
        _grid.EndEdit();
        DataGridViewRow row = _grid.Rows[rowIndex];
        if (row.Cells[TypeColumn].ReadOnly)
            return;

        bool window = IsWindowRow(row);
        IReadOnlyList<string> keys = window ? _windowTypes : _doorTypes;

        _typeMenu.Items.Clear();
        if (keys.Count == 0)
        {
            _typeMenu.Items.Add(new ToolStripMenuItem(
                window ? "No window types are loaded in the project" : "No door types are loaded in the project")
            { Enabled = false });
        }

        foreach (IGrouping<string, OpeningTypeKey> family in keys
                     .Select(k => OpeningTypeKey.TryParse(k, out OpeningTypeKey? key) ? key : null)
                     .OfType<OpeningTypeKey>()
                     .GroupBy(k => k.FamilyName ?? string.Empty))
        {
            var familyItem = new ToolStripMenuItem(family.Key.Length == 0 ? "(no family)" : family.Key);
            foreach (OpeningTypeKey key in family)
            {
                string value = key.ToString();
                familyItem.DropDownItems.Add(new ToolStripMenuItem(key.TypeName, null,
                    (_, _) => row.Cells[TypeColumn].Value = value));
            }

            _typeMenu.Items.Add(familyItem);
        }

        _typeMenu.Items.Add(new ToolStripSeparator());
        _typeMenu.Items.Add(new ToolStripMenuItem("Clear", null, (_, _) => row.Cells[TypeColumn].Value = string.Empty));
        _typeMenu.Show(_grid, location);
    }

    /// <summary>Shows <c>(no TYPE)</c> for empty DXF codes and type keys that are not loaded in red.</summary>
    private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || _grid.Rows[e.RowIndex].IsNewRow || e.CellStyle is null)
            return;

        DataGridViewRow row = _grid.Rows[e.RowIndex];
        if (e.ColumnIndex == CodeColumn && row.Tag is OpeningTypeMapRow { FromDxf: true } && CellText(row, CodeColumn).Length == 0)
        {
            e.Value = NoCodeText;
            e.FormattingApplied = true;
            return;
        }

        if (e.ColumnIndex != TypeColumn)
            return;

        string type = CellText(row, TypeColumn);
        if (type.Length == 0)
            return;

        bool window = IsWindowRow(row);
        bool loaded = OpeningTypeKey.TryParse(type, out OpeningTypeKey? key) && key is not null &&
                      (window ? _windowTypes : _doorTypes).Any(k =>
                          OpeningTypeKey.TryParse(k, out OpeningTypeKey? candidate) && candidate is not null &&
                          key.Matches(candidate.FamilyName ?? string.Empty, candidate.TypeName));

        e.CellStyle.ForeColor = loaded ? _grid.DefaultCellStyle.ForeColor : Color.Firebrick;
        row.Cells[TypeColumn].ToolTipText = loaded ? string.Empty : "This type is not loaded in the project.";
    }

    /// <summary>
    /// Paints a hint in the empty type cell of a DXF row: the default type it falls back to (grey),
    /// or that it is not mapped (red). Only painted, so the cell value stays empty while editing.
    /// </summary>
    private void OnCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.ColumnIndex != TypeColumn || e.RowIndex < 0 || e.Graphics is null ||
            _grid.Rows[e.RowIndex].IsNewRow || _grid.Rows[e.RowIndex].Tag is not OpeningTypeMapRow { FromDxf: true })
            return;

        DataGridViewRow row = _grid.Rows[e.RowIndex];
        bool editing = _grid.IsCurrentCellInEditMode && _grid.CurrentCell == row.Cells[TypeColumn];
        if (editing || CellText(row, TypeColumn).Length > 0)
            return;

        string? fallback = FindDefaultType(IsWindowRow(row) ? OpeningCategory.Window : OpeningCategory.Door);
        string hint = fallback is null ? "(not mapped - right-click to choose)" : $"(default: {fallback})";
        Color color = fallback is null ? Color.Firebrick : SystemColors.GrayText;

        e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);
        TextRenderer.DrawText(e.Graphics, hint, e.CellStyle?.Font ?? _grid.Font, e.CellBounds, color,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        e.Handled = true;
    }

    /// <summary>
    /// The type that codes without their own type fall back to: the <c>(no TYPE)</c> row of the
    /// category when the DXF has one, otherwise the saved <c>*</c> entry.
    /// </summary>
    private string? FindDefaultType(OpeningCategory category)
    {
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (CellText(row, CodeColumn).Length != 0)
                continue;
            if ((IsWindowRow(row) ? OpeningCategory.Window : OpeningCategory.Door) != category)
                continue;

            string type = CellText(row, TypeColumn);
            return type.Length > 0 ? type : null;
        }

        return _baseMap.Resolve(category, OpeningTypeMap.DefaultCode);
    }

    /// <summary>Loads a type map file; the DXF codes stay listed and get the types from the file.</summary>
    private void LoadFromFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Load opening type map",
            Filter = "Type map files (*.txt;*.csv)|*.txt;*.csv|All files (*.*)|*.*",
            CheckFileExists = true,
            FileName = OpeningImportPreferences.TypeMapFilePath ?? string.Empty
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            OpeningTypeMapReadResult result = OpeningTypeMapFile.Read(dialog.FileName);
            FillRows(result.Map);
            OpeningImportPreferences.TypeMapFilePath = dialog.FileName;

            if (result.Errors.Count > 0)
            {
                string errors = string.Join(Environment.NewLine, result.Errors.Take(10));
                string more = result.Errors.Count > 10 ? $"{Environment.NewLine}+{result.Errors.Count - 10} more" : string.Empty;
                MessageBox.Show(this, $"Some lines were skipped:{Environment.NewLine}{errors}{more}",
                    "DxfToRevit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not read the file:\n{ex.Message}", "DxfToRevit",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>Saves the rows that have a Revit type to a type map file.</summary>
    private void SaveToFile()
    {
        if (!TryReadMap(out OpeningTypeMap map, out string? error))
        {
            MessageBox.Show(this, error, "DxfToRevit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Save opening type map",
            Filter = "Type map files (*.txt)|*.txt|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = OpeningImportPreferences.TypeMapFilePath ?? "OpeningTypes.txt"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            OpeningTypeMapFile.Write(dialog.FileName, map);
            OpeningImportPreferences.TypeMapFilePath = dialog.FileName;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not save the file:\n{ex.Message}", "DxfToRevit",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>Whether the row's category is Window.</summary>
    private static bool IsWindowRow(DataGridViewRow row) =>
        string.Equals(CellText(row, CategoryColumn), nameof(OpeningCategory.Window), StringComparison.OrdinalIgnoreCase);

    /// <summary>Trimmed text of a cell (empty when the cell has no value).</summary>
    private static string CellText(DataGridViewRow row, int column) =>
        Convert.ToString(row.Cells[column].Value)?.Trim() ?? string.Empty;

    /// <summary>A centimetre input (0.1 cm steps).</summary>
    private static NumericUpDown CreateCmInput(decimal minimum) => new()
    {
        Minimum = minimum,
        Maximum = 10000,
        DecimalPlaces = 1,
        Increment = 5,
        Width = 80
    };

    /// <summary>Clamps a value into the input range.</summary>
    private static decimal ClampCm(NumericUpDown input, double value) =>
        Math.Min(input.Maximum, Math.Max(input.Minimum, (decimal)value));

    /// <summary>Adds a label and an input to a table layout row.</summary>
    private static void AddLabeled(TableLayoutPanel layout, string text, Control input, int column, int row)
    {
        layout.Controls.Add(new Label
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 7, 10, 3)
        }, column, row);
        layout.Controls.Add(input, column + 1, row);
    }
}
