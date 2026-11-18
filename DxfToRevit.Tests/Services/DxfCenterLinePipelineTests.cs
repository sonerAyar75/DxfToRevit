using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Rules;
using DxfToRevit.Core.Services;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using Xunit;

namespace DxfToRevit.Tests.Services;

/// <summary>
/// Pipeline: Analyze has no side effects, Run writes the output; collinear walls of different thickness stay separate.
/// </summary>
public sealed class DxfCenterLinePipelineTests : IDisposable
{
    private readonly string _dir;

    public DxfCenterLinePipelineTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "DxfToRevitTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort temp cleanup */ }
    }

    /// <summary>
    /// Two walls end to end on the same axis (y = 100): 0→3000 WALL-20 (200 mm), 3000→5000 WALL-10 (100 mm).
    /// </summary>
    private string CreateCollinearMixedThicknessDxf()
    {
        var doc = new DxfDocument();
        var wall20 = new Layer("WALL-20");
        var wall10 = new Layer("WALL-10");

        doc.Entities.Add(new Line(new Vector3(0, 0, 0), new Vector3(3000, 0, 0)) { Layer = wall20 });
        doc.Entities.Add(new Line(new Vector3(0, 200, 0), new Vector3(3000, 200, 0)) { Layer = wall20 });
        doc.Entities.Add(new Line(new Vector3(3000, 50, 0), new Vector3(5000, 50, 0)) { Layer = wall10 });
        doc.Entities.Add(new Line(new Vector3(3000, 150, 0), new Vector3(5000, 150, 0)) { Layer = wall10 });

        string path = Path.Combine(_dir, "mixed.dxf");
        doc.Save(path);
        return path;
    }

    [Fact]
    public void Analyze_DoesNotWriteOutputFile()
    {
        string input = CreateCollinearMixedThicknessDxf();

        var result = new DxfCenterLinePipeline().Analyze(input);

        Assert.Null(result.OutputPath);
        Assert.Single(Directory.GetFiles(_dir));
        Assert.Equal(2, result.WallSolidCount);
    }

    [Fact]
    public void Run_WritesOutputFile()
    {
        string input = CreateCollinearMixedThicknessDxf();

        var result = new DxfCenterLinePipeline().Run(input);

        Assert.NotNull(result.OutputPath);
        Assert.True(File.Exists(result.OutputPath));
    }

    [Fact]
    public void Analyze_CollinearWallsWithDifferentThickness_StaySeparate()
    {
        string input = CreateCollinearMixedThicknessDxf();

        var result = new DxfCenterLinePipeline().Analyze(input);

        var thicknesses = result.WallSolids.Select(s => s.ThicknessMm).OrderBy(t => t).ToList();
        Assert.Equal(new[] { 100.0, 200.0 }, thicknesses);
    }

    [Fact]
    public void Merger_WithThickness_DoesNotMergeDifferentThicknesses()
    {
        var a = new LineSegment2D(new Vector2(0, 0), new Vector2(100, 0));
        var b = new LineSegment2D(new Vector2(100, 0), new Vector2(250, 0));

        var result = new CollinearCenterLineMerger().Merge(new[] { a, b }, new[] { 200.0, 100.0 });

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Merger_WithThickness_MergesSameThickness()
    {
        var a = new LineSegment2D(new Vector2(0, 0), new Vector2(100, 0));
        var b = new LineSegment2D(new Vector2(100, 0), new Vector2(250, 0));

        var result = new CollinearCenterLineMerger().Merge(new[] { a, b }, new[] { 200.0, 200.0 });

        Assert.Single(result);
        Assert.Equal(200.0, result[0].ThicknessMm);
        Assert.Equal(250.0, result[0].Segment.Length, 6);
    }

    [Theory]
    [InlineData("WALL", true)]
    [InlineData("wall-20", true)]
    [InlineData("  WALL-20  ", true)]
    [InlineData("WALL-EXT", true)]
    [InlineData("WALLPAPER", false)]
    [InlineData("WALLS", false)]
    [InlineData("", false)]
    public void WallLayerRule_IsWallLayer(string layer, bool expected)
    {
        Assert.Equal(expected, new WallLayerRule().IsWallLayer(layer));
    }
}
