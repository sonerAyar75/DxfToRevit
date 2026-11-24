
using DxfToRevit.Core.Dxf;
using netDxf.Entities;
using netDxf;

namespace DxfToRevit.Tests.Dxf;

/// <summary>
/// DxfReader path validation and loading errors.
/// </summary>
public class DxfReaderTests
{
    private readonly DxfReader _reader = new();

    /// <summary>
    /// Read_EmptyPath_ThrowsArgumentException: Empty input scenario.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Read_EmptyPath_ThrowsArgumentException(
        string filePath)
    {
        Assert.Throws<ArgumentException>(() =>
            _reader.Read(filePath));
    }

    /// <summary>
    /// Read_NonexistentFile_ThrowsFileNotFoundException: File reading / import.
    /// </summary>
    [Fact]
    public void Read_NonexistentFile_ThrowsFileNotFoundException()
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"DxfToRevit_Missing_{Guid.NewGuid():N}.dxf");

        Assert.Throws<FileNotFoundException>(() =>
            _reader.Read(filePath));
    }

    /// <summary>
    /// Read_ValidDxfFile_LoadsLineEntity: File reading / import.
    /// </summary>
    [Fact]
    public void Read_ValidDxfFile_LoadsLineEntity()
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"DxfToRevit_Test_{Guid.NewGuid():N}.dxf");

        try
        {
            var sourceDocument = new DxfDocument();

            var line = new Line(
                new Vector3(0, 0, 0),
                new Vector3(5000, 0, 0));

            sourceDocument.Entities.Add(line);
            sourceDocument.Save(filePath);

            DxfDocument loadedDocument = _reader.Read(filePath);

            Assert.NotNull(loadedDocument);
            Assert.Single(loadedDocument.Entities.Lines);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

}

