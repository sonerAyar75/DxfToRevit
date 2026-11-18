using DxfToRevit.Core.Services;

namespace DxfToRevit.App;

/// <summary>
/// Simple Windows form for running the centerline pipeline on a DXF file without Revit.
/// The result is written next to the input as <c>&lt;name&gt;_centerline.dxf</c> (see
/// <see cref="DxfCenterLinePipeline.Run"/>). Files can be chosen with the button or dropped on the form.
/// </summary>
public sealed class MainForm : Form
{
    private readonly TextBox _pathBox;
    private readonly Button _browseButton;
    private readonly Button _runButton;
    private readonly TextBox _logBox;
    private readonly Label _statusLabel;

    /// <summary>Builds the form controls and enables drag-and-drop of DXF files.</summary>
    public MainForm()
    {
        Text = "DxfToRevit — Centerline";
        Width = 720;
        Height = 480;
        MinimumSize = new Size(560, 360);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.75f);

        var pathLabel = new Label
        {
            Text = "DXF file:",
            AutoSize = true,
            Location = new Point(16, 20)
        };

        _pathBox = new TextBox
        {
            Location = new Point(16, 44),
            Width = 560,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            PlaceholderText = "Choose a DXF file…"
        };

        _browseButton = new Button
        {
            Text = "Browse…",
            Location = new Point(588, 42),
            Width = 100,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _browseButton.Click += BrowseButton_Click;

        _runButton = new Button
        {
            Text = "Generate centerlines",
            Location = new Point(16, 88),
            Width = 160,
            Height = 32,
            Enabled = false
        };
        _runButton.Click += RunButton_Click;

        _statusLabel = new Label
        {
            Text = "Ready.",
            AutoSize = true,
            Location = new Point(190, 96),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var logLabel = new Label
        {
            Text = "Log:",
            AutoSize = true,
            Location = new Point(16, 132)
        };

        _logBox = new TextBox
        {
            Location = new Point(16, 156),
            Width = 672,
            Height = 260,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Font = new Font("Consolas", 9.5f),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        Controls.Add(pathLabel);
        Controls.Add(_pathBox);
        Controls.Add(_browseButton);
        Controls.Add(_runButton);
        Controls.Add(_statusLabel);
        Controls.Add(logLabel);
        Controls.Add(_logBox);

        AllowDrop = true;
        DragEnter += MainForm_DragEnter;
        DragDrop += MainForm_DragDrop;
    }

    /// <summary>Opens a file dialog to choose the input DXF.</summary>
    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Choose a DXF file",
            Filter = "DXF files (*.dxf)|*.dxf|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            SetInputPath(dialog.FileName);
    }

    /// <summary>Accepts dragged files.</summary>
    private void MainForm_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            e.Effect = DragDropEffects.Copy;
    }

    /// <summary>Uses the first dropped file when it is a .dxf file.</summary>
    private void MainForm_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
            return;

        string path = files[0];
        if (path.EndsWith(".dxf", StringComparison.OrdinalIgnoreCase))
            SetInputPath(path);
        else
            MessageBox.Show(this, "Please drop a .dxf file.", "DxfToRevit",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    /// <summary>Shows the chosen path and enables the run button when the file exists.</summary>
    private void SetInputPath(string path)
    {
        _pathBox.Text = path;
        _runButton.Enabled = File.Exists(path);
        _statusLabel.Text = File.Exists(path) ? "File selected. Ready to run." : "File not found.";
    }

    /// <summary>Runs the pipeline on a worker thread and logs the result counts.</summary>
    private async void RunButton_Click(object? sender, EventArgs e)
    {
        string inputPath = _pathBox.Text.Trim();
        if (!File.Exists(inputPath))
        {
            MessageBox.Show(this, "Choose a valid DXF file.", "DxfToRevit",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _runButton.Enabled = false;
        _browseButton.Enabled = false;
        _statusLabel.Text = "Processing…";
        _logBox.Clear();
        AppendLog($"Input: {inputPath}");

        try
        {
            var result = await Task.Run(() =>
            {
                var pipeline = new DxfCenterLinePipeline();
                return pipeline.Run(inputPath);
            });

            AppendLog($"Lines: {result.InputLineCount} (wall: {result.WallLineCount})");
            AppendLog($"Face pairs: {result.PairCount}");
            if (result.AmbiguityCount > 0)
            {
                AppendLog($"Ambiguities: {result.AmbiguityCount}");
                foreach (string a in result.Ambiguities)
                    AppendLog($"  - {a}");
            }
            AppendLog($"After junctions: {result.CenterLineCountBeforeMerge}");
            AppendLog($"After collinear merge: {result.CenterLineCountAfterMerge}");
            AppendLog($"Closed walls (WALL-SOLID): {result.WallSolidCount}");
            AppendLog($"Columns (COLUMN*): {result.ColumnCount}");
            AppendLog($"Output: {result.OutputPath}");

            _statusLabel.Text = "Done.";
            MessageBox.Show(this,
                $"Centerline DXF saved:\n{result.OutputPath}",
                "DxfToRevit",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            AppendLog("ERROR: " + ex.Message);
            _statusLabel.Text = "Error.";
            MessageBox.Show(this, ex.Message, "DxfToRevit — Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _runButton.Enabled = File.Exists(_pathBox.Text);
            _browseButton.Enabled = true;
        }
    }

    /// <summary>Appends a line to the log box.</summary>
    private void AppendLog(string line)
    {
        if (_logBox.Text.Length > 0)
            _logBox.AppendText(Environment.NewLine);
        _logBox.AppendText(line);
    }
}
