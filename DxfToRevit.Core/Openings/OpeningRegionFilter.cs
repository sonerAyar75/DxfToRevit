using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Models;

namespace DxfToRevit.Core.Openings;

/// <summary>
/// Removes wall lines that lie inside an opening marker — typically the short jamb lines drawn at
/// the ends of an opening. Inside a marked opening the wall is continuous, so such lines carry no
/// wall information and could otherwise be mistaken for a pair of wall faces.
/// </summary>
public sealed class OpeningRegionFilter
{
    /// <summary>Distance (mm) from a marker edge that still counts as inside (jamb lines lie on the edges).</summary>
    public double ToleranceMm { get; }

    /// <summary>Creates the filter.</summary>
    /// <param name="toleranceMm">Edge tolerance in millimetres; must not be negative.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="toleranceMm"/> is negative.</exception>
    public OpeningRegionFilter(double toleranceMm = GeometryTolerance.PointDistanceMm)
    {
        if (toleranceMm < 0)
            throw new ArgumentOutOfRangeException(nameof(toleranceMm));
        ToleranceMm = toleranceMm;
    }

    /// <summary>
    /// Returns the lines without the wall lines whose both endpoints lie inside a marker.
    /// Lines that only cross a marker (continuous wall faces) are kept.
    /// </summary>
    /// <param name="lines">Classified DXF lines.</param>
    /// <param name="markers">Opening markers.</param>
    /// <param name="removedCount">Number of removed lines.</param>
    /// <returns>The remaining lines, in their original order.</returns>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public IReadOnlyList<ClassifiedDxfLine> RemoveLinesInside(
        IReadOnlyList<ClassifiedDxfLine> lines,
        IReadOnlyList<OpeningMarker> markers,
        out int removedCount)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(markers);

        removedCount = 0;
        if (markers.Count == 0)
            return lines;

        var kept = new List<ClassifiedDxfLine>(lines.Count);
        foreach (ClassifiedDxfLine line in lines)
        {
            bool insideMarker = line.IsWallLayer && markers.Any(m =>
                PolygonMetrics2D.ContainsPoint(m.Outline, line.Geometry.Start, ToleranceMm) &&
                PolygonMetrics2D.ContainsPoint(m.Outline, line.Geometry.End, ToleranceMm));

            if (insideMarker)
                removedCount++;
            else
                kept.Add(line);
        }

        return kept;
    }
}
