namespace DxfToRevit.Revit;

/// <summary>API-free summary of a Revit level used by <see cref="LevelMatchPlanner"/>.</summary>
/// <param name="Name">Level name.</param>
/// <param name="ElevationFeet">Level elevation in feet.</param>
public readonly record struct LevelCandidate(string Name, double ElevationFeet);
