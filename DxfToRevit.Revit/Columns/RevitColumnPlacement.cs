#if REVIT_API_AVAILABLE
using Autodesk.Revit.DB;

namespace DxfToRevit.Revit.Columns;

/// <summary>
/// Sets the base and top constraints of a column family instance (same logic as for walls).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>With a top level: Top Level = that level and Top Offset = 0, so the column follows the level.</description></item>
/// <item><description>Unconnected: Top Level = base level and Top Offset = base offset + height.</description></item>
/// </list>
/// </remarks>
internal static class RevitColumnPlacement
{
    /// <summary>Applies the base offset and the top constraint.</summary>
    /// <param name="column">Column instance (already placed on <paramref name="level"/>).</param>
    /// <param name="level">Base level.</param>
    /// <param name="baseOffsetFeet">Base offset in feet.</param>
    /// <param name="heightFeet">Unconnected height in feet (ignored when <paramref name="topLevel"/> is set).</param>
    /// <param name="error">Reason when a parameter could not be set.</param>
    /// <param name="topLevel">Top level to bind to, or <c>null</c> for an unconnected column.</param>
    /// <returns><c>true</c> when all parameters were set.</returns>
    public static bool TryApplyHeight(
        FamilyInstance column,
        Level level,
        double baseOffsetFeet,
        double heightFeet,
        out string? error,
        Level? topLevel = null)
    {
        error = null;

        // The base level is already the level passed to NewFamilyInstance; only the offset and the top are set.
        bool ok = TrySet(column, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM, baseOffsetFeet);

        if (topLevel is not null)
        {
            ok = ok &&
                 TrySet(column, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM, topLevel.Id) &&
                 TrySet(column, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM, 0.0);
        }
        else
        {
            ok = ok &&
                 TrySet(column, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM, level.Id) &&
                 TrySet(column, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM, baseOffsetFeet + heightFeet);
        }

        if (!ok)
            error = "Could not set column height (Base Offset / Top Level / Top Offset not writable on this family).";

        return ok;
    }

    /// <summary>Sets a writable double parameter; returns <c>false</c> when it is missing or read-only.</summary>
    private static bool TrySet(Element element, BuiltInParameter bip, double value)
    {
        Parameter? p = element.get_Parameter(bip);
        return p is { IsReadOnly: false } && p.Set(value);
    }

    /// <summary>Sets a writable element id parameter; returns <c>false</c> when it is missing or read-only.</summary>
    private static bool TrySet(Element element, BuiltInParameter bip, ElementId value)
    {
        Parameter? p = element.get_Parameter(bip);
        return p is { IsReadOnly: false } && p.Set(value);
    }
}
#endif
