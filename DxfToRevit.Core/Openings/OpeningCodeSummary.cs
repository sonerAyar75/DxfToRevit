using System.Globalization;

namespace DxfToRevit.Core.Openings;

/// <summary>
/// Groups the openings found in a DXF by category and <c>TYPE</c> code, so the codes can be mapped
/// to Revit types before the import.
/// </summary>
public static class OpeningCodeSummary
{
    /// <summary>Builds the usage list, ordered by category, then by code (numbers in numeric order).</summary>
    /// <param name="openings">Openings produced by the pipeline.</param>
    /// <exception cref="ArgumentNullException"><paramref name="openings"/> is <c>null</c>.</exception>
    public static IReadOnlyList<OpeningCodeUsage> Build(IEnumerable<OpeningModel> openings)
    {
        ArgumentNullException.ThrowIfNull(openings);

        return openings
            .GroupBy(o => (o.Definition.Category, Code: o.Attributes.TypeCode.Trim().ToUpperInvariant()))
            .Select(g => new OpeningCodeUsage(
                g.Key.Category,
                g.First().Attributes.TypeCode.Trim(),
                g.Count(),
                g.Select(o => (W: Math.Round(o.Placement.WidthMm), H: Math.Round(o.Attributes.HeightMm)))
                    .Distinct()
                    .OrderBy(s => s.W).ThenBy(s => s.H)
                    .Select(s => string.Create(CultureInfo.InvariantCulture, $"{s.W:0}x{s.H:0}"))
                    .ToList()))
            .OrderBy(u => u.Category)
            .ThenBy(u => u.Code, CodeComparer.Instance)
            .ToList();
    }

    /// <summary>Orders codes numerically when both are numbers, otherwise by text (ignoring case).</summary>
    internal sealed class CodeComparer : IComparer<string>
    {
        /// <summary>Shared instance.</summary>
        public static CodeComparer Instance { get; } = new();

        /// <inheritdoc />
        public int Compare(string? x, string? y)
        {
            bool xNumber = double.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out double xv);
            bool yNumber = double.TryParse(y, NumberStyles.Float, CultureInfo.InvariantCulture, out double yv);
            if (xNumber && yNumber)
                return xv.CompareTo(yv);
            if (xNumber != yNumber)
                return xNumber ? -1 : 1;
            return StringComparer.OrdinalIgnoreCase.Compare(x, y);
        }
    }
}
