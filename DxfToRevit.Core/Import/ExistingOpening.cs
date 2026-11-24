using DxfToRevit.Core.Openings;
using netDxf;

namespace DxfToRevit.Core.Import;

/// <summary>A door, window or wall opening already in the model.</summary>
/// <param name="ElementId">Element id in the host application.</param>
/// <param name="Category">Door, window or empty opening.</param>
/// <param name="LevelName">Level name (of the host wall for wall openings).</param>
/// <param name="LocationMm">Plan location (millimetres): the insertion point, or the centre of a wall opening.</param>
public sealed record ExistingOpening(string ElementId, OpeningCategory Category, string? LevelName, Vector2 LocationMm);
