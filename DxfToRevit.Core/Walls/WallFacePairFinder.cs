using DxfToRevit.Core.Geometry;

namespace DxfToRevit.Core.Walls;

/// <summary>
/// Finds pairs of parallel wall faces.
/// </summary>
/// <remarks>
/// Two lines form a candidate pair when they are on the same wall layer, have the same thickness
/// code, are parallel, overlap along their length and are one wall thickness apart (±1 mm).
/// When one line has more than one candidate partner over an overlapping stretch, all of those
/// candidates are rejected and reported as ambiguities instead of guessing. The search compares
/// every pair of wall lines, so it is quadratic in the number of wall lines.
/// </remarks>
public sealed class WallFacePairFinder
{
    /// <summary>
    /// Returns the valid wall face pairs among the classified lines. Only wall lines with a
    /// thickness code are considered.
    /// </summary>
    /// <param name="lines">Classified DXF lines.</param>
    /// <returns>Accepted pairs and ambiguity messages.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lines"/> is <c>null</c>.</exception>
    public WallFacePairFinderResult FindPairs(
        IEnumerable<ClassifiedDxfLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var wallLines = lines
            .Where(line =>
                line.IsWallLayer &&
                line.WallThicknessMm.HasValue)
            .ToList();

        var candidatePairs = new List<(int A, int B, WallFacePair Pair)>();

        for (int i = 0; i < wallLines.Count; i++)
        {
            for (int j = i + 1; j < wallLines.Count; j++)
            {
                ClassifiedDxfLine lineA = wallLines[i];
                ClassifiedDxfLine lineB = wallLines[j];

                if (!string.Equals(
                    lineA.SourceLayer,
                    lineB.SourceLayer,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (lineA.WallThicknessMm != lineB.WallThicknessMm)
                    continue;

                if (!lineA.Geometry.IsParallelTo(lineB.Geometry))
                    continue;

                if (!lineA.Geometry.OverlapsAlongLength(lineB.Geometry))
                    continue;

                double distance =
                    lineA.Geometry.GetDistanceToParallelLine(lineB.Geometry);

                double thickness = lineA.WallThicknessMm!.Value;

                if (Math.Abs(distance - thickness) > 1.0)
                    continue;

                var pair = new WallFacePair(
                    lineA.Geometry,
                    lineB.Geometry,
                    lineA.SourceLayer,
                    thickness,
                    lineA.Handle,
                    lineB.Handle);

                candidatePairs.Add((i, j, pair));
            }
        }

        var rejected = new bool[candidatePairs.Count];
        var ambiguities = new List<string>();

        for (int i = 0; i < candidatePairs.Count; i++)
        {
            for (int j = i + 1; j < candidatePairs.Count; j++)
            {
                var c1 = candidatePairs[i];
                var c2 = candidatePairs[j];

                if (!SharesLineAndConflicts(c1, c2, wallLines))
                    continue;

                rejected[i] = true;
                rejected[j] = true;

                ambiguities.Add(FormatAmbiguity(c1.Pair));
                ambiguities.Add(FormatAmbiguity(c2.Pair));
            }
        }

        var acceptedPairs = new List<WallFacePair>();
        for (int i = 0; i < candidatePairs.Count; i++)
        {
            if (!rejected[i])
                acceptedPairs.Add(candidatePairs[i].Pair);
        }

        return new WallFacePairFinderResult(
            acceptedPairs,
            ambiguities.Distinct().ToList());
    }

    /// <summary>
    /// Whether two candidate pairs share a line and their partners overlap on that shared line
    /// (i.e. the shared line has two partners over the same stretch).
    /// </summary>
    private static bool SharesLineAndConflicts(
        (int A, int B, WallFacePair Pair) c1,
        (int A, int B, WallFacePair Pair) c2,
        IReadOnlyList<ClassifiedDxfLine> wallLines)
    {
        int? shared = null;
        int partner1 = -1;
        int partner2 = -1;

        if (c1.A == c2.A)
        {
            shared = c1.A;
            partner1 = c1.B;
            partner2 = c2.B;
        }
        else if (c1.A == c2.B)
        {
            shared = c1.A;
            partner1 = c1.B;
            partner2 = c2.A;
        }
        else if (c1.B == c2.A)
        {
            shared = c1.B;
            partner1 = c1.A;
            partner2 = c2.B;
        }
        else if (c1.B == c2.B)
        {
            shared = c1.B;
            partner1 = c1.A;
            partner2 = c2.A;
        }

        if (shared is null)
            return false;

        LineSegment2D sharedGeom = wallLines[shared.Value].Geometry;
        LineSegment2D partnerGeom1 = wallLines[partner1].Geometry;
        LineSegment2D partnerGeom2 = wallLines[partner2].Geometry;

        return OverlapIntervalsIntersect(
            sharedGeom, partnerGeom1, partnerGeom2);
    }

    /// <summary>
    /// Whether the overlap of <paramref name="partner1"/> and of <paramref name="partner2"/> with
    /// <paramref name="shared"/> intersect with a positive length.
    /// </summary>
    private static bool OverlapIntervalsIntersect(
        LineSegment2D shared,
        LineSegment2D partner1,
        LineSegment2D partner2)
    {
        var calculator = new SegmentOverlapCalculator();

        if (!calculator.TryGetOverlapInterval(
                shared, partner1, out double s1, out double e1))
            return false;

        if (!calculator.TryGetOverlapInterval(
                shared, partner2, out double s2, out double e2))
            return false;

        return Math.Min(e1, e2) > Math.Max(s1, s2);
    }

    /// <summary>Builds the ambiguity message for a rejected candidate pair.</summary>
    private static string FormatAmbiguity(WallFacePair pair) =>
        $"Layer '{pair.SourceLayer}': " +
        $"Face A Handle '{pair.FaceAHandle}', " +
        $"Face B Handle '{pair.FaceBHandle}' — " +
        "a line has multiple valid wall face matches. " +
        "The candidate pair was not accepted.";
}
