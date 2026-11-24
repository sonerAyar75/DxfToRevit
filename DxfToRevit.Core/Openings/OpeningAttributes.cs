namespace DxfToRevit.Core.Openings;

/// <summary>Interpreted attribute values of a marker, in millimetres.</summary>
/// <param name="TypeCode">Type code (trimmed); empty when the attribute is missing.</param>
/// <param name="HeightMm">Opening height in millimetres.</param>
/// <param name="SillHeightMm">Sill height in millimetres (0 for doors).</param>
public sealed record OpeningAttributes(string TypeCode, double HeightMm, double SillHeightMm);
