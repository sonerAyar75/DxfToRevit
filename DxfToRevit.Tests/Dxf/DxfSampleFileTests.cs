

using DxfToRevit.Core.Dxf;
using DxfToRevit.Core.Walls;

namespace DxfToRevit.Tests.Dxf;

/// <summary>
/// Reading Sample.dxf (real data) and checking its layers.
/// </summary>
public class DxfSampleFileTests
{
    /// <summary>
    /// Read_SampleFile_ShouldLoadDocument: Real data from Sample.dxf.
    /// </summary>
    [Fact]
    public void Read_SampleFile_ShouldLoadDocument()
    {
        string filePath = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "Sample.dxf");

        Assert.True(
            File.Exists(filePath),
            $"DXF file was not found: {filePath}");

        var reader = new DxfReader();

        var document = reader.Read(filePath);

        Assert.NotNull(document);
    }

    /// <summary>
    /// ReadLines_SampleFile_ShouldReadWallLayers: Layer/thickness resolution.
    /// </summary>
    [Fact]
    public void ReadLines_SampleFile_ShouldReadWallLayers()
    {
        string filePath = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "Sample.dxf");

        var document = new DxfReader().Read(filePath);

        var geometryReader = new DxfGeometryReader();
        IReadOnlyList<DxfLineModel> lines =
            geometryReader.ReadLines(document);

        Assert.NotEmpty(lines);

        Assert.Contains(
            lines,
            line => string.Equals(
                line.SourceLayer,
                "wall-20",
                StringComparison.OrdinalIgnoreCase));

        Assert.Contains(
            lines,
            line => string.Equals(
                line.SourceLayer,
                "Wall-15",
                StringComparison.OrdinalIgnoreCase));

        Assert.All(lines, line =>
        {
            Assert.False(
                string.IsNullOrWhiteSpace(line.Handle));
        });
    }

    /// <summary>
    /// Classify_SampleFile_ShouldRecognizeWallThicknesses: Layer/thickness resolution.
    /// </summary>
    [Fact]
    public void Classify_SampleFile_ShouldRecognizeWallThicknesses()
    {
        
        string filePath = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "Sample.dxf");

        var document = new DxfReader().Read(filePath);
        var geometryReader = new DxfGeometryReader();
        var lines = geometryReader.ReadLines(document);

        var classificationService = new DxfLineClassificationService();
        var classifiedLines = classificationService.Classify(lines);

        Assert.Equal(lines.Count, classifiedLines.Count);

        for (int i = 0; i < lines.Count; i++)
        {
            Assert.Equal(
                lines[i].Handle,
                classifiedLines[i].Handle);
        }


        var wall20Lines = classifiedLines
            .Where(line => string.Equals(
                line.SourceLayer,
                "wall-20",
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.NotEmpty(wall20Lines);

        Assert.All(wall20Lines, line =>
        {
            Assert.True(line.IsWallLayer);
            Assert.Equal(200.0, line.WallThicknessMm);
        });

        var wall15Lines = classifiedLines
            .Where(line => string.Equals(
                line.SourceLayer,
                "Wall-15",
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.NotEmpty(wall15Lines);

        Assert.All(wall15Lines, line =>
        {
            Assert.True(line.IsWallLayer);
            Assert.Equal(150.0, line.WallThicknessMm);
        });
    }

    /// <summary>
    /// ReadLines_SampleFile_ShouldHaveValidLineGeometry: Real data from Sample.dxf.
    /// </summary>
    [Fact]
    public void ReadLines_SampleFile_ShouldHaveValidLineGeometry()
    {
        string filePath = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "Sample.dxf");

        var document = new DxfReader().Read(filePath);

        var geometryReader = new DxfGeometryReader();
        var lines = geometryReader.ReadLines(document);

        Assert.NotEmpty(lines);

        Assert.All(lines, line =>
        {
            Assert.False(line.Geometry.IsDegenerate);
            Assert.True(line.Geometry.Length > 0);
        });
    }

    /// <summary>
    /// ReadLines_SampleFile_ShouldReportWallLayerGeometry: Layer/thickness resolution.
    /// </summary>
    [Fact]
    public void ReadLines_SampleFile_ShouldReportWallLayerGeometry()
    {
        string filePath = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "Sample.dxf");

        var document = new DxfReader().Read(filePath);

        var geometryReader = new DxfGeometryReader();
        var lines = geometryReader.ReadLines(document);

        var wallLines = lines
            .Where(line =>
                string.Equals(
                    line.SourceLayer,
                    "wall-20",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    line.SourceLayer,
                    "Wall-15",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.NotEmpty(wallLines);

        foreach (var line in wallLines)
        {
            string report =
                $"Layer: {line.SourceLayer}, " +
                $"Start: ({line.Geometry.Start.X:F2}, {line.Geometry.Start.Y:F2}), " +
                $"End: ({line.Geometry.End.X:F2}, {line.Geometry.End.Y:F2}), " +
                $"Length: {line.Geometry.Length:F2}";

            Console.WriteLine(report);
        }
    }

}

