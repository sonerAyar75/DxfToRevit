using DxfToRevit.Revit.Walls;

namespace DxfToRevit.Revit.Tests.Mapping;

/// <summary>WallTypeMatchPlanner: exact type name, family with matching width, or duplicate proposal.</summary>
public sealed class WallTypeMatchPlannerTests
{
    private static readonly WallTypeCandidate[] Types =
    {
        new("Generic - 200mm", "Basic Wall", 200, 1),
        new("Generic - 300mm", "Basic Wall", 300, 1),
        new("Exterior - Brick", "Basic Wall", 250, 5),
        new("Some Other", "Other Family", 100, 1)
    };

    private readonly WallTypeMatchPlanner _planner = new();

    [Fact]
    public void ExactTypeName_WinsRegardlessOfThickness()
    {
        var m = _planner.Plan(Types, "Generic - 300mm", 150);
        Assert.Equal(WallTypeMatchKind.ExactName, m.Kind);
        Assert.Equal(1, m.Index);
    }

    [Fact]
    public void FamilyKey_MatchesByWidthWithinTolerance()
    {
        var m = _planner.Plan(Types, "Basic Wall", 200.4);
        Assert.Equal(WallTypeMatchKind.FamilyWidthMatch, m.Kind);
        Assert.Equal(0, m.Index);
    }

    [Fact]
    public void KeyMatching_IsCaseInsensitive()
    {
        var m = _planner.Plan(Types, "basic wall", 300);
        Assert.Equal(WallTypeMatchKind.FamilyWidthMatch, m.Kind);
        Assert.Equal(1, m.Index);
    }

    [Fact]
    public void MissingWidth_DuplicatesFromFamily_WithGeneratedName()
    {
        var m = _planner.Plan(Types, "Basic Wall", 150);
        Assert.Equal(WallTypeMatchKind.DuplicateFromFamily, m.Kind);
        Assert.Equal(0, m.Index);
        Assert.Equal("Basic Wall - 150mm", m.NewTypeName);
    }

    [Fact]
    public void Duplicate_PrefersFewestLayers()
    {
        var types = new[]
        {
            new WallTypeCandidate("Exterior - Brick", "Basic Wall", 250, 5),
            new WallTypeCandidate("Generic - 200mm", "Basic Wall", 200, 1)
        };

        var m = _planner.Plan(types, "Basic Wall", 150);
        Assert.Equal(WallTypeMatchKind.DuplicateFromFamily, m.Kind);
        Assert.Equal(1, m.Index);
    }

    [Fact]
    public void Duplicate_NameCollision_GetsSuffix()
    {
        var types = new[]
        {
            new WallTypeCandidate("Generic - 200mm", "Basic Wall", 200, 1),
            new WallTypeCandidate("Basic Wall - 150mm", "Basic Wall", 180, 1)
        };

        var m = _planner.Plan(types, "Basic Wall", 150);
        Assert.Equal(WallTypeMatchKind.DuplicateFromFamily, m.Kind);
        Assert.Equal("Basic Wall - 150mm (2)", m.NewTypeName);
    }

    [Fact]
    public void UnknownKey_IsNotFound()
    {
        var m = _planner.Plan(Types, "Nope", 200);
        Assert.Equal(WallTypeMatchKind.NotFound, m.Kind);
        Assert.Equal(-1, m.Index);
    }

    [Fact]
    public void InvalidInput_Throws()
    {
        Assert.Throws<ArgumentException>(() => _planner.Plan(Types, " ", 200));
        Assert.Throws<ArgumentOutOfRangeException>(() => _planner.Plan(Types, "Basic Wall", 0));
    }
}
