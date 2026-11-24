using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Models;
using DxfToRevit.Core.Openings;
using netDxf;
using Xunit;
using static DxfToRevit.Tests.Openings.OpeningTestFixtures;

namespace DxfToRevit.Tests.Openings;

/// <summary>Attribute parsing, wall matching, interpretation and jamb line filtering of opening markers.</summary>
public sealed class OpeningInterpretationTests
{
    /// <summary>Attribute values are centimetres, a decimal comma is accepted.</summary>
    [Fact]
    public void Parse_ConvertsCentimetresAndAcceptsDecimalComma()
    {
        var marker = Marker("window", 0, 0, 1200, 200,
            attributes: Attributes(("TYPE", "W1"), ("HEIGHT", "120,5"), ("SILL_HEIGHT", "95")));
        var issues = new List<OpeningIssue>();

        OpeningAttributes attributes = new OpeningAttributeParser().Parse(marker, issues);

        Assert.Equal("W1", attributes.TypeCode);
        Assert.Equal(1205, attributes.HeightMm, 6);
        Assert.Equal(950, attributes.SillHeightMm, 6);
        Assert.Empty(issues);
    }

    /// <summary>Missing attributes fall back to the kind defaults with a warning.</summary>
    [Fact]
    public void Parse_MissingHeight_UsesDefaultAndWarns()
    {
        var marker = Marker("door", 0, 0, 900, 200, attributes: Attributes(("TYPE", "1")));
        var issues = new List<OpeningIssue>();

        OpeningAttributes attributes = new OpeningAttributeParser().Parse(marker, issues);

        Assert.Equal(OpeningDefinitionRegistry.DefaultDoorHeightCm * 10, attributes.HeightMm, 6);
        Assert.Equal(0, attributes.SillHeightMm, 6);
        Assert.Contains(issues, i => i.Code == OpeningIssueCode.MissingAttribute && i.Severity == OpeningIssueSeverity.Warning);
    }

    /// <summary>A non-numeric height falls back to the default with an invalid-attribute warning.</summary>
    [Fact]
    public void Parse_InvalidHeight_WarnsInvalidAttribute()
    {
        var marker = Marker("door", 0, 0, 900, 200, attributes: Attributes(("TYPE", "1"), ("HEIGHT", "tall")));
        var issues = new List<OpeningIssue>();

        OpeningAttributes attributes = new OpeningAttributeParser().Parse(marker, issues);

        Assert.Equal(OpeningDefinitionRegistry.DefaultDoorHeightCm * 10, attributes.HeightMm, 6);
        Assert.Contains(issues, i => i.Code == OpeningIssueCode.InvalidAttribute);
    }

    /// <summary>A marker on a wall gives the wall index, width, thickness and centre on the axis.</summary>
    [Fact]
    public void TryMatch_MarkerOnWall_ReturnsPlacement()
    {
        var walls = new[] { HorizontalWall() };
        var marker = Marker("door", 1000, 0, 900, 200);
        var issues = new List<OpeningIssue>();

        bool matched = new OpeningWallMatcher().TryMatch(marker, walls, out OpeningPlacement? placement, issues);

        Assert.True(matched);
        Assert.NotNull(placement);
        Assert.Equal(0, placement!.HostWallIndex);
        Assert.Equal(900, placement.WidthMm, 3);
        Assert.Equal(200, placement.ThicknessMm, 3);
        Assert.Equal(1000, placement.CenterOnWallMm.X, 3);
        Assert.Equal(0, placement.CenterOnWallMm.Y, 3);
        Assert.Empty(issues);
    }

    /// <summary>A marker away from every wall is an error.</summary>
    [Fact]
    public void TryMatch_MarkerAwayFromWalls_ReportsNoHostWall()
    {
        var walls = new[] { HorizontalWall() };
        var marker = Marker("door", 1000, 3000, 900, 200);
        var issues = new List<OpeningIssue>();

        bool matched = new OpeningWallMatcher().TryMatch(marker, walls, out _, issues);

        Assert.False(matched);
        Assert.Contains(issues, i => i.Code == OpeningIssueCode.NoHostWall && i.Severity == OpeningIssueSeverity.Error);
    }

    /// <summary>A marker deeper or shallower than the wall is placed with a depth warning.</summary>
    [Fact]
    public void TryMatch_DepthDiffersFromWall_WarnsDepthMismatch()
    {
        var walls = new[] { HorizontalWall(thicknessMm: 200) };
        var marker = Marker("door", 1000, 0, 900, 150);
        var issues = new List<OpeningIssue>();

        bool matched = new OpeningWallMatcher().TryMatch(marker, walls, out OpeningPlacement? placement, issues);

        Assert.True(matched);
        Assert.Equal(200, placement!.ThicknessMm, 3);
        Assert.Contains(issues, i => i.Code == OpeningIssueCode.DepthMismatch && i.Severity == OpeningIssueSeverity.Warning);
    }

