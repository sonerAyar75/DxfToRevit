namespace DxfToRevit.Core.Models;

/// <summary>
/// Output of <see cref="Services.WallFacePairFinder"/>: the accepted face pairs and the
/// ambiguity messages for candidates that were rejected.
/// </summary>
public sealed class WallFacePairFinderResult
{
    /// <summary>Accepted wall face pairs.</summary>
    public IReadOnlyList<WallFacePair> Pairs { get; }

    /// <summary>
    /// Human-readable descriptions of candidate pairs rejected because a line had more than one
    /// valid partner over the same stretch.
    /// </summary>
    public IReadOnlyList<string> Ambiguities { get; }

    /// <summary>Creates the result.</summary>
    /// <param name="pairs">Accepted pairs.</param>
    /// <param name="ambiguities">Ambiguity messages.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public WallFacePairFinderResult(
        IReadOnlyList<WallFacePair> pairs,
        IReadOnlyList<string> ambiguities)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        ArgumentNullException.ThrowIfNull(ambiguities);
        Pairs = pairs;
        Ambiguities = ambiguities;
    }
}
