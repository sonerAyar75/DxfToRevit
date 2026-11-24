namespace DxfToRevit.Revit;

/// <summary>
/// API-free summary of a Revit column family type used by <see cref="ColumnTypeMatchPlanner"/>.
/// Dimensions that cannot be read are <see cref="double.NaN"/>.
/// </summary>
/// <param name="Name">Type name.</param>
/// <param name="FamilyName">Family name.</param>
/// <param name="WidthMm">Width (or diameter) in millimetres.</param>
/// <param name="DepthMm">Depth (or diameter) in millimetres.</param>
public readonly record struct ColumnTypeCandidate(string Name, string FamilyName, double WidthMm, double DepthMm);
