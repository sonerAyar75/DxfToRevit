using DxfToRevit.Core.Import;
using DxfToRevit.Core.Openings;
using netDxf;

namespace DxfToRevit.Revit.Tests.Mapping;

/// <summary>
/// Opening adapter without the Revit API: request → create arguments in feet, door flip decisions,
/// type key parsing and the creator without a document.
/// </summary>
public sealed class OpeningMappingTests
{
    private static OpeningCreationRequest Request(
        Vector2 direction, OpeningHanding? handing = null, OpeningCategory category = OpeningCategory.Door) =>
        new(
            id: "D001",
            category: category,
            creationMode: category == OpeningCategory.EmptyOpening ? OpeningCreationMode.WallOpening : OpeningCreationMode.HostedFamily,
            typeKey: category == OpeningCategory.EmptyOpening ? null : "M_Single-Flush::0915 x 2134mm",
            typeCode: "1",
            hostWallRequestId: "W001",
            locationMm: new Vector2(304.8, 609.6),
            wallDirection: direction,
            widthMm: 914.4,
            heightMm: 2133.6,
            sillHeightMm: 304.8,
            handing: handing,
            placement: new ElementPlacement(levelName: "Level 1"));

    [Fact]
    public void Map_ConvertsToFeetAndNormalisesDirection()
    {
        OpeningCreateArgs args = new OpeningCreateArgsMapper().Map(Request(new Vector2(3, 4)));

        Assert.Equal(1.0, args.LocationXFeet, 9);
        Assert.Equal(2.0, args.LocationYFeet, 9);
        Assert.Equal(0.6, args.DirectionX, 9);
        Assert.Equal(0.8, args.DirectionY, 9);
        Assert.Equal(3.0, args.WidthFeet, 9);
        Assert.Equal(7.0, args.HeightFeet, 9);
        Assert.Equal(1.0, args.SillHeightFeet, 9);
        Assert.Equal(914.4, args.WidthMm, 9);
        Assert.Equal("Level 1", args.LevelName);
        Assert.Equal("M_Single-Flush::0915 x 2134mm", args.TypeKey);
    }

    [Fact]
    public void Map_ZeroDirection_Throws()
    {
        Assert.Throws<ArgumentException>(() => new OpeningCreateArgsMapper().Map(Request(new Vector2(0, 0))));
    }

    [Theory]
    // Default convention (verified in Revit 2025): facing points to the swing side.
    [InlineData(SwingSide.Left, 0, 1, false)]
    [InlineData(SwingSide.Left, 0, -1, true)]
    [InlineData(SwingSide.Right, 0, -1, false)]
    [InlineData(SwingSide.Right, 0, 1, true)]
    public void NeedsFacingFlip_DefaultDoorConvention(SwingSide side, double fx, double fy, bool expected)
    {
        var planner = new OpeningOrientationPlanner();

        bool flip = planner.NeedsFacingFlip(
            OpeningCategory.Door, (1, 0), new OpeningHanding(HingeEnd.Start, side), (fx, fy));

        Assert.Equal(expected, flip);
    }

    [Fact]
    public void NeedsFacingFlip_FacingAwayFromSwingConvention_IsInverted()
    {
        var planner = new OpeningOrientationPlanner { Door = new OpeningOrientationConvention(false, true) };
        OpeningHanding handing = new OpeningHanding(HingeEnd.Start, SwingSide.Left);

        Assert.True(planner.NeedsFacingFlip(OpeningCategory.Door, (1, 0), handing, (0, 1)));
        Assert.False(planner.NeedsFacingFlip(OpeningCategory.Door, (1, 0), handing, (0, -1)));
    }

    [Fact]
    public void NeedsFacingFlip_DefaultWindowConvention_FacingPointsToSwing()
    {
        var planner = new OpeningOrientationPlanner();
        // Sash swings to the left (+Y) face: the window facing must point to +Y (verified in Revit 2025).
        OpeningHanding handing = new OpeningHanding(HingeEnd.Start, SwingSide.Left);

        Assert.False(planner.NeedsFacingFlip(OpeningCategory.Window, (1, 0), handing, (0, 1)));
        Assert.True(planner.NeedsFacingFlip(OpeningCategory.Window, (1, 0), handing, (0, -1)));
    }