    /// <summary>The hinge circle gives the hinge end (nearest opening end) and the swing side (its wall face).</summary>
    [Theory]
    [InlineData(560, 90, HingeEnd.Start, SwingSide.Left)]
    [InlineData(1440, 90, HingeEnd.End, SwingSide.Left)]
    [InlineData(560, -90, HingeEnd.Start, SwingSide.Right)]
    [InlineData(1440, -90, HingeEnd.End, SwingSide.Right)]
    public void Interpret_DoorHinge_GivesHandingFromCircle(double hingeX, double hingeY, HingeEnd end, SwingSide side)
    {
        var walls = new[] { HorizontalWall() };
        var marker = Marker("door", 1000, 0, 900, 200, hinge: new Vector2(hingeX, hingeY),
            attributes: Attributes(("TYPE", "1"), ("HEIGHT", "210")));

        OpeningInterpretation result = new OpeningInterpreter().Interpret(new[] { marker }, walls);

        OpeningModel opening = Assert.Single(result.Openings);
        Assert.Equal("D001", opening.Id);
        Assert.NotNull(opening.Handing);
        Assert.Equal(OpeningLeafLayout.SingleHinged, opening.Handing!.Layout);
        Assert.Equal(end, opening.Handing.HingeEnd);
        Assert.Equal(side, opening.Handing.SwingSide);
        Assert.DoesNotContain(result.Issues, i => i.Code == OpeningIssueCode.MissingHinge);
        Assert.DoesNotContain(result.Issues, i => i.Severity == OpeningIssueSeverity.Error);
    }

    /// <summary>A door without a circle is created without orientation and with a warning.</summary>
    [Fact]
    public void Interpret_DoorWithoutCircle_WarnsAndLeavesOrientation()
    {
        var walls = new[] { HorizontalWall() };
        var marker = Marker("door", 1000, 0, 900, 200, attributes: Attributes(("TYPE", "1"), ("HEIGHT", "210")));

        OpeningInterpretation result = new OpeningInterpreter().Interpret(new[] { marker }, walls);

        OpeningModel opening = Assert.Single(result.Openings);
        Assert.Null(opening.Handing);
        Assert.Contains(result.Issues, i => i.Code == OpeningIssueCode.MissingHinge);
    }

    /// <summary>A window hinge circle gives the same single-leaf orientation as a door's.</summary>
    [Fact]
    public void Interpret_WindowHinge_GivesSingleHandingFromCircle()
    {
        var walls = new[] { HorizontalWall() };
        // Window 1200 wide from x = 400 to 1600; circle near the end, on the right (-Y) face.
        var marker = Marker("window", 1000, 0, 1200, 200, hinge: new Vector2(1550, -90),
            attributes: Attributes(("TYPE", "1"), ("HEIGHT", "150"), ("SILL_HEIGHT", "90")));

        OpeningInterpretation result = new OpeningInterpreter().Interpret(new[] { marker }, walls);

        OpeningModel opening = Assert.Single(result.Openings);
        Assert.Equal("W001", opening.Id);
        Assert.Equal(OpeningHanding.Single(HingeEnd.End, SwingSide.Right), opening.Handing);
    }

    /// <summary>Circles near both ends mark a double leaf: swing side only, no hinge end.</summary>
    [Theory]
    [InlineData("door")]
    [InlineData("window")]
    public void Interpret_CirclesAtBothEnds_GiveDoubleLeaf(string kind)
    {
        var walls = new[] { HorizontalWall() };
        var marker = Marker(kind, 1000, 0, 1400, 200,
            hinges: new[] { new Vector2(350, 90), new Vector2(1650, 90) },
            attributes: Attributes(("TYPE", "2"), ("HEIGHT", "210"), ("SILL_HEIGHT", "0")));

        OpeningInterpretation result = new OpeningInterpreter().Interpret(new[] { marker }, walls);

        OpeningModel opening = Assert.Single(result.Openings);
        Assert.Equal(OpeningLeafLayout.DoubleHinged, opening.Handing!.Layout);
        Assert.Null(opening.Handing.HingeEnd);
        Assert.Equal(SwingSide.Left, opening.Handing.SwingSide);
    }

    /// <summary>A circle in the middle (sliding, fixed, multi-panel) gives the side only.</summary>
    [Fact]
    public void Interpret_CircleInMiddle_GivesSideOnly()
    {
        var walls = new[] { HorizontalWall() };
        var marker = Marker("window", 1000, 0, 1800, 200, hinge: new Vector2(1000, -90),
            attributes: Attributes(("TYPE", "3"), ("HEIGHT", "150"), ("SILL_HEIGHT", "90")));

        OpeningInterpretation result = new OpeningInterpreter().Interpret(new[] { marker }, walls);

        OpeningModel opening = Assert.Single(result.Openings);
        Assert.Equal(OpeningHanding.SideOnly(SwingSide.Right), opening.Handing);
    }

