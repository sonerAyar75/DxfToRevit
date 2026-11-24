using DxfToRevit.Core.Geometry;
using netDxf;

namespace DxfToRevit.Core.Walls;

/// <summary>
/// Builds closed wall footprints (rectangles) from a centerline and a thickness by offsetting
/// the centerline by ±T/2 perpendicular to its direction.
/// </summary>
public sealed class WallFootprintBuilder
{
    /// <summary>
    /// Creates the four-corner closed outline for one centerline.
    /// </summary>
    /// <remarks>
    /// <c>u = d / |d|</c>, <c>n = (-u.y, u.x)</c>, <c>h = T / 2</c>;
    /// corners: <c>S + n*h, E + n*h, E - n*h, S - n*h</c>.
    /// </remarks>
    /// <param name="centerLine">Wall centerline.</param>
    /// <param name="thicknessMm">Wall thickness in millimetres; must be positive.</param>
    /// <returns>The wall footprint.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="thicknessMm"/> is not positive.</exception>
    /// <exception cref="ArgumentException"><paramref name="centerLine"/> is degenerate.</exception>
    public WallSolid2D Build(LineSegment2D centerLine, double thicknessMm)
    {
        ArgumentNullException.ThrowIfNull(centerLine);

        if (thicknessMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(thicknessMm));

        if (centerLine.IsDegenerate)
            throw new ArgumentException("Centerline must not be degenerate.", nameof(centerLine));

        Vector2 d = centerLine.Direction;
        double len = d.Modulus();
        Vector2 unit = d / len;
        Vector2 normal = new Vector2(-unit.Y, unit.X);
        double half = thicknessMm / 2.0;

        Vector2 s = centerLine.Start;
        Vector2 e = centerLine.End;

        var outline = new[]
        {
            s + normal * half,
            e + normal * half,
            e - normal * half,
            s - normal * half
        };

        return new WallSolid2D(centerLine, thicknessMm, outline);
    }

    /// <summary>
    /// Builds one footprint per non-degenerate centerline.
    /// </summary>
    /// <param name="centerLines">Wall centerlines.</param>
    /// <param name="thicknessMm">Thickness of each centerline (same order and count).</param>
    /// <returns>The footprints; degenerate centerlines are skipped.</returns>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The two lists have different lengths.</exception>
    public IReadOnlyList<WallSolid2D> BuildMany(
        IReadOnlyList<LineSegment2D> centerLines,
        IReadOnlyList<double> thicknessMm)
    {
        ArgumentNullException.ThrowIfNull(centerLines);
        ArgumentNullException.ThrowIfNull(thicknessMm);

        if (centerLines.Count != thicknessMm.Count)
            throw new ArgumentException("centerLines and thicknessMm must have the same length.");

        var result = new List<WallSolid2D>(centerLines.Count);
        for (int i = 0; i < centerLines.Count; i++)
        {
            if (centerLines[i].IsDegenerate)
                continue;
            result.Add(Build(centerLines[i], thicknessMm[i]));
        }

        return result;
    }
}
