#if REVIT_API_AVAILABLE
using Autodesk.Revit.DB;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;
using System.Globalization;

namespace DxfToRevit.Revit.Common;

/// <summary>Reading and describing element parameters, shared by the type resolvers.</summary>
internal static class RevitParameters
{
    /// <summary>Returns the first parameter found under any of the names (optionally writable only).</summary>
    /// <param name="element">Element or type.</param>
    /// <param name="names">Parameter names, in order of preference.</param>
    /// <param name="writable">Whether only writable parameters count.</param>
    public static Parameter? Find(Element element, IReadOnlyList<string> names, bool writable = false)
    {
        foreach (string name in names)
        {
            Parameter? p = element.LookupParameter(name);
            if (p is not null && (!writable || !p.IsReadOnly))
                return p;
        }

        return null;
    }

    /// <summary>Reads a length parameter in millimetres, or <c>null</c> when absent.</summary>
    /// <param name="element">Element or type.</param>
    /// <param name="names">Parameter names, in order of preference.</param>
    public static double? ReadLengthMm(Element element, IReadOnlyList<string> names)
    {
        Parameter? p = Find(element, names);
        return p is { StorageType: StorageType.Double }
            ? LengthUnitConverter.FeetToMm(p.AsDouble())
            : null;
    }

    /// <summary>Comma-separated names of the writable length (double) parameters of a type.</summary>
    /// <param name="symbol">Family type.</param>
    public static string DescribeWritableLengths(FamilySymbol symbol) =>
        string.Join(", ", symbol.Parameters
            .Cast<Parameter>()
            .Where(p => p.StorageType == StorageType.Double && !p.IsReadOnly)
            .Select(p => p.Definition.Name)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase));

    /// <summary>Formats a size for cache keys (three decimals, invariant culture).</summary>
    /// <param name="value">Size in millimetres.</param>
    public static string FormatSize(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
#endif
