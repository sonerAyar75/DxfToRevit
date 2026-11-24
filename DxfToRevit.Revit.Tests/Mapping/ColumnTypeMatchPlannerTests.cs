namespace DxfToRevit.Revit.Tests.Mapping;

/// <summary>ColumnTypeMatchPlanner: exact type name, family with matching size, or duplicate proposal.</summary>
public sealed class ColumnTypeMatchPlannerTests
{
    private static readonly ColumnTypeCandidate[] Types =
    {
        new("300 x 300mm", "Rectangular Column", 300, 300),
        new("400 x 600mm", "Rectangular Column", 400, 600),
        new("Dia 500mm", "Round Column", 500, 500),
        new("Unreadable", "Broken Column", double.NaN, double.NaN)
    };

    private readonly ColumnTypeMatchPlanner _planner = new();

    [Fact]
    public void ExactTypeName_Wins()
    {
        var m = _planner.Plan(Types, "400 x 600mm", 999, 999);
        Assert.Equal(ColumnTypeMatchKind.ExactName, m.Kind);
        Assert.Equal(1, m.Index);
    }

    [Fact]
    public void Family_MatchesBothDimensions()
    {
        var m = _planner.Plan(Types, "Rectangular Column", 400.3, 600);
        Assert.Equal(ColumnTypeMatchKind.FamilySizeMatch, m.Kind);
        Assert.Equal(1, m.Index);
    }

    [Fact]
    public void Family_WidthMatchesButDepthDoesNot_Duplicates()
    {
        var m = _planner.Plan(Types, "Rectangular Column", 300, 350);
        Assert.Equal(ColumnTypeMatchKind.DuplicateFromFamily, m.Kind);
        Assert.Equal(0, m.Index);
        Assert.Equal("Rectangular Column - 300x350mm", m.NewTypeName);
    }

    [Fact]
    public void Circular_UsesDiameterNaming()
    {
        var m = _planner.Plan(Types, "Round Column", 600, 600, circular: true);
        Assert.Equal(ColumnTypeMatchKind.DuplicateFromFamily, m.Kind);
        Assert.Equal(2, m.Index);
        Assert.Equal("Round Column - D600mm", m.NewTypeName);
    }

    [Fact]
    public void Circular_ExistingDiameter_IsMatched()
    {
        var m = _planner.Plan(Types, "Round Column", 500, 500, circular: true);
        Assert.Equal(ColumnTypeMatchKind.FamilySizeMatch, m.Kind);
        Assert.Equal(2, m.Index);
    }

    [Fact]
    public void UnreadableSizes_NeverMatch_ButCanBeDuplicated()
    {
        var m = _planner.Plan(Types, "Broken Column", 300, 300);
        Assert.Equal(ColumnTypeMatchKind.DuplicateFromFamily, m.Kind);
        Assert.Equal(3, m.Index);
    }

    [Fact]
    public void NameCollision_GetsSuffix()
    {
        var types = new[]
        {
            new ColumnTypeCandidate("A", "Rectangular Column", 300, 300),
            new ColumnTypeCandidate("Rectangular Column - 250x250mm", "Rectangular Column", 260, 260)
        };

        var m = _planner.Plan(types, "Rectangular Column", 250, 250);
        Assert.Equal(ColumnTypeMatchKind.DuplicateFromFamily, m.Kind);
        Assert.Equal("Rectangular Column - 250x250mm (2)", m.NewTypeName);
    }

    [Fact]
    public void UnknownKey_IsNotFound()
    {
        var m = _planner.Plan(Types, "Nope", 300, 300);
        Assert.Equal(ColumnTypeMatchKind.NotFound, m.Kind);
    }

    [Fact]
    public void InvalidInput_Throws()
    {
        Assert.Throws<ArgumentException>(() => _planner.Plan(Types, "", 300, 300));
        Assert.Throws<ArgumentOutOfRangeException>(() => _planner.Plan(Types, "Rectangular Column", 0, 300));
    }
}