    [Fact]
    public void ConventionFor_UsesWindowConventionOnlyForWindows()
    {
        var window = new OpeningOrientationConvention(true, false);
        var planner = new OpeningOrientationPlanner { Window = window };

        Assert.Same(window, planner.ConventionFor(OpeningCategory.Window));
        Assert.Same(OpeningOrientationConvention.StandardDoor, planner.ConventionFor(OpeningCategory.Door));
    }

    [Theory]
    // Default convention (verified in Revit 2025): hand points towards the hinge.
    [InlineData(HingeEnd.End, 1, 0, false)]
    [InlineData(HingeEnd.End, -1, 0, true)]
    [InlineData(HingeEnd.Start, -1, 0, false)]
    [InlineData(HingeEnd.Start, 1, 0, true)]
    public void NeedsHandFlip_DefaultConvention(HingeEnd end, double hx, double hy, bool expected)
    {
        var planner = new OpeningOrientationPlanner();

        bool flip = planner.NeedsHandFlip(
            OpeningCategory.Door, (1, 0), new OpeningHanding(end, SwingSide.Left), (hx, hy));

        Assert.Equal(expected, flip);
    }

    [Fact]
    public void NeedsHandFlip_RotatedWall_UsesWallDirection()
    {
        var planner = new OpeningOrientationPlanner();
        OpeningHanding handing = new OpeningHanding(HingeEnd.End, SwingSide.Left);

        // Wall along +Y, hinge at the end: the hand should point to +Y.
        Assert.False(planner.NeedsHandFlip(OpeningCategory.Door, (0, 1), handing, (0, 1)));
        Assert.True(planner.NeedsHandFlip(OpeningCategory.Door, (0, 1), handing, (0, -1)));
    }

    [Theory]
    [InlineData(OpeningCategory.Door)]
    [InlineData(OpeningCategory.Window)]
    public void NeedsHandFlip_WithoutHingeEnd_NeverFlips(OpeningCategory category)
    {
        var planner = new OpeningOrientationPlanner();

        Assert.False(planner.NeedsHandFlip(category, (1, 0), new OpeningHanding(null, SwingSide.Left), (-1, 0)));
    }

    [Theory]
    [InlineData("M_Single-Flush::0915 x 2134mm", "M_Single-Flush", "0915 x 2134mm")]
    [InlineData("  Family :: Type  ", "Family", "Type")]
    [InlineData("0915 x 2134mm", null, "0915 x 2134mm")]
    [InlineData("::Type", null, "Type")]
    public void TypeKey_TryParse_ValidKeys(string text, string? family, string type)
    {
        Assert.True(OpeningTypeKey.TryParse(text, out OpeningTypeKey? key));
        Assert.Equal(family, key!.FamilyName);
        Assert.Equal(type, key.TypeName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Family::")]
    public void TypeKey_TryParse_InvalidKeys(string? text)
    {
        Assert.False(OpeningTypeKey.TryParse(text, out _));
    }

    [Fact]
    public void TypeKey_Matches_IgnoresCaseAndAnyFamilyWhenOmitted()
    {
        OpeningTypeKey.TryParse("m_single-flush::0915 X 2134MM", out OpeningTypeKey? withFamily);
        OpeningTypeKey.TryParse("0915 x 2134mm", out OpeningTypeKey? typeOnly);

        Assert.True(withFamily!.Matches("M_Single-Flush", "0915 x 2134mm"));
        Assert.False(withFamily.Matches("M_Double-Flush", "0915 x 2134mm"));
        Assert.True(typeOnly!.Matches("M_Double-Flush", "0915 x 2134mm"));
        Assert.Equal("M_Single-Flush::0915 x 2134mm", OpeningTypeKey.Format("M_Single-Flush", "0915 x 2134mm"));
    }

    [Fact]
    public void RevitOpeningCreator_WithoutDocument_Fails()
    {
        var creator = new RevitOpeningCreator();

        OpeningCreationResult result = creator.CreateOpening(Request(new Vector2(1, 0)), "123");

        Assert.False(result.Success);
        Assert.Contains("not available", result.Message);
    }

    [Fact]
    public void NullContext_CreateOpening_Fails()
    {
        OpeningCreationResult result = NullRevitDocumentContext.Instance.CreateOpening(Request(new Vector2(1, 0)), "123");

        Assert.False(result.Success);
    }
}
