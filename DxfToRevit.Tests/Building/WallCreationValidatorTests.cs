using DxfToRevit.Core.Building;
using netDxf;
using Xunit;

namespace DxfToRevit.Tests.Building;

/// <summary>
/// WallCreationValidator: minimum length and thickness rules.
/// </summary>
public sealed class WallCreationValidatorTests
{
    private readonly WallCreationValidator _validator = new(minLengthMm: 1.0);

    /// <summary>1000 mm centerline, 200 mm thickness → valid.</summary>
    [Fact]
    public void Validate_ValidRequest_ReturnsNull()
    {
        var req = new WallCreationRequest("W1", new Vector2(0, 0), new Vector2(1000, 0), 200);
        Assert.Null(_validator.Validate(req));
    }

    /// <summary>Degenerate segment (length 0) → error.</summary>
    [Fact]
    public void Validate_DegenerateCenterLine_ReturnsError()
    {
        var req = new WallCreationRequest("W1", new Vector2(5, 5), new Vector2(5, 5), 200);
        string? error = _validator.Validate(req);
        Assert.NotNull(error);
        Assert.Contains("length", error, StringComparison.OrdinalIgnoreCase);
    }
}
