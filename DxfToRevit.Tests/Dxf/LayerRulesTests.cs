using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Dxf;
using DxfToRevit.Core.Openings;
using DxfToRevit.Core.Walls;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;

namespace DxfToRevit.Tests.Dxf;

/// <summary>
/// Data-driven layer rules: other layer names through the rules, the defaults unchanged, and the
/// rules reaching the analysis. Also the drawing that keeps the loaded document for the modules.
/// </summary>
public sealed class LayerRulesTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "DxfToRevitTests_" + Guid.NewGuid().ToString("N"));

    public LayerRulesTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort temp cleanup */ }
    }

    [Theory]
    [InlineData("A-WALL-20", true, 200)]
    [InlineData("a-wall", true, 0)]
    [InlineData("WALL-20", false, 0)]
    public void WallLayerRule_CustomBaseName(string layer, bool isWall, double thickness)
    {
        var rule = new WallLayerRule(new[] { "A-WALL" });

        Assert.Equal(isWall, rule.IsWallLayer(layer));
        Assert.Equal(thickness > 0, rule.TryGetThicknessMm(layer, out double mm));
        Assert.Equal(thickness, mm);
    }

    [Fact]
    public void WallLayerRule_SeveralBaseNames()
    {
        var rule = new WallLayerRule(new[] { "WALL", "PERDE" });

        Assert.True(rule.TryGetThicknessMm("PERDE-30", out double mm));
        Assert.Equal(300, mm);
        Assert.True(rule.TryGetThicknessMm("WALL-15", out mm));
        Assert.Equal(150, mm);
    }

    [Fact]
    public void ColumnLayerRule_CustomNamesAndPrefixes()
    {
        var rule = new ColumnLayerRule(names: new[] { "S-COLS" }, prefixes: new[] { "S-COL-" });

        Assert.True(rule.IsColumnLayer("s-cols"));
        Assert.True(rule.IsColumnLayer("S-COL-40X40"));
        Assert.False(rule.IsColumnLayer("COLUMN"));
    }

    [Fact]
    public void Rules_BlankNames_Throw()
    {
        Assert.Throws<ArgumentException>(() => new WallLayerRule(Array.Empty<string>()));
        Assert.Throws<ArgumentException>(() => new WallLayerRule(new[] { " " }));
        Assert.Throws<ArgumentException>(() => new ColumnLayerRule(Array.Empty<string>(), Array.Empty<string>()));
    }

    [Fact]
    public void Default_IsTheStandardNaming()
    {
        Assert.Equal(new[] { "WALL" }, LayerRules.Default.Walls.BaseNames);
        Assert.Equal(new[] { "COLUMN", "COLUMNS" }, LayerRules.Default.Columns.Names);
        Assert.Equal(new[] { "COLUMN-" }, LayerRules.Default.Columns.Prefixes);
    }

    /// <summary>A drawing on other layer names is analysed with matching rules, and ignored with the defaults.</summary>
    [Fact]
    public void Pipeline_UsesTheGivenLayerRules()
    {
        string path = SaveDrawing("A-WALL-20", "S-COLS");
        var rules = new LayerRules(new WallLayerRule(new[] { "A-WALL" }), new ColumnLayerRule(new[] { "S-COLS" }, Array.Empty<string>()));

        DxfCenterLinePipeline.Result custom =
            new DxfCenterLinePipeline(OpeningDefinitionRegistry.CreateDefault(), rules).Analyze(path);
        DxfCenterLinePipeline.Result standard = new DxfCenterLinePipeline().Analyze(path);

        Assert.Equal(1, custom.WallSolidCount);
        Assert.Equal(200, custom.WallSolids[0].ThicknessMm, 6);
        Assert.Equal(1, custom.ColumnCount);
        Assert.Equal(0, standard.WallSolidCount);
        Assert.Equal(0, standard.ColumnCount);
    }

    /// <summary>The drawing keeps the loaded document, so a module can read entities the analysis does not cover.</summary>
    [Fact]
    public void AnalyzeDrawing_KeepsTheDocumentWithTheAnalysis()
    {
        string path = SaveDrawing("WALL-20", "COLUMN");

        DxfDrawing drawing = new DxfCenterLinePipeline().AnalyzeDrawing(path);

        Assert.Equal(path, drawing.Path);
        Assert.Equal(1, drawing.Analysis.WallSolidCount);
        Assert.Contains(drawing.Document.Entities.Circles, c => c.Layer.Name == "COLUMN");
    }

    /// <summary>One 200 mm wall (two face lines) on <paramref name="wallLayer"/> and one circle on <paramref name="columnLayer"/>.</summary>
    private string SaveDrawing(string wallLayer, string columnLayer)
    {
        var doc = new DxfDocument();
        var walls = new Layer(wallLayer);
        doc.Entities.Add(new Line(new Vector3(0, 0, 0), new Vector3(3000, 0, 0)) { Layer = walls });
        doc.Entities.Add(new Line(new Vector3(0, 200, 0), new Vector3(3000, 200, 0)) { Layer = walls });
        doc.Entities.Add(new Circle(new Vector3(5000, 0, 0), 200) { Layer = new Layer(columnLayer) });

        string path = Path.Combine(_dir, $"{wallLayer}.dxf");
        doc.Save(path);
        return path;
    }
}
