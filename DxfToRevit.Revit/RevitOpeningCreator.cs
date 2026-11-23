using DxfToRevit.Core.Building;

namespace DxfToRevit.Revit;

/// <summary>
/// <see cref="IOpeningCreator"/> that creates doors, windows and wall openings in a Revit document
/// through an <see cref="IRevitDocumentContext"/>. Without a bound document every request fails.
/// </summary>
public sealed class RevitOpeningCreator : IOpeningCreator
{
    private readonly IRevitDocumentContext _documentContext;

    /// <summary>Creates the creator.</summary>
    /// <param name="documentContext">Document context; <see cref="NullRevitDocumentContext"/> when <c>null</c>.</param>
    public RevitOpeningCreator(IRevitDocumentContext? documentContext = null)
    {
        _documentContext = documentContext ?? NullRevitDocumentContext.Instance;
    }

    /// <inheritdoc />
    public OpeningCreationResult CreateOpening(OpeningCreationRequest request, string hostWallElementId)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_documentContext.IsAvailable)
        {
            return OpeningCreationResult.Fail(request.Id,
                "RevitOpeningCreator: Document context is not available. " +
                "Inject RevitApiDocumentContext or use InMemoryOpeningCreator in unit tests.");
        }

        return _documentContext.CreateOpening(request, hostWallElementId);
    }
}
