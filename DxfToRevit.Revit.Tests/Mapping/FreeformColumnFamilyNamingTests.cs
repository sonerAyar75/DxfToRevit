namespace DxfToRevit.Revit.Tests.Mapping;

/// <summary>
/// Freeform column family name: same shape → same name (independent of start vertex, winding and rounding noise).
/// </summary>
public sealed class FreeformColumnFamilyNamingTests
{
    private static readonly (double X, double Y)[] LShape =
    {
        (0, 0), (400, 0), (400, 100), (100, 100), (100, 400), (0, 400)
    };

    [Fact]
    public void FamilyName_ContainsPrefixAndSize()
    {
        string name = FreeformColumnFamilyNaming.FamilyName(LShape);

        Assert.StartsWith("DxfToRevit Freeform 400x400 - ", name);
        Assert.Matches(@" - [0-9a-f]{8}$", name);
    }

    [Fact]
    public void FamilyName_StartVertexAndOrientationDoNotMatter()
    {
        var rotatedStart = LShape.Skip(2).Concat(LShape.Take(2)).ToArray();
        var reversed = LShape.Reverse().ToArray();

        string a = FreeformColumnFamilyNaming.FamilyName(LShape);
        Assert.Equal(a, FreeformColumnFamilyNaming.FamilyName(rotatedStart));
        Assert.Equal(a, FreeformColumnFamilyNaming.FamilyName(reversed));
    }

    [Fact]
    public void FamilyName_TinyNoiseBelowPrecision_SameName()
    {
        var noisy = LShape.Select(p => (p.X + 0.01, p.Y - 0.01)).ToArray();
        Assert.Equal(FreeformColumnFamilyNaming.FamilyName(LShape), FreeformColumnFamilyNaming.FamilyName(noisy));
    }

    [Fact]
    public void FamilyName_DifferentShape_DifferentName()
    {
        var other = LShape.Select(p => (p.X, p.Y * 1.5)).ToArray();
        Assert.NotEqual(FreeformColumnFamilyNaming.FamilyName(LShape), FreeformColumnFamilyNaming.FamilyName(other));
    }

    [Fact]
    public void FamilyName_RepeatedClosingVertex_Ignored()
    {
        var closed = LShape.Concat(new[] { LShape[0] }).ToArray();
        Assert.Equal(FreeformColumnFamilyNaming.FamilyName(LShape), FreeformColumnFamilyNaming.FamilyName(closed));
    }

    [Fact]
    public void LibraryFileSearch_FindsColumnTemplate_PrefersEnglishMetric()
    {
        string root = Path.Combine(Path.GetTempPath(), "DxfToRevitTpl_" + Guid.NewGuid().ToString("N"));
        try
        {
            string imperial = Path.Combine(root, "English-Imperial", "Column.rft");
            string metric = Path.Combine(root, "English", "Metric Column.rft");
            Directory.CreateDirectory(Path.GetDirectoryName(imperial)!);
            Directory.CreateDirectory(Path.GetDirectoryName(metric)!);
            File.WriteAllText(imperial, "x");
            File.WriteAllText(metric, "x");

            string? found = LibraryFileSearch.FindFirst(new[] { root }, new[] { "Metric Column.rft", "Column.rft" });

            Assert.Equal(metric, found);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* best-effort */ }
        }
    }
}