    /// <summary>Circles near one end plus middle circles still give a single leaf at that end.</summary>
    [Fact]
    public void Interpret_CircleAtOneEndAndMiddle_GivesSingleLeaf()
    {
        var walls = new[] { HorizontalWall() };
        var marker = Marker("window", 1000, 0, 1800, 200,
            hinges: new[] { new Vector2(1000, 90), new Vector2(150, 90) },
            attributes: Attributes(("TYPE", "4"), ("HEIGHT", "150"), ("SILL_HEIGHT", "90")));

        OpeningInterpretation result = new OpeningInterpreter().Interpret(new[] { marker }, walls);

        Assert.Equal(OpeningHanding.Single(HingeEnd.Start, SwingSide.Left), Assert.Single(result.Openings).Handing);
    }

    /// <summary>Circles on both wall faces: the face with more circles wins, with a warning.</summary>
    [Fact]
    public void Interpret_CirclesOnBothFaces_UsesMajorityAndWarns()
    {
        var walls = new[] { HorizontalWall() };
        var marker = Marker("door", 1000, 0, 1400, 200,
            hinges: new[] { new Vector2(350, -90), new Vector2(1650, -90), new Vector2(1650, 90) },
            attributes: Attributes(("TYPE", "2"), ("HEIGHT", "210")));

        OpeningInterpretation result = new OpeningInterpreter().Interpret(new[] { marker }, walls);

        Assert.Equal(OpeningHanding.Double(SwingSide.Right), Assert.Single(result.Openings).Handing);
        Assert.Contains(result.Issues, i => i.Code == OpeningIssueCode.MissingHinge && i.Severity == OpeningIssueSeverity.Warning);
    }

    /// <summary>Windows without a circle and empty openings have no orientation; each kind has its own id prefix.</summary>
    [Fact]
    public void Interpret_WindowAndOpening_UseOwnPrefixesWithoutHanding()
    {
        var walls = new[] { HorizontalWall() };
        var window = Marker("window", 1000, 0, 1200, 200,
            attributes: Attributes(("TYPE", "1"), ("HEIGHT", "150"), ("SILL_HEIGHT", "90")), handle: "B1");
        var opening = Marker("opening", 3000, 0, 1000, 200, hinge: new Vector2(2600, 90),
            attributes: Attributes(("TYPE", "-"), ("HEIGHT", "210"), ("SILL_HEIGHT", "0")), handle: "B2");

        OpeningInterpretation result = new OpeningInterpreter().Interpret(new[] { window, opening }, walls);

        Assert.Equal(new[] { "W001", "O001" }, result.Openings.Select(o => o.Id));
        Assert.All(result.Openings, o => Assert.Null(o.Handing));
        Assert.Equal(OpeningCreationMode.WallOpening, result.Openings[1].Definition.CreationMode);
        Assert.Contains(result.Issues, i => i.Code == OpeningIssueCode.MissingHinge && i.MarkerHandle == "B1");
        Assert.DoesNotContain(result.Issues, i => i.MarkerHandle == "B2");
    }

    /// <summary>Overlapping markers on the same wall are both rejected.</summary>
    [Fact]
    public void Interpret_OverlappingMarkers_AreRejected()
    {
        var walls = new[] { HorizontalWall() };
        var first = Marker("door", 1000, 0, 900, 200, hinge: new Vector2(560, 90), handle: "C1");
        var second = Marker("door", 1500, 0, 900, 200, hinge: new Vector2(1060, 90), handle: "C2");

        OpeningInterpretation result = new OpeningInterpreter().Interpret(new[] { first, second }, walls);

        Assert.Empty(result.Openings);
        Assert.Equal(2, result.Issues.Count(i => i.Code == OpeningIssueCode.Overlapping));
    }

    /// <summary>Lines completely inside a marker (jambs, leaf) are removed; wall lines passing through stay.</summary>
    [Fact]
    public void RemoveLinesInside_DropsOnlyLinesInsideMarkers()
    {
        var marker = Marker("door", 1000, 0, 900, 200);
        var jamb = Line(560, -100, 560, 100, "J1");
        var wallFace = Line(0, 100, 5000, 100, "F1");
        var elsewhere = Line(3000, -100, 3000, 100, "E1");

        IReadOnlyList<ClassifiedDxfLine> kept = new OpeningRegionFilter()
            .RemoveLinesInside(new[] { jamb, wallFace, elsewhere }, new[] { marker }, out int removed);

        Assert.Equal(1, removed);
        Assert.Equal(new[] { "F1", "E1" }, kept.Select(l => l.Handle));
    }

    private static ClassifiedDxfLine Line(double x1, double y1, double x2, double y2, string handle) =>
        new(new LineSegment2D(new Vector2(x1, y1), new Vector2(x2, y2)), "WALL", true, 200, handle);
}
