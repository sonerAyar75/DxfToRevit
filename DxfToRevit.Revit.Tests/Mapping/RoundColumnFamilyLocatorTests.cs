namespace DxfToRevit.Revit.Tests.Mapping;

/// <summary>
/// Round column family search order: add-in folder → Revit library (metric first).
/// </summary>
public sealed class RoundColumnFamilyLocatorTests : IDisposable
{
    private readonly string _root;

    public RoundColumnFamilyLocatorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "DxfToRevitLocator_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best-effort */ }
    }

    private string Touch(params string[] parts)
    {
        string path = Path.Combine(new[] { _root }.Concat(parts).ToArray());
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "rfa");
        return path;
    }

    [Fact]
    public void Find_PrefersBundledFamily()
    {
        string bundled = Touch("addin", "Families", "My Round.rfa");
        Touch("lib", "English", "US", "Columns", "M_Round Column.rfa");

        var locator = new RoundColumnFamilyLocator(
            new[] { Path.Combine(_root, "addin", "Families") },
            new[] { Path.Combine(_root, "lib") });

        Assert.Equal(bundled, locator.Find());
    }

    [Fact]
    public void Find_LibraryPrefersMetricRoundColumn()
    {
        Touch("lib", "English-Imperial", "US", "Columns", "Round Column.rfa");
        string metric = Touch("lib", "English", "US", "Columns", "M_Round Column.rfa");

        var locator = new RoundColumnFamilyLocator(
            new[] { Path.Combine(_root, "missing") },
            new[] { Path.Combine(_root, "lib") });

        Assert.Equal(metric, locator.Find());
    }

    [Fact]
    public void Find_FallsBackToImperialRoundColumn()
    {
        string imperial = Touch("lib", "English-Imperial", "US", "Columns", "Round Column.rfa");

        var locator = new RoundColumnFamilyLocator(Array.Empty<string>(), new[] { Path.Combine(_root, "lib") });

        Assert.Equal(imperial, locator.Find());
    }

    [Fact]
    public void Find_NothingAvailable_ReturnsNull()
    {
        var locator = new RoundColumnFamilyLocator(
            new[] { Path.Combine(_root, "none"), null, "  " },
            new[] { Path.Combine(_root, "none2") });

        Assert.Null(locator.Find());
    }
}
