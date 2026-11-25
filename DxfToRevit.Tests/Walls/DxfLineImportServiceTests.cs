

using DxfToRevit.Core.Walls;

namespace DxfToRevit.Tests.Walls;

/// <summary>
/// Integration of importing a DXF file and classifying its lines.
/// </summary>
public sealed class DxfLineImportServiceTests
{
    /// <summary>
    /// Import_ValidDxfFile_ReturnsClassifiedLines: File reading / import.
    /// </summary>
    [Fact]
    public void Import_ValidDxfFile_ReturnsClassifiedLines()
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.dxf");

        try
        {
            var document = new netDxf.DxfDocument();

            var wallLayer = new netDxf.Tables.Layer("WALL-20");
            var columnLayer = new netDxf.Tables.Layer("COLUMN");

            document.Layers.Add(wallLayer);
            document.Layers.Add(columnLayer);

            document.Entities.Add(
                new netDxf.Entities.Line(
                    new netDxf.Vector3(0, 0, 0),
                    new netDxf.Vector3(1000, 0, 0))
                {
                    Layer = wallLayer
                });

            document.Entities.Add(
                new netDxf.Entities.Line(
                    new netDxf.Vector3(0, 100, 0),
                    new netDxf.Vector3(1000, 100, 0))
                {
                    Layer = columnLayer
                });

            document.Save(filePath);

            var service = new DxfLineImportService();

            IReadOnlyList<ClassifiedDxfLine> results =
                service.Import(filePath);

            Assert.Equal(2, results.Count);

            Assert.True(results[0].IsWallLayer);
            Assert.Equal(200.0, results[0].WallThicknessMm);
            Assert.Equal("WALL-20", results[0].SourceLayer);

            Assert.False(results[1].IsWallLayer);
            Assert.Null(results[1].WallThicknessMm);
            Assert.Equal("COLUMN", results[1].SourceLayer);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    /// <summary>
    /// Import_NonExistingFile_ThrowsFileNotFoundException: File reading / import.
    /// </summary>
    [Fact]
    public void Import_NonExistingFile_ThrowsFileNotFoundException()
    {
        var service = new DxfLineImportService();

        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.dxf");

        Assert.Throws<FileNotFoundException>(
            () => service.Import(filePath));
    }
}

