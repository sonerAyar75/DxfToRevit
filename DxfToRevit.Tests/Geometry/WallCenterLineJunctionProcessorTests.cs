
using DxfToRevit.Core.Geometry;
using Xunit;
using netDxf;
namespace DxfToRevit.Tests.Core.Geometry;

/// <summary>
/// L/T junction processor scenarios on several centerlines.
/// </summary>
public sealed class WallCenterLineJunctionProcessorTests
{
    /// <summary>
    /// Constructor_CreatesProcessor: Model creation.
    /// </summary>
    [Fact]
    public void Constructor_CreatesProcessor()
    {
        var processor = new WallCenterLineJunctionProcessor();

        Assert.NotNull(processor);
    }


    /// <summary>
    /// Process_SampleCenterLines_ReceivesFiveLines: Real data from Sample.dxf.
    /// </summary>
    [Fact]
    public void Process_SampleCenterLines_ReceivesFiveLines()
    {
        var centerLines = new List<LineSegment2D>
    {
        // Bottom exterior wall
        new(
            new Vector2(200, 100),
            new Vector2(9688.25, 100)),

        // Left exterior wall
        new(
            new Vector2(100, 200),
            new Vector2(100, 6000)),

        // Top exterior wall
        new(
            new Vector2(200, 6100),
            new Vector2(9688.25, 6100)),

        // Right exterior wall
        new(
            new Vector2(9788.25, 200),
            new Vector2(9788.25, 6000)),

        // Interior wall
        new(
            new Vector2(4944.12, 200),
            new Vector2(4944.12, 6000))
    };

        Assert.Equal(5, centerLines.Count);
    }


