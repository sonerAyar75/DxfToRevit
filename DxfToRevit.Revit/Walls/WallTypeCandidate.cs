namespace DxfToRevit.Revit.Walls;

/// <summary>API-free summary of a Revit wall type used by <see cref="WallTypeMatchPlanner"/>.</summary>
/// <param name="Name">Type name (e.g. <c>Generic - 200mm</c>).</param>
/// <param name="FamilyName">Family name (e.g. <c>Basic Wall</c>).</param>
/// <param name="WidthMm">Total wall width in millimetres.</param>
/// <param name="LayerCount">Number of compound structure layers (0 when unknown).</param>
public readonly record struct WallTypeCandidate(string Name, string FamilyName, double WidthMm, int LayerCount);
