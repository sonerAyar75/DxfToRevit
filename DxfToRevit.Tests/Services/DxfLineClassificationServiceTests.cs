
using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Models;
using DxfToRevit.Core.Services;
using netDxf;
using Xunit;

namespace DxfToRevit.Tests.Services;

/// <summary>
/// Batch line classification service.
/// </summary>
public sealed class DxfLineClassificationServiceTests
{
    private static DxfLineModel CreateLine(string layerName)
    {
        var geometry = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        return new DxfLineModel(geometry, layerName, "TEST001");
    }

    /// <summary>
    /// Classify_MultipleLines_ReturnsClassificationForEachLine: Valid input / happy path.
    /// </summary>
    [Fact]
    public void Classify_MultipleLines_ReturnsClassificationForEachLine()
    {
        var service = new DxfLineClassificationService();

        var lines = new List<DxfLineModel>
        {
            CreateLine("WALL-15"),
            CreateLine("WALL-20"),
            CreateLine("COLUMN"),
            CreateLine("WALL")
        };

        IReadOnlyList<ClassifiedDxfLine> results =
            service.Classify(lines);

        Assert.Equal(4, results.Count);

        Assert.True(results[0].IsWallLayer);
        Assert.Equal(150.0, results[0].WallThicknessMm);

        Assert.True(results[1].IsWallLayer);
        Assert.Equal(200.0, results[1].WallThicknessMm);

        Assert.False(results[2].IsWallLayer);
        Assert.Null(results[2].WallThicknessMm);

        Assert.True(results[3].IsWallLayer);
        Assert.Null(results[3].WallThicknessMm);
    }

    /// <summary>
    /// Classify_EmptyCollection_ReturnsEmptyResult: Empty input scenario.
    /// </summary>
    [Fact]
    public void Classify_EmptyCollection_ReturnsEmptyResult()
    {
        var service = new DxfLineClassificationService();

        IReadOnlyList<ClassifiedDxfLine> results =
            service.Classify(Array.Empty<DxfLineModel>());

        Assert.Empty(results);
    }

    /// <summary>
    /// Classify_NullCollection_ThrowsArgumentNullException: Null argument → exception.
    /// </summary>
    [Fact]
    public void Classify_NullCollection_ThrowsArgumentNullException()
    {
        var service = new DxfLineClassificationService();

        Assert.Throws<ArgumentNullException>(
            () => service.Classify(null!));
    }
}

