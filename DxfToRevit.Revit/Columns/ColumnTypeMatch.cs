using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;

namespace DxfToRevit.Revit.Columns;

/// <summary>Planner output.</summary>
/// <param name="Kind">Kind of match.</param>
/// <param name="Index">Index into the candidate list (-1 when not found).</param>
/// <param name="NewTypeName">Name for the duplicated type when <paramref name="Kind"/> is <see cref="ColumnTypeMatchKind.DuplicateFromFamily"/>.</param>
public readonly record struct ColumnTypeMatch(ColumnTypeMatchKind Kind, int Index, string? NewTypeName = null);
