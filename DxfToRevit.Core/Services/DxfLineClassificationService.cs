using DxfToRevit.Core.Models;

namespace DxfToRevit.Core.Services;

/// <summary>
/// Classifies a batch of raw DXF lines with <see cref="DxfLineClassifier"/>.
/// </summary>
public sealed class DxfLineClassificationService
{
    private readonly DxfLineClassifier _classifier = new();

    /// <summary>Classifies every line in the sequence.</summary>
    /// <param name="lines">Raw DXF lines.</param>
    /// <returns>Classified lines in the same order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lines"/> is <c>null</c>.</exception>
    public IReadOnlyList<ClassifiedDxfLine> Classify(IEnumerable<DxfLineModel> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var results = new List<ClassifiedDxfLine>();
        foreach (DxfLineModel line in lines)
            results.Add(_classifier.Classify(line));

        return results;
    }
}
