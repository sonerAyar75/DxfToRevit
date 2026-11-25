using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Geometry;
using netDxf;

namespace DxfToRevit.Tests.Columns;

/// <summary>
/// Rotated rectangular column: becomes a rectangular family with true width/depth and an angle (not freeform).
/// </summary>
public sealed class RotatedRectangleColumnTests
{
    /// <summary>A w×d rectangle rotated by <paramref name="deg"/> degrees about its centre.</summary>
    private static Vector2[] Rect(double cx, double cy, double w, double d, double deg, bool clockwiseOrder = false)
    {
        double a = deg * Math.PI / 180.0;
        double c = Math.Cos(a), s = Math.Sin(a);
        var local = new[] { (-w / 2, -d / 2), (w / 2, -d / 2), (w / 2, d / 2), (-w / 2, d / 2) };
        var pts = local.Select(p => new Vector2(cx + p.Item1 * c - p.Item2 * s, cy + p.Item1 * s + p.Item2 * c)).ToArray();
        return clockwiseOrder ? pts.Reverse().ToArray() : pts;
    }

    [Theory]
    [InlineData(30.0, 300.0, 600.0, 30.0)]
    [InlineData(-20.0, 300.0, 600.0, -20.0)]
    [InlineData(45.0, 400.0, 250.0, 45.0)]
    // 60° → −30°, width and depth swap
    [InlineData(60.0, 300.0, 600.0, -30.0)]
    // 120° → 30°, 180° period: width and depth stay the same
    [InlineData(120.0, 300.0, 600.0, 30.0)]
    public void TryGetOrientedRectangle_ReturnsTrueSizeAndNormalizedAngle(
        double deg, double w, double d, double expectedDeg)
    {
        bool ok = PolygonMetrics2D.TryGetOrientedRectangle(
            Rect(1000, 2000, w, d, deg), out Vector2 center, out double width, out double depth, out double angle);

        Assert.True(ok);
        Assert.Equal(1000, center.X, 6);
        Assert.Equal(2000, center.Y, 6);
        Assert.Equal(expectedDeg, angle * 180.0 / Math.PI, 6);

        bool swapped = Math.Abs(((deg - expectedDeg) / 90.0) % 2) > 0.5;
        Assert.Equal(swapped ? d : w, width, 6);
        Assert.Equal(swapped ? w : d, depth, 6);
    }

    [Fact]
    public void TryGetOrientedRectangle_ClockwiseVertexOrder_SameGeometry()
    {
        bool ok = PolygonMetrics2D.TryGetOrientedRectangle(
            Rect(0, 0, 300, 600, 30, clockwiseOrder: true), out _, out double w, out double d, out double angle);

        Assert.True(ok);
        // Rebuilding the rectangle from the result must give the same footprint.
        var rebuilt = Rect(0, 0, w, d, angle * 180.0 / Math.PI);
        var original = Rect(0, 0, 300, 600, 30);
        foreach (Vector2 p in original)
            Assert.Contains(rebuilt, q => Vector2.Distance(p, q) < 1e-6);
    }

    [Fact]
    public void TryGetOrientedRectangle_Parallelogram_False()
    {
        var outline = new[] { new Vector2(0, 0), new Vector2(400, 0), new Vector2(500, 300), new Vector2(100, 300) };
        Assert.False(PolygonMetrics2D.TryGetOrientedRectangle(outline, out _, out _, out _, out _));
    }

    [Fact]
    public void Factory_RotatedRectangle_IsRectangularWithRotation()
    {
        var col = new ColumnModel("C1", "COLUMN", "H1", Rect(500, 500, 300, 600, 30));
        var request = new ColumnCreationRequestFactory().FromColumnModels(new[] { col })[0];

        Assert.Equal(ColumnSectionKind.Rectangular, request.Type.SectionKind);
        Assert.Equal(300, request.Type.WidthMm!.Value, 6);
        Assert.Equal(600, request.Type.DepthMm!.Value, 6);
        Assert.Equal(30, request.RotationRadians * 180.0 / Math.PI, 6);
        Assert.Equal(500, request.CentroidMm.X, 6);
        Assert.Equal(500, request.CentroidMm.Y, 6);
    }

    [Fact]
    public void Factory_AxisAlignedRectangle_HasZeroRotation()
    {
        var col = new ColumnModel("C2", "COLUMN", "H2", Rect(0, 0, 300, 600, 0));
        var request = new ColumnCreationRequestFactory().FromColumnModels(new[] { col })[0];

        Assert.Equal(ColumnSectionKind.Rectangular, request.Type.SectionKind);
        Assert.Equal(0, request.RotationRadians);
    }

    [Fact]
    public void Factory_RotatedRectangle_DisabledOption_IsFreeform()
    {
        var col = new ColumnModel("C3", "COLUMN", "H3", Rect(0, 0, 300, 600, 30));
        var request = new ColumnCreationRequestFactory { RecognizeRotatedRectangles = false }
            .FromColumnModels(new[] { col })[0];

        Assert.Equal(ColumnSectionKind.Freeform, request.Type.SectionKind);
    }

    [Fact]
    public void Validator_RotatedRectangle_UsesTrueSectionNotBoundingBox()
    {
        // Rotated 40×600 column: the axis-aligned box exceeds 50 mm in both directions, but the true width is 40 mm.
        var col = new ColumnModel("C4", "COLUMN", "H4", Rect(0, 0, 40, 600, 30));
        var request = new ColumnCreationRequestFactory().FromColumnModels(new[] { col })[0];

        string? error = new ColumnCreationValidator().Validate(request);

        Assert.NotNull(error);
        Assert.Contains("40.0", error);
    }
}
