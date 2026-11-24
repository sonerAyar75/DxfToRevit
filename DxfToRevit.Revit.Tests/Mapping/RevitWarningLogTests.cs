using DxfToRevit.Revit;

namespace DxfToRevit.Revit.Tests.Mapping;

/// <summary>Grouping of the Revit warnings collected at the commit of an import.</summary>
public sealed class RevitWarningLogTests
{
    [Fact]
    public void Groups_CountsSameTextAndDistinctElements()
    {
        var log = new RevitWarningLog();
        log.Add(RevitWarningKind.Overlap, "Highlighted walls overlap.", new[] { "1", "2" });
        log.Add(RevitWarningKind.Overlap, "Highlighted walls overlap.", new[] { "2", "3" });

        RevitWarningGroup group = Assert.Single(log.Groups);
        Assert.Equal(2, group.Count);
        Assert.Equal(3, group.ElementCount);
    }

    [Fact]
    public void Groups_ListOverlapsFirst()
    {
        var log = new RevitWarningLog();
        log.Add(RevitWarningKind.Other, "Line is slightly off axis.", new[] { "1" });
        log.Add(RevitWarningKind.Overlap, "There are identical instances in the same place.", new[] { "5", "6" });

        Assert.Equal(new[] { RevitWarningKind.Overlap, RevitWarningKind.Other }, log.Groups.Select(g => g.Kind));
    }

    [Fact]
    public void Add_BlankText_UsesPlaceholder()
    {
        var log = new RevitWarningLog();
        log.Add(RevitWarningKind.Other, "  ", Array.Empty<string>());

        Assert.Equal("Revit warning", Assert.Single(log.Groups).Text);
    }

    [Fact]
    public void Groups_SameTextOfDifferentKinds_AreSeparate()
    {
        var log = new RevitWarningLog();
        log.Add(RevitWarningKind.Other, "Text", new[] { "1" });
        log.Add(RevitWarningKind.Overlap, "Text", new[] { "1" });

        Assert.Equal(2, log.Groups.Count);
    }
}
