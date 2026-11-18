


using DxfToRevit.Core.Dxf;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using Xunit;

namespace DxfToRevit.Tests.Dxf;

/// <summary>DxfGeometryReader: LINE entities become line models with their layer and handle.</summary>
public class DxfGeometryReaderTests
{
    private readonly DxfGeometryReader _reader = new();

    /// <summary>
    /// ReadLines_OneLine_ReturnsCorrectGeometryAndLayer: Layer/thickness resolution.
    /// </summary>
    [Fact]
    public void ReadLines_OneLine_ReturnsCorrectGeometryAndLayer()
    {
        var document = new DxfDocument();

        document.Entities.Add(
            new Line(
                new Vector3(0, 0, 0),
                new Vector3(5000, 0, 0))
            {
                Layer = new Layer("WALL-20")
            });

        var result = _reader.ReadLines(document);

        Assert.Single(result);

        Assert.Equal(5000, result[0].Geometry.Length);
        Assert.Equal(0, result[0].Geometry.Start.X);
        Assert.Equal(0, result[0].Geometry.Start.Y);
        Assert.Equal(5000, result[0].Geometry.End.X);
        Assert.Equal(0, result[0].Geometry.End.Y);

        Assert.Equal("WALL-20", result[0].SourceLayer);
    }

    /// <summary>
    /// ReadLines_MultipleLines_ReturnsAllLines: File reading / import.
    /// </summary>
    [Fact]
    public void ReadLines_MultipleLines_ReturnsAllLines()
    {
        var document = new DxfDocument();

        document.Entities.Add(
            new Line(
                new Vector3(0, 0, 0),
                new Vector3(5000, 0, 0)));

        document.Entities.Add(
            new Line(
                new Vector3(1000, 2000, 0),
                new Vector3(1000, 6000, 0)));

        var result = _reader.ReadLines(document);

        Assert.Equal(2, result.Count);
    }

    /// <summary>
    /// ReadLines_NoLines_ReturnsEmptyCollection: Empty input scenario.
    /// </summary>
    [Fact]
    public void ReadLines_NoLines_ReturnsEmptyCollection()
    {
        var document = new DxfDocument();

        var result = _reader.ReadLines(document);

        Assert.Empty(result);
    }

    /// <summary>
    /// ReadLines_NullDocument_ThrowsArgumentNullException: Null argument → exception.
    /// </summary>
    [Fact]
    public void ReadLines_NullDocument_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _reader.ReadLines(null!));
    }

    /// <summary>
    /// ReadLines_DifferentLayers_PreservesEachSourceLayer: Layer/thickness resolution.
    /// </summary>
    [Fact]
    public void ReadLines_DifferentLayers_PreservesEachSourceLayer()
    {
        var document = new DxfDocument();

        document.Entities.Add(
            new Line(
                new Vector3(0, 0, 0),
                new Vector3(5000, 0, 0))
            {
                Layer = new Layer("WALL-20")
            });

        document.Entities.Add(
            new Line(
                new Vector3(1000, 1000, 0),
                new Vector3(1000, 4000, 0))
            {
                Layer = new Layer("COLUMN")
            });

        var result = _reader.ReadLines(document);

        Assert.Equal(2, result.Count);

        Assert.Contains(
            result,
            item => item.SourceLayer == "WALL-20");

        Assert.Contains(
            result,
            item => item.SourceLayer == "COLUMN");
    }

}


