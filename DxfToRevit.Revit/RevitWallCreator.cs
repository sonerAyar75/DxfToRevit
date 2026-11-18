using DxfToRevit.Core.Building;

namespace DxfToRevit.Revit;

/// <summary>
/// <see cref="IWallCreator"/> that creates Revit Basic Walls through an injected
/// <see cref="IRevitDocumentContext"/>.
/// </summary>
/// <remarks>
/// <code>
/// // Add-in / integration host:
/// var ctx = new RevitApiDocumentContext(uidoc.Document);
/// IWallCreator walls = new RevitWallCreator(ctx);
///
/// // Tests / CI (no document; every request fails):
/// IWallCreator walls = new RevitWallCreator();
/// </code>
/// </remarks>
public sealed class RevitWallCreator : IWallCreator
{
    private readonly IRevitDocumentContext _documentContext;

    /// <summary>
    /// Creates the creator. Without a context, <see cref="NullRevitDocumentContext"/> is used (safe default).
    /// </summary>
    /// <param name="documentContext">Document context to write to.</param>
    public RevitWallCreator(IRevitDocumentContext? documentContext = null)
    {
        _documentContext = documentContext ?? NullRevitDocumentContext.Instance;
    }

    /// <summary>The bound document context (for tests and diagnostics).</summary>
    public IRevitDocumentContext DocumentContext => _documentContext;

    /// <inheritdoc />
    public WallCreationResult CreateWall(WallCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_documentContext.IsAvailable)
        {
            return WallCreationResult.Fail(
                request.Id,
                "RevitWallCreator: Document context is not available. " +
                "Inject RevitApiDocumentContext with an open Document, " +
                "or use InMemoryWallCreator in unit tests.");
        }

        return _documentContext.CreateBasicWall(request);
    }

    /// <inheritdoc />
    public IReadOnlyList<WallCreationResult> CreateWalls(IEnumerable<WallCreationRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        return requests.Select(CreateWall).ToList();
    }
}