    /// <summary>
    /// Process_Sample_LowerLeftLJunction_JoinsLines: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_Sample_LowerLeftLJunction_JoinsLines()
    {
        var centerLines = new List<LineSegment2D>
    {
        new(new Vector2(200, 100), new Vector2(9688.25, 100)),
        new(new Vector2(100, 200), new Vector2(100, 6000))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(2, result.Count);

        // Start of the bottom wall
        Assert.Equal(100, result[0].Start.X, 6);
        Assert.Equal(100, result[0].Start.Y, 6);

        // Start of the left wall
        Assert.Equal(100, result[1].Start.X, 6);
        Assert.Equal(100, result[1].Start.Y, 6);
    }


    /// <summary>
    /// Process_Sample_LowerRightLJunction_JoinsLines: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_Sample_LowerRightLJunction_JoinsLines()
    {
        var centerLines = new List<LineSegment2D>
    {
        new(new Vector2(200, 100), new Vector2(9688.25, 100)),
        new(new Vector2(9788.25, 200), new Vector2(9788.25, 6000))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(2, result.Count);

        // End of the bottom wall
        Assert.Equal(9788.25, result[0].End.X, 6);
        Assert.Equal(100, result[0].End.Y, 6);

        // Start of the right wall
        Assert.Equal(9788.25, result[1].Start.X, 6);
        Assert.Equal(100, result[1].Start.Y, 6);
    }


    /// <summary>
    /// Process_SampleCenterLines_ConnectsAllSixJunctions: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_SampleCenterLines_ConnectsAllSixJunctions()
    {
        var centerLines = new List<LineSegment2D>
    {
        new(new Vector2(200, 100), new Vector2(9688.25, 100)),
        new(new Vector2(100, 200), new Vector2(100, 6000)),
        new(new Vector2(200, 6100), new Vector2(9688.25, 6100)),
        new(new Vector2(9788.25, 200), new Vector2(9788.25, 6000)),
        new(new Vector2(4944.12, 200), new Vector2(4944.12, 6000))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(5, result.Count);

        // Bottom wall
        AssertLine(result[0], 100, 100, 9788.25, 100);

        // Left wall
        AssertLine(result[1], 100, 100, 100, 6100);

        // Top wall
        AssertLine(result[2], 100, 6100, 9788.25, 6100);

        // Right wall
        AssertLine(result[3], 9788.25, 100, 9788.25, 6100);

        // Internal wall
        AssertLine(result[4], 4944.12, 100, 4944.12, 6100);
    }

    private static void AssertLine(
        LineSegment2D line,
        double startX,
        double startY,
        double endX,
        double endY)
    {
        const int precision = 2;

        Assert.Equal(startX, line.Start.X, precision);
        Assert.Equal(startY, line.Start.Y, precision);
        Assert.Equal(endX, line.End.X, precision);
        Assert.Equal(endY, line.End.Y, precision);
    }


    /// <summary>
    /// Process_ShuffledCenterLines_ConnectsAllSixJunctions: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_ShuffledCenterLines_ConnectsAllSixJunctions()
    {
        var centerLines = new List<LineSegment2D>
    {
        new(new Vector2(4944.12, 200), new Vector2(4944.12, 6000)),
        new(new Vector2(9788.25, 200), new Vector2(9788.25, 6000)),
        new(new Vector2(200, 6100), new Vector2(9688.25, 6100)),
        new(new Vector2(100, 200), new Vector2(100, 6000)),
        new(new Vector2(200, 100), new Vector2(9688.25, 100))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(5, result.Count);

        // Internal wall
        AssertLine(result[0], 4944.12, 100, 4944.12, 6100);

        // Right wall
        AssertLine(result[1], 9788.25, 100, 9788.25, 6100);

        // Top wall
        AssertLine(result[2], 100, 6100, 9788.25, 6100);

        // Left wall
        AssertLine(result[3], 100, 100, 100, 6100);

        // Bottom wall
        AssertLine(result[4], 100, 100, 9788.25, 100);
    }


    /// <summary>
    /// Process_LJunctionExceedsMaxDistance_DoesNotExtendLines: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_LJunctionExceedsMaxDistance_DoesNotExtendLines()
    {
        var centerLines = new List<LineSegment2D>
    {
        // Horizontal line ends 200 mm before intersection.
        new(new Vector2(0, 0), new Vector2(800, 0)),

        // Vertical line starts 200 mm after intersection.
        new(new Vector2(1000, 200), new Vector2(1000, 1000))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(2, result.Count);

        // Neither line should be extended.
        AssertLine(result[0], 0, 0, 800, 0);
        AssertLine(result[1], 1000, 200, 1000, 1000);
    }


    /// <summary>
    /// Process_TJunctionExceedsMaxDistance_DoesNotExtendBranch: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_TJunctionExceedsMaxDistance_DoesNotExtendBranch()
    {
        var centerLines = new List<LineSegment2D>
    {
        // Main horizontal wall
        new(new Vector2(0, 0), new Vector2(1000, 0)),

        // Branch vertical wall ends 200 mm before main wall
        new(new Vector2(500, 200), new Vector2(500, 1000))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(2, result.Count);

        // Main wall remains unchanged.
        AssertLine(result[0], 0, 0, 1000, 0);

        // Branch wall remains unchanged.
        AssertLine(result[1], 500, 200, 500, 1000);
    }


    /// <summary>
    /// Process_IntersectionNotOnBothLines_DoesNotJoinLines: Line intersection.
    /// </summary>
    [Fact]
    public void Process_IntersectionNotOnBothLines_DoesNotJoinLines()
    {
        var centerLines = new List<LineSegment2D>
    {
        // Horizontal line
        new(new Vector2(0, 0), new Vector2(1000, 0)),

        // Slightly inclined line
        // Its infinite line intersects the horizontal line,
        // but the intersection is outside this segment.
        new(new Vector2(1200, 200), new Vector2(1500, 500))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(2, result.Count);

        AssertLine(result[0], 0, 0, 1000, 0);
        AssertLine(result[1], 1200, 200, 1500, 500);
    }


    /// <summary>
    /// Process_IntersectionInsideBothSegments_DoesNotModifyLines: Line intersection.
    /// </summary>
    [Fact]
    public void Process_IntersectionInsideBothSegments_DoesNotModifyLines()
    {
        var centerLines = new List<LineSegment2D>
    {
        // Horizontal wall
        new(new Vector2(0, 500), new Vector2(1000, 500)),

        // Vertical wall
        new(new Vector2(500, 0), new Vector2(500, 1000))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(2, result.Count);

        AssertLine(result[0], 0, 500, 1000, 500);
        AssertLine(result[1], 500, 0, 500, 1000);
    }

    /// <summary>
    /// Process_Inclined45DegreeLJunction_ConnectsLines: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_Inclined45DegreeLJunction_ConnectsLines()
    {
        var centerLines = new List<LineSegment2D>
    {
        // Horizontal wall: ends 100 mm before intersection.
        new(new Vector2(0, 0), new Vector2(900, 0)),

        // 45-degree wall: starts at (1000, 100).
        new(new Vector2(1000, 100), new Vector2(2000, 1100))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(2, result.Count);

        AssertLine(result[0], 0, 0, 900, 0);
        AssertLine(result[1], 900, 0, 2000, 1100);
    }


    /// <summary>
    /// Process_Inclined45DegreeLJunction_ExtendsBothLines: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_Inclined45DegreeLJunction_ExtendsBothLines()
    {
        var centerLines = new List<LineSegment2D>
    {
        // Horizontal wall: 100 mm extension required.
        new(new Vector2(0, 100), new Vector2(800, 100)),

        // 45-degree wall: approximately 141.42 mm extension required.
        new(new Vector2(1000, 200), new Vector2(2000, 1200))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(2, result.Count);

        // Horizontal wall
        AssertLine(result[0], 0, 100, 900, 100);

        // Inclined wall
        AssertLine(result[1], 900, 100, 2000, 1200);
    }


    /// <summary>
    /// Process_Inclined45DegreeTJunction_ExtendsBranch: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_Inclined45DegreeTJunction_ExtendsBranch()
    {
        var centerLines = new List<LineSegment2D>
    {
        // Main horizontal wall
        new(new Vector2(0, 100), new Vector2(1000, 100)),

        // 45-degree branch wall
        new(new Vector2(700, 200), new Vector2(1200, 700))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(2, result.Count);

        // Main wall remains unchanged.
        AssertLine(result[0], 0, 100, 1000, 100);

        // Branch extends to the main wall.
        AssertLine(result[1], 600, 100, 1200, 700);
    }



    /// <summary>
    /// Process_Inclined60DegreeLJunction_ExtendsBothLines: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_Inclined60DegreeLJunction_ExtendsBothLines()
    {
        var centerLines = new List<LineSegment2D>
    {
        // Horizontal wall
        new(new Vector2(0, 100), new Vector2(800, 100)),

        // 60-degree inclined wall
        new(new Vector2(950, 186.6025404),
        new Vector2(1450, 1052.627944)) };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(2, result.Count);

        AssertLine(result[0], 0, 100, 900, 100);

        AssertLine(result[1], 900, 100,
            1450, 1052.627944);
    }


    /// <summary>
    /// Process_Inclined30DegreeLJunction_ExtendsBothLines: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_Inclined30DegreeLJunction_ExtendsBothLines()
    {
        var centerLines = new List<LineSegment2D>
        {
            // Horizontal wall: 100 mm before intersection.
            new(new Vector2(0, 100), new Vector2(900, 100)),

            // 30-degree inclined wall: 100 mm before intersection.
            new(new Vector2(1086.6025404, 150),
                new Vector2(1913.3974596, 627.3502692))
        };



        var intersectionCalculator = new LineIntersectionCalculator();

        Console.WriteLine($"Line A Start: {centerLines[0].Start}");
        Console.WriteLine($"Line A End: {centerLines[0].End}");
        Console.WriteLine($"Line A Direction: {centerLines[0].Direction}");

        Console.WriteLine($"Line B Start: {centerLines[1].Start}");
        Console.WriteLine($"Line B End: {centerLines[1].End}");
        Console.WriteLine($"Line B Direction: {centerLines[1].Direction}");

        Console.WriteLine($"Direction A Length: {centerLines[0].Direction.Modulus()}");
        Console.WriteLine($"Direction B Length: {centerLines[1].Direction.Modulus()}");


        bool found = intersectionCalculator.TryFindIntersection(
            centerLines[0],
            centerLines[1],
            out Vector2 intersection);

        Assert.True(found);

        Console.WriteLine($"Intersection after method call: {intersection}");

        Assert.Equal(1000, intersection.X, 2);
        Assert.Equal(100, intersection.Y, 2);



        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Console.WriteLine("Processed lines:");

        for (int i = 0; i < result.Count; i++)
        {
            Console.WriteLine(
                $"Line {i}: Start={result[i].Start}, End={result[i].End}");
        }

        Assert.Equal(2, result.Count);

        AssertLine(result[0], 0, 100, 1000, 100);

        AssertLine(result[1], 1000, 100,
            1913.3974596, 627.3502692);
    }

    /// <summary>
    /// Process_Inclined30DegreeTJunction_ExtendsBranch: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_Inclined30DegreeTJunction_ExtendsBranch()
    {
        var centerLines = new List<LineSegment2D>
    {
        // Main horizontal wall
        new(new Vector2(0, 100), new Vector2(1000, 100)),

        // 30-degree branch wall.
        // Its endpoint is 100 mm before the intersection.
        new(new Vector2(80.3974596, -200),
            new Vector2(513.3974596, 50))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(2, result.Count);

        // Main wall remains unchanged.
        AssertLine(result[0], 0, 100, 1000, 100);

        // Branch extends to the main wall.
        AssertLine(
            result[1],
            80.3974596,
            -200,
            600,
            100);
    }

    /// <summary>
    /// Process_Inclined120DegreeLJunction_ExtendsBothWalls: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_Inclined120DegreeLJunction_ExtendsBothWalls()
    {
        var centerLines = new List<LineSegment2D>
    {
        // Main horizontal wall: ends 100 mm before intersection
        new(new Vector2(0, 100), new Vector2(400, 100)),

        // Inclined wall: also ends 100 mm before intersection
        new(new Vector2(550, 13.3974596),
            new Vector2(1000, -766.0254038))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(2, result.Count);

        // Main wall
        AssertLine(result[0], 0, 100, 500, 100);

        // Inclined wall
        AssertLine(
            result[1],
            500,
            100,
            1000,
            -766.0254038);
    }

    /// <summary>
    /// Process_Inclined120DegreeTJunction_ExtendsBranch: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_Inclined120DegreeTJunction_ExtendsBranch()
    {
        var centerLines = new List<LineSegment2D>
    {
        // Main horizontal wall
        new(new Vector2(0, 100), new Vector2(1000, 100)),

        // 120-degree inclined branch
        new(new Vector2(550, 13.3974596),
            new Vector2(1000, -766.0254038))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(2, result.Count);

        // Main wall remains unchanged.
        AssertLine(result[0], 0, 100, 1000, 100);

        // Branch extends to the main wall.
        AssertLine(
            result[1],
            500,
            100,
            1000,
            -766.0254038);
    }

    /// <summary>
    /// Process_Inclined150DegreeLJunction_ExtendsBothWalls: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_Inclined150DegreeLJunction_ExtendsBothWalls()
    {
        var centerLines = new List<LineSegment2D>
    {
        // Main horizontal wall
        new(new Vector2(0, 100), new Vector2(400, 100)),

        // 150-degree inclined wall
        new(new Vector2(586.6025404, 50),
            new Vector2(1019.6152423, -200))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(2, result.Count);

        // Main wall extends to the intersection.
        AssertLine(result[0], 0, 100, 500, 100);

        // Inclined wall extends to the same intersection.
        AssertLine(
            result[1],
            500,
            100,
            1019.6152423,
            -200);
    }

    /// <summary>
    /// Process_Inclined150DegreeTJunction_ExtendsBranch: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_Inclined150DegreeTJunction_ExtendsBranch()
    {
        var centerLines = new List<LineSegment2D>
    {
        // Main horizontal wall
        new(new Vector2(0, 100), new Vector2(1000, 100)),

        // 150-degree inclined branch
        new(new Vector2(586.6025404, 50),
            new Vector2(1019.6152423, -200))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(2, result.Count);

        // Main wall remains unchanged.
        AssertLine(result[0], 0, 100, 1000, 100);

        // Branch extends to the main wall.
        AssertLine(
            result[1],
            500,
            100,
            1019.6152423,
            -200);
    }

    /// <summary>
    /// Process_ExceedsMaximumExtension_DoesNotJoinWalls: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_ExceedsMaximumExtension_DoesNotJoinWalls()
    {
        var centerLines = new List<LineSegment2D>
    {
        // Main horizontal wall
        new(new Vector2(0, 100), new Vector2(300, 100)),

        // Inclined wall
        new(new Vector2(500, 100), new Vector2(700, 300))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(2, result.Count);

        // Both walls must remain unchanged.
        AssertLine(result[0], 0, 100, 300, 100);

        AssertLine(
            result[1],
            500,
            100,
            700,
            300);
    }

    /// <summary>
    /// Process_RandomInclinedLJunctions_ExtendsBothWalls: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_RandomInclinedLJunctions_ExtendsBothWalls()
    {
        var random = new Random(20261030);
        var processor = new WallCenterLineJunctionProcessor();

        for (int i = 0; i < 100; i++)
        {
            double angleDegrees =
                15 + random.NextDouble() * 150;

            double angleRadians =
                angleDegrees * Math.PI / 180.0;

            double dx = Math.Cos(angleRadians);
            double dy = Math.Sin(angleRadians);

            // Theoretical intersection
            double intersectionX = 500;
            double intersectionY = 100;

            var centerLines = new List<LineSegment2D>
        {
            // Main horizontal wall
            new(
                new Vector2(0, 100),
                new Vector2(400, 100)),

            // Randomly inclined wall
            new(
                new Vector2(
                    intersectionX + 100 * dx,
                    intersectionY + 100 * dy),

                new Vector2(
                    intersectionX + 600 * dx,
                    intersectionY + 600 * dy))
        };

            var result = processor.Process(centerLines, 150);

            Assert.Equal(2, result.Count);

            AssertLine(result[0], 0, 100, 500, 100);

            AssertLine(
                result[1],
                500,
                100,
                intersectionX + 600 * dx,
                intersectionY + 600 * dy);
        }
    }

    /// <summary>
    /// Process_RandomInclinedTJunctions_ExtendsBranches: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_RandomInclinedTJunctions_ExtendsBranches()
    {
        var random = new Random(20261031);
        var processor = new WallCenterLineJunctionProcessor();

        for (int i = 0; i < 100; i++)
        {
            double angleDegrees =
                15 + random.NextDouble() * 150;

            double angleRadians =
                angleDegrees * Math.PI / 180.0;

            double dx = Math.Cos(angleRadians);
            double dy = Math.Sin(angleRadians);

            // Theoretical intersection
            double intersectionX = 500;
            double intersectionY = 100;

            var centerLines = new List<LineSegment2D>
        {
            // Main horizontal wall
            new(
                new Vector2(0, 100),
                new Vector2(1000, 100)),

            // Randomly inclined branch
            new(
                new Vector2(
                    intersectionX + 100 * dx,
                    intersectionY + 100 * dy),

                new Vector2(
                    intersectionX + 600 * dx,
                    intersectionY + 600 * dy))
        };

            var result = processor.Process(centerLines, 150);

            Assert.Equal(2, result.Count);

            // Main wall remains unchanged.
            AssertLine(result[0], 0, 100, 1000, 100);

            // Branch extends to the intersection.
            AssertLine(
                result[1],
                intersectionX,
                intersectionY,
                intersectionX + 600 * dx,
                intersectionY + 600 * dy);
        }
    }

    /// <summary>
    /// Process_ShuffledCenterLines_ProducesSameGeometry: branch extension scenario.
    /// </summary>
    [Fact]
    public void Process_ShuffledCenterLines_ProducesSameGeometry()
    {
        var processor = new WallCenterLineJunctionProcessor();

        var centerLines = new List<LineSegment2D>
    {
        // Bottom horizontal wall
        new(new Vector2(0, 100), new Vector2(400, 100)),

        // Left vertical wall
        new(new Vector2(100, 200), new Vector2(100, 600)),

        // Top horizontal wall
        new(new Vector2(0, 700), new Vector2(400, 700)),

        // Right vertical wall
        new(new Vector2(500, 200), new Vector2(500, 600))
    };

        var shuffledCenterLines = new List<LineSegment2D>
    {
        centerLines[2],
        centerLines[0],
        centerLines[3],
        centerLines[1]
    };

        var result1 = processor.Process(centerLines, 150);
        var result2 = processor.Process(shuffledCenterLines, 150);

        Assert.Equal(result1.Count, result2.Count);

        // Compare geometries independently of list order.
        foreach (var line in result1)
        {
            Assert.Contains(result2, candidate =>
                (
                    Vector2.Distance(line.Start, candidate.Start) < 0.01 &&
                    Vector2.Distance(line.End, candidate.End) < 0.01
                )
                ||
                (
                    Vector2.Distance(line.Start, candidate.End) < 0.01 &&
                    Vector2.Distance(line.End, candidate.Start) < 0.01
                ));
        }
    }

    /// <summary>
    /// Process_ReversedLineDirections_ProducesSameGeometry: branch extension scenario.
    /// </summary>
    [Fact]
    public void Process_ReversedLineDirections_ProducesSameGeometry()
    {
        var processor = new WallCenterLineJunctionProcessor();

        var centerLines = new List<LineSegment2D>
    {
        // Main horizontal wall
        new(new Vector2(0, 100), new Vector2(400, 100)),

        // Inclined wall
        new(new Vector2(550, 13.3974596),
            new Vector2(1000, -766.0254038))
    };

        var reversedCenterLines = new List<LineSegment2D>
    {
        // Both lines have reversed directions
        new(new Vector2(400, 100), new Vector2(0, 100)),

        new(new Vector2(1000, -766.0254038),
            new Vector2(550, 13.3974596))
    };

        var result1 = processor.Process(centerLines, 150);
        var result2 = processor.Process(reversedCenterLines, 150);

        Assert.Equal(result1.Count, result2.Count);

        foreach (var line in result1)
        {
            Assert.Contains(result2, candidate =>
                (
                    Vector2.Distance(line.Start, candidate.Start) < 0.01 &&
                    Vector2.Distance(line.End, candidate.End) < 0.01
                )
                ||
                (
                    Vector2.Distance(line.Start, candidate.End) < 0.01 &&
                    Vector2.Distance(line.End, candidate.Start) < 0.01
                ));
        }
    }

    /// <summary>
    /// Process_InclinedTJunction_RequiresMoreThan150mmExtension: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_InclinedTJunction_RequiresMoreThan150mmExtension()
    {
        var centerLines = new List<LineSegment2D>
    {
        // Main horizontal centerline
        new(
            new Vector2(-2034.365, 3052.678),
            new Vector2(2034.365, 3052.678)),

        // Inclined branch centerline
        new(
            new Vector2(-1123.976, 3170.979),
            new Vector2(1.024, 3820.498))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 150);

        Assert.Equal(2, result.Count);

        // 150 mm limit should prevent the extension.
        AssertLine(
            result[1],
            -1123.976,
            3170.979,
            1.024,
            3820.498);
    }

    /// <summary>
    /// Process_InclinedTJunction_250mmExtension_ConnectsLines: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_InclinedTJunction_250mmExtension_ConnectsLines()
    {
        var centerLines = new List<LineSegment2D>
    {
        // Main horizontal centerline
        new(
            new Vector2(-2034.365, 3052.678),
            new Vector2(2034.365, 3052.678)),

        // Inclined branch centerline
        new(
            new Vector2(-1123.976, 3170.979),
            new Vector2(1.024, 3820.498))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 250);

        Assert.Equal(2, result.Count);

        // Main line remains unchanged.
        AssertLine(
            result[0],
            -2034.365,
            3052.678,
            2034.365,
            3052.678);

        // Inclined branch reaches the main line.
        AssertLine(
            result[1],
            -1328.879,
            3052.678,
            1.024,
            3820.498);
    }

    /// <summary>
    /// Process_InclinedTJunctionToVerticalWall_250mmExtension_ConnectsLines: L/T junction and endpoint extension.
    /// </summary>
    [Fact]
    public void Process_InclinedTJunctionToVerticalWall_250mmExtension_ConnectsLines()
    {
        var centerLines = new List<LineSegment2D>
    {
        // Main vertical centerline
        new(
            new Vector2(3052.678, -2034.365),
            new Vector2(3052.678, 2034.365)),

        // Inclined branch centerline
        new(
            new Vector2(3170.979, -1123.976),
            new Vector2(3820.498, 1.024))
    };

        var processor = new WallCenterLineJunctionProcessor();

        var result = processor.Process(centerLines, 250);

        Assert.Equal(2, result.Count);

        AssertLine(
            result[0],
            3052.678, -2034.365,
            3052.678, 2034.365);

        AssertLine(
            result[1],
            3052.678, -1328.879,
            3820.498, 1.024);
    }



}

