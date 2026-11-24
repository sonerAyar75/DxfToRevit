using netDxf;

namespace DxfToRevit.Core.Import;

/// <summary>A column already in the model.</summary>
/// <param name="ElementId">Element id in the host application.</param>
/// <param name="LevelName">Base level name.</param>
/// <param name="LocationMm">Plan location (millimetres).</param>
public sealed record ExistingColumn(string ElementId, string? LevelName, Vector2 LocationMm);
