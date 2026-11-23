using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Models;
using DxfToRevit.Core.Openings;
using netDxf;

namespace DxfToRevit.Tests.Openings;

/// <summary>Builders for opening markers and walls used by the opening tests.</summary>
internal static class OpeningTestFixtures
{
    /// <summary>The standard opening kinds.</summary>
    public static OpeningDefinitionRegistry Registry { get; } = OpeningDefinitionRegistry.CreateDefault();

    /// <summary>A straight wall footprint along the X axis from x = 0 to <paramref name="lengthMm"/>, centred on y = 0.</summary>
    /// <param name="lengthMm">Wall length.</param>
    /// <param name="thicknessMm">Wall thickness.</param>
    public static WallSolid2D HorizontalWall(double lengthMm = 5000, double thicknessMm = 200) =>
        new WallFootprintBuilder().Build(new LineSegment2D(new Vector2(0, 0), new Vector2(lengthMm, 0)), thicknessMm);

    /// <summary>
    /// An axis-aligned rectangular marker centred at (<paramref name="centerX"/>, <paramref name="centerY"/>),
    /// <paramref name="width"/> along X and <paramref name="depth"/> along Y.
    /// </summary>
    /// <param name="kind">Block name: door, window or opening.</param>
    /// <param name="centerX">Centre X.</param>
    /// <param name="centerY">Centre Y.</param>
    /// <param name="width">Width along X.</param>
    /// <param name="depth">Depth along Y.</param>
    /// <param name="hinge">Hinge circle centre; <c>null</c> for no circle (the insertion point is used).</param>
    /// <param name="attributes">Attribute values by tag.</param>
    /// <param name="handle">Entity handle.</param>
    public static OpeningMarker Marker(
        string kind,
        double centerX,
        double centerY,
        double width,
        double depth,
        Vector2? hinge = null,
        IReadOnlyDictionary<string, string>? attributes = null,
        string handle = "A1")
    {
        OpeningDefinition definition = Registry.Find(kind, null)
                                       ?? throw new ArgumentException($"Unknown kind '{kind}'.", nameof(kind));
        double hw = width / 2, hd = depth / 2;
        var outline = new[]
        {
            new Vector2(centerX - hw, centerY - hd),
            new Vector2(centerX + hw, centerY - hd),
            new Vector2(centerX + hw, centerY + hd),
            new Vector2(centerX - hw, centerY + hd)
        };

        return new OpeningMarker(
            handle,
            kind,
            "0",
            definition,
            outline,
            hinge ?? new Vector2(centerX - hw, centerY - hd),
            hinge is not null,
            attributes ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>Attribute dictionary from tag/value pairs.</summary>
    /// <param name="pairs">Tag and value pairs.</param>
    public static IReadOnlyDictionary<string, string> Attributes(params (string Tag, string Value)[] pairs) =>
        pairs.ToDictionary(p => p.Tag, p => p.Value, StringComparer.OrdinalIgnoreCase);

    /// <summary>Path of a file in the test data folder.</summary>
    /// <param name="fileName">File name.</param>
    public static string TestDataPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
}
