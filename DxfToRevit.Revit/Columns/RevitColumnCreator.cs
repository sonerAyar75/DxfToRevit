using DxfToRevit.Core.Columns;
using DxfToRevit.Revit.Common;

namespace DxfToRevit.Revit.Columns;

/// <summary>
/// <see cref="IColumnCreator"/> that writes columns to a Revit document through an
/// <see cref="IRevitDocumentContext"/>. Without a bound document every request fails.
/// </summary>
public sealed class RevitColumnCreator : IColumnCreator
{
    private readonly IRevitDocumentContext _documentContext;

    /// <summary>Creates the creator.</summary>
    /// <param name="documentContext">Document context; <see cref="NullRevitDocumentContext"/> when <c>null</c>.</param>
    public RevitColumnCreator(IRevitDocumentContext? documentContext = null)
    {
        _documentContext = documentContext ?? NullRevitDocumentContext.Instance;
    }

    /// <summary>The document context columns are written to.</summary>
    public IRevitDocumentContext DocumentContext => _documentContext;

    /// <inheritdoc />
    public ColumnCreationResult CreateColumn(ColumnCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_documentContext.IsAvailable)
        {
            return ColumnCreationResult.Fail(
                request.Id,
                "RevitColumnCreator: Document context is not available. " +
                "Inject RevitApiDocumentContext or use InMemoryColumnCreator in unit tests.");
        }

        return _documentContext.CreateColumn(request);
    }

    /// <inheritdoc />
    public IReadOnlyList<ColumnCreationResult> CreateColumns(IEnumerable<ColumnCreationRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        return requests.Select(CreateColumn).ToList();
    }
}
