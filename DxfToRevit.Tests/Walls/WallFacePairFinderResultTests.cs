
using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Walls;
using netDxf;

namespace DxfToRevit.Tests.Walls;

/// <summary>
/// Pair finder result model (pairs + ambiguities).
/// </summary>
public class WallFacePairFinderResultTests
{
    /// <summary>
    /// Constructor_WithPairsAndNoAmbiguities_ShouldStoreResults: Multiple matches / ambiguity.
    /// </summary>
    [Fact]
    public void Constructor_WithPairsAndNoAmbiguities_ShouldStoreResults()
    {
        var faceA = new LineSegment2D(
            new Vector2(0, 0),
            new Vector2(1000, 0));

        var faceB = new LineSegment2D(
            new Vector2(0, 200),
            new Vector2(1000, 200));

        var pair = new WallFacePair(
            faceA,
            faceB,
            "WALL-20",
            200,
            "TEST001",
            "TEST002");

        var result = new WallFacePairFinderResult(
            new[] { pair },
            Array.Empty<string>());

        Assert.Single(result.Pairs);
        Assert.Empty(result.Ambiguities);
        Assert.Equal(200, result.Pairs[0].ThicknessMm);
    }

    /// <summary>
    /// Constructor_WithAmbiguities_ShouldStoreMessages: Multiple matches / ambiguity.
    /// </summary>
    [Fact]
    public void Constructor_WithAmbiguities_ShouldStoreMessages()
    {
        var result = new WallFacePairFinderResult(
            Array.Empty<WallFacePair>(),
            new[]
            {
                "A line has multiple valid wall face matches."
            });

        Assert.Empty(result.Pairs);
        Assert.Single(result.Ambiguities);
        Assert.Contains(
            "multiple valid wall face matches",
            result.Ambiguities[0]);
    }
}

