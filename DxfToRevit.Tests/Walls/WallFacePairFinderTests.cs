
using DxfToRevit.Core.Dxf;
using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Models;
using DxfToRevit.Core.Services;
using netDxf;
using netDxf.Tables;
using netDxf.Entities;
using DxfToRevit.Core.IO;

namespace DxfToRevit.Tests.Walls;

/// <summary>
/// Finding parallel face pairs: trimmed faces and ambiguity scenarios.
/// </summary>
public class WallFacePairFinderTests
{
    /// <summary>
    /// FindPairs_SameLayerParallelFaces_ReturnsOnePair: Parallelism / parallel distance check.
    /// </summary>
    [Fact]
    public void FindPairs_SameLayerParallelFaces_ReturnsOnePair()
    {
        var faceA = new ClassifiedDxfLine(
            new LineSegment2D(
                new Vector2(0, 0),
                new Vector2(1000, 0)),
            "WALL-20",
            true,
            200,"TEST001");

        var faceB = new ClassifiedDxfLine(
            new LineSegment2D(
                new Vector2(0, 200),
                new Vector2(1000, 200)),
            "WALL-20",
            true,
            200, "TEST001");

        var finder = new WallFacePairFinder();

        var result = finder.FindPairs(new[] { faceA, faceB });

        Assert.Single(result.Pairs);
        Assert.Empty(result.Ambiguities);

        Assert.Equal("WALL-20", result.Pairs[0].SourceLayer);
        Assert.Equal(200, result.Pairs[0].ThicknessMm);
    }

    /// <summary>
    /// FindPairs_SeparatedParallelLines_ReturnsNoPairs: Parallelism / parallel distance check.
    /// </summary>
    [Fact]
    public void FindPairs_SeparatedParallelLines_ReturnsNoPairs()
    {
        var lineA = new ClassifiedDxfLine(
            new LineSegment2D(
                new Vector2(0, 0),
                new Vector2(1000, 0)),
            "WALL-20",
            true,
            200, "TEST001");

        var lineB = new ClassifiedDxfLine(
            new LineSegment2D(
                new Vector2(1200, 200),
                new Vector2(1800, 200)),
            "WALL-20",
            true,
            200, "TEST001");

        var finder = new WallFacePairFinder();

        var result = finder.FindPairs(new[] { lineA, lineB });

        Assert.Empty(result.Pairs);
        Assert.Empty(result.Ambiguities);
    }

    /// <summary>
    /// FindPairs_ThreeLines_OnlyOnePairMatchesThickness: Layer/thickness resolution.
    /// </summary>
    [Fact]
    public void FindPairs_ThreeLines_OnlyOnePairMatchesThickness()
    {
        var lineA = new ClassifiedDxfLine(
            new LineSegment2D(
                new Vector2(0, 0),
                new Vector2(1000, 0)),
            "WALL-20",
            true,
            200, "TEST001");

        var lineB = new ClassifiedDxfLine(
            new LineSegment2D(
                new Vector2(0, 200),
                new Vector2(1000, 200)),
            "WALL-20",
            true,
            200, "TEST001");

        var lineC = new ClassifiedDxfLine(
            new LineSegment2D(
                new Vector2(0, 215),
                new Vector2(1000, 215)),
            "WALL-20",
            true,
            200, "TEST001");

        var finder = new WallFacePairFinder();

        var result = finder.FindPairs(new[] { lineA, lineB, lineC });

        Assert.Single(result.Pairs);
        Assert.Empty(result.Ambiguities);

        Assert.Equal("WALL-20", result.Pairs[0].SourceLayer);
        Assert.Equal(200, result.Pairs[0].ThicknessMm);
        Assert.Equal(
            200,
            result.Pairs[0].FaceA.GetDistanceToParallelLine(
                result.Pairs[0].FaceB),
            6);
    }

    /// <summary>
    /// FindPairs_ThreeEquallySpacedLines_ShouldReportAmbiguity: Multiple matches / ambiguity.
    /// </summary>
    [Fact]
    public void FindPairs_ThreeEquallySpacedLines_ShouldReportAmbiguity()
    {
        var lineA = new ClassifiedDxfLine(
            new LineSegment2D(
                new Vector2(0, 0),
                new Vector2(1000, 0)),
            "WALL-20",
            true,
            200, "TEST001");

        var lineB = new ClassifiedDxfLine(
            new LineSegment2D(
                new Vector2(0, 200),
                new Vector2(1000, 200)),
            "WALL-20",
            true,
            200, "TEST001");

        var lineC = new ClassifiedDxfLine(
            new LineSegment2D(
                new Vector2(0, 400),
                new Vector2(1000, 400)),
            "WALL-20",
            true,
            200, "TEST001");

        var finder = new WallFacePairFinder();

        var result = finder.FindPairs(
            new[] { lineA, lineB, lineC });

        Assert.Empty(result.Pairs);
        Assert.NotEmpty(result.Ambiguities);

        Assert.Contains(
            lineA.Handle,
            string.Join(" ", result.Ambiguities));

        Assert.Contains(
            lineB.Handle,
            string.Join(" ", result.Ambiguities));

        Assert.Contains(
            lineC.Handle,
            string.Join(" ", result.Ambiguities));
    }

