using netDxf;

namespace DxfToRevit.Core.Import;

/// <summary>A straight wall already in the model.</summary>
/// <param name="ElementId">Element id in the host application.</param>
/// <param name="LevelName">Base level name.</param>
/// <param name="StartMm">Location line start (millimetres).</param>
/// <param name="EndMm">Location line end (millimetres).</param>
/// <param name="ThicknessMm">Wall thickness (millimetres).</param>
public sealed record ExistingWall(string ElementId, string? LevelName, Vector2 StartMm, Vector2 EndMm, double ThicknessMm);
