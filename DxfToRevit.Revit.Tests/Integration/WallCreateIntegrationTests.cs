using DxfToRevit.Core.Walls;
using netDxf;

namespace DxfToRevit.Revit.Tests.Integration;

/// <summary>
/// <see cref="RevitWallCreator"/> scenarios with an injected document.
/// </summary>
public sealed class WallCreateIntegrationTests
{
    private readonly RevitDocumentFixture _fixture = new();

    /// <summary>
    /// When a host provides a document: new RevitWallCreator(new RevitApiDocumentContext(doc)).
    /// Skipped for now — no document available.
    /// </summary>
    [Fact(Skip = "Revit Document integration requires injecting RevitApiDocumentContext with an open Document.")]
    public void CreateWall_WithInjectedDocument_Succeeds()
    {
        // IRevitDocumentContext ctx = new RevitApiDocumentContext(uidoc.Document);
        IRevitDocumentContext ctx = _fixture.Context
            ?? throw new InvalidOperationException(RevitDocumentFixture.SkipReason);

        var creator = new RevitWallCreator(ctx);
        var request = new WallCreationRequest(
            "W-INT-001",
            new Vector2(0, 0),
            new Vector2(1000, 0),
            thicknessMm: 200,
            levelName: "Level 1",
            heightMm: 3000);

        WallCreationResult result = creator.CreateWall(request);

        Assert.True(result.Success, result.Message);
        Assert.False(string.IsNullOrWhiteSpace(result.HostElementId));
    }

    [Fact]
    public void Fixture_WithoutContext_IsNotAvailable()
    {
        Assert.False(_fixture.IsAvailable);
    }
}