    /// <summary>
    /// FindPairs_SampleFile_ShouldReportResults: Real data from Sample.dxf.
    /// </summary>
    [Fact]
    public void FindPairs_SampleFile_ShouldReportResults()
    {
        string filePath = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "Sample.dxf");

        var document = new DxfReader().Read(filePath);

        var geometryReader = new DxfGeometryReader();
        var lines = geometryReader.ReadLines(document);

        var classificationService =
            new DxfLineClassificationService();

        var classifiedLines =
            classificationService.Classify(lines);

        var finder = new WallFacePairFinder();

        var result = finder.FindPairs(classifiedLines);

        Console.WriteLine(
            $"Total DXF lines: {lines.Count}");

        Console.WriteLine(
            $"Accepted wall face pairs: {result.Pairs.Count}");

        Console.WriteLine(
            $"Ambiguities: {result.Ambiguities.Count}");


        foreach (var pair in result.Pairs)
        {
            Console.WriteLine(
                $"Layer: {pair.SourceLayer}, " +
                $"Thickness: {pair.ThicknessMm} mm");

            Console.WriteLine(
                $"Face A [{pair.FaceAHandle}]: " +
                $"({pair.FaceA.Start.X:F2}, {pair.FaceA.Start.Y:F2}) -> " +
                $"({pair.FaceA.End.X:F2}, {pair.FaceA.End.Y:F2})");

            Console.WriteLine(
                $"Face B [{pair.FaceBHandle}]: " +
                $"({pair.FaceB.Start.X:F2}, {pair.FaceB.Start.Y:F2}) -> " +
                $"({pair.FaceB.End.X:F2}, {pair.FaceB.End.Y:F2})");

            Console.WriteLine(
                $"Distance: " +
                $"{pair.FaceA.GetDistanceToParallelLine(pair.FaceB):F2} mm");

            Console.WriteLine();
        }


        foreach (var ambiguity in result.Ambiguities)
        {
            Console.WriteLine(ambiguity);
        }


        Assert.NotNull(result);

        var wallManager = new WallModelManager();
        var centerLines = result.Pairs
            .Select((pair, i) => wallManager.CreateFromFacePair($"W{i+1}", pair).CenterLine!)
            .ToList();

        Console.WriteLine(
            $"Original centerline count: {centerLines.Count}");

        // Process L and T junctions
        var junctionProcessor = new WallCenterLineJunctionProcessor();

        var processedCenterLines = junctionProcessor.Process(
            centerLines,
            250);

        Console.WriteLine(
            $"Processed centerline count: {processedCenterLines.Count}");

        Assert.Equal(centerLines.Count, processedCenterLines.Count);

    }

    /// <summary>
    /// FindPairs_TrimmedWallFace_ReturnsTwoNonOverlappingPairs: Overlap interval or overlapping segment.
    /// </summary>
    [Fact]
    public void FindPairs_TrimmedWallFace_ReturnsTwoNonOverlappingPairs()
    {
        var faceA = new ClassifiedDxfLine(
            new LineSegment2D(
                new Vector2(0, 0),
                new Vector2(1000, 0)),
            "WALL-20",
            true,
            200,
            "MAIN-A");

        var faceB = new ClassifiedDxfLine(
            new LineSegment2D(
                new Vector2(0, 200),
                new Vector2(400, 200)),
            "WALL-20",
            true,
            200,
            "MAIN-B1");

        var faceC = new ClassifiedDxfLine(
            new LineSegment2D(
                new Vector2(600, 200),
                new Vector2(1000, 200)),
            "WALL-20",
            true,
            200,
            "MAIN-B2");

        var finder = new WallFacePairFinder();

        var result = finder.FindPairs(
            new[] { faceA, faceB, faceC });

        Assert.Equal(2, result.Pairs.Count);
        Assert.Empty(result.Ambiguities);
    }



}

