namespace DxfToRevit.Revit.Tests.Mapping;

/// <summary>LevelMatchPlanner: named level lookup and the lowest level as default.</summary>
public sealed class LevelMatchPlannerTests
{
    private static readonly LevelCandidate[] Levels =
    {
        new("Level 2", 10.0),
        new("Level 1", 0.0),
        new("Roof", 20.0)
    };

    private readonly LevelMatchPlanner _planner = new();

    [Fact]
    public void NoName_PicksLowestElevation()
    {
        int i = _planner.Plan(Levels, null, out string? error);
        Assert.Equal(1, i);
        Assert.Null(error);
    }

    [Fact]
    public void Name_MatchesCaseInsensitive()
    {
        int i = _planner.Plan(Levels, " level 2 ", out string? error);
        Assert.Equal(0, i);
        Assert.Null(error);
    }

    [Fact]
    public void MissingName_ReturnsErrorListingLevels()
    {
        int i = _planner.Plan(Levels, "Level 9", out string? error);
        Assert.Equal(-1, i);
        Assert.Contains("Level 9", error);
        Assert.Contains("Roof", error);
    }

    [Fact]
    public void NoLevels_ReturnsError()
    {
        int i = _planner.Plan(Array.Empty<LevelCandidate>(), "Level 1", out string? error);
        Assert.Equal(-1, i);
        Assert.NotNull(error);
    }
}
