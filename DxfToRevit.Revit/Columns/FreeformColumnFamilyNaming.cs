using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DxfToRevit.Revit.Columns;

/// <summary>
/// Produces a stable family name from a freeform column section (no Revit API needed).
/// The same shape gets the same name regardless of translation, start vertex or winding, so
/// columns with identical sections share one family in the project.
/// </summary>
public static class FreeformColumnFamilyNaming
{
    /// <summary>Prefix of every generated family name.</summary>
    public const string Prefix = "DxfToRevit Freeform";

    /// <summary>Precision (mm) to which coordinates are rounded before hashing.</summary>
    public const double PrecisionMm = 0.1;

    /// <summary>
    /// Builds the family name from the section vertices (relative to the placement point, in mm),
    /// e.g. <c>DxfToRevit Freeform 400x400 - 1a2b3c4d</c>: bounding size plus a short hash of the shape.
    /// </summary>
    /// <param name="profileMm">Section vertices in millimetres.</param>
    /// <returns>The family name.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="profileMm"/> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException">The profile has fewer than three vertices.</exception>
    public static string FamilyName(IReadOnlyList<(double X, double Y)> profileMm)
    {
        ArgumentNullException.ThrowIfNull(profileMm);
        if (profileMm.Count < 3)
            throw new ArgumentException("Profile needs at least 3 vertices.", nameof(profileMm));

        List<(long X, long Y)> pts = Canonicalize(profileMm);

        long minX = pts.Min(p => p.X), maxX = pts.Max(p => p.X);
        long minY = pts.Min(p => p.Y), maxY = pts.Max(p => p.Y);
        string size = $"{Mm(maxX - minX)}x{Mm(maxY - minY)}";

        var sb = new StringBuilder();
        foreach (var (x, y) in pts)
            sb.Append(x).Append(',').Append(y).Append(';');

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        string shortHash = Convert.ToHexString(hash, 0, 4).ToLowerInvariant();

        return $"{Prefix} {size} - {shortHash}";
    }

    /// <summary>
    /// Rounds the vertices, removes consecutive duplicates and a repeated closing vertex, orders
    /// them counter-clockwise and starts at the smallest (X, Y) vertex.
    /// </summary>
    private static List<(long X, long Y)> Canonicalize(IReadOnlyList<(double X, double Y)> profileMm)
    {
        var pts = new List<(long X, long Y)>();
        foreach (var (x, y) in profileMm)
        {
            var q = (Round(x), Round(y));
            if (pts.Count == 0 || pts[^1] != q)
                pts.Add(q);
        }

        if (pts.Count > 1 && pts[0] == pts[^1])
            pts.RemoveAt(pts.Count - 1);

        // Winding: reverse when the signed area is negative (clockwise).
        double area2 = 0;
        for (int i = 0; i < pts.Count; i++)
        {
            var a = pts[i];
            var b = pts[(i + 1) % pts.Count];
            area2 += (double)a.X * b.Y - (double)b.X * a.Y;
        }

        if (area2 < 0)
            pts.Reverse();

        int start = 0;
        for (int i = 1; i < pts.Count; i++)
        {
            if (pts[i].X < pts[start].X || (pts[i].X == pts[start].X && pts[i].Y < pts[start].Y))
                start = i;
        }

        return pts.Skip(start).Concat(pts.Take(start)).ToList();
    }

    /// <summary>Rounds millimetres to integer units of <see cref="PrecisionMm"/>.</summary>
    private static long Round(double mm) => (long)Math.Round(mm / PrecisionMm, MidpointRounding.AwayFromZero);

    /// <summary>Formats integer units as whole millimetres.</summary>
    private static string Mm(long units) =>
        Math.Round(units * PrecisionMm).ToString("0", CultureInfo.InvariantCulture);
}
