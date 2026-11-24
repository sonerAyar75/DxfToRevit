using DxfToRevit.Core.Dxf;
using DxfToRevit.Core.Openings;
using DxfToRevit.Core.Services;
using netDxf;
using Xunit;
using static DxfToRevit.Tests.Openings.OpeningTestFixtures;

namespace DxfToRevit.Tests.Openings;

/// <summary>
/// Reading opening markers from <c>OpeningsDoors.dxf</c>: three dynamic <c>door</c> blocks
/// (1000, 800 and 1200 mm) in a 150 mm and a 200 mm wall.
/// </summary>
public sealed class OpeningDxfTests
{
    private const string SampleFile = "OpeningsDoors.dxf";

    /// <summary>Dynamic block references are resolved to the <c>door</c> block and read with their attributes.</summary>
    [Fact]
    public void ReadMarkers_SampleFile_FindsThreeDoors()
    {
        DxfDocument document = DxfDocument.Load(TestDataPath(SampleFile));

        DxfOpeningReadResult result = new DxfOpeningReader().ReadMarkers(document);

        Assert.Empty(result.Issues);
        Assert.Equal(3, result.Markers.Count);
        Assert.All(result.Markers, m =>
        {
            Assert.Equal(OpeningCategory.Door, m.Definition.Category);
            Assert.True(m.HasHingeCircle);
            Assert.True(m.Attributes.ContainsKey(OpeningAttributeTags.Type));
        });
    }

    /// <summary>The pipeline attaches the doors to their walls with the dynamic block widths and no errors.</summary>
    /// <summary>Circles hidden by a visibility state (invisible flag) are not hinge marks.</summary>
    [Fact]
    public void ReadMarkers_HiddenCircles_AreIgnored()
    {
        var block = new netDxf.Blocks.Block("window");
        block.Entities.Add(new netDxf.Entities.Polyline2D(new[]
        {
            new netDxf.Entities.Polyline2DVertex(-1000, 0), new netDxf.Entities.Polyline2DVertex(0, 0),
            new netDxf.Entities.Polyline2DVertex(0, -150), new netDxf.Entities.Polyline2DVertex(-1000, -150)
        }, true));
        block.Entities.Add(new netDxf.Entities.Circle(new Vector3(0, 0, 0), 20));
        block.Entities.Add(new netDxf.Entities.Circle(new Vector3(-1000, 0, 0), 20) { IsVisible = false });
        block.Entities.Add(new netDxf.Entities.Circle(new Vector3(-500, 0, 0), 20) { IsVisible = false });
        var document = new DxfDocument();
        document.Entities.Add(new netDxf.Entities.Insert(block, new Vector3(5000, 2000, 0)));

        OpeningMarker marker = Assert.Single(new DxfOpeningReader().ReadMarkers(document).Markers);

        Vector2 hinge = Assert.Single(marker.HingePoints);
        Assert.Equal(5000, hinge.X, 6);
        Assert.Equal(2000, hinge.Y, 6);
    }

    [Fact]
    public void Analyze_SampleFile_CreatesDoorsWithBlockWidths()
    {
        DxfCenterLinePipeline.Result result = new DxfCenterLinePipeline().Analyze(TestDataPath(SampleFile));

        Assert.Equal(3, result.OpeningMarkerCount);
        Assert.Equal(3, result.Openings.Count);
        Assert.DoesNotContain(result.OpeningIssues, i => i.Severity == OpeningIssueSeverity.Error);
        Assert.Equal(new[] { 800.0, 1000.0, 1200.0 },
            result.Openings.Select(o => Math.Round(o.Placement.WidthMm)).OrderBy(w => w));
        Assert.All(result.Openings, o =>
        {
            Assert.Equal(2100, o.Attributes.HeightMm, 3);
            Assert.NotNull(o.Handing);
            Assert.InRange(o.Placement.HostWallIndex, 0, result.WallSolids.Count - 1);
        });
    }

    /// <summary>The two doors in the 150 mm wall share one continuous host wall (the gaps are bridged).</summary>
    [Fact]
    public void Analyze_SampleFile_WallContinuesThroughDoors()
    {
        DxfCenterLinePipeline.Result result = new DxfCenterLinePipeline().Analyze(TestDataPath(SampleFile));

        var thinWallDoors = result.Openings.Where(o => Math.Abs(o.Placement.ThicknessMm - 150) < 1).ToList();

        Assert.Equal(2, thinWallDoors.Count);
        Assert.Single(thinWallDoors.Select(o => o.Placement.HostWallIndex).Distinct());
    }

    /// <summary>Default heights chosen by the user are used when the block has no height attribute.</summary>
    [Fact]
    public void Analyze_CustomRegistry_IsUsedByThePipeline()
    {
        var registry = OpeningDefinitionRegistry.CreateDefault(doorHeightCm: 220);

        DxfCenterLinePipeline.Result result = new DxfCenterLinePipeline(registry).Analyze(TestDataPath(SampleFile));

        // The sample blocks carry a HEIGHT attribute, so the attribute value still wins.
        Assert.All(result.Openings, o => Assert.Equal(2100, o.Attributes.HeightMm, 3));
        Assert.Equal(3, result.Openings.Count);
    }
}
