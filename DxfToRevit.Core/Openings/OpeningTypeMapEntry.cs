namespace DxfToRevit.Core.Openings;

/// <summary>One row of the opening type map.</summary>
/// <param name="Category">Opening category (door or window).</param>
/// <param name="Code">Type code from the marker <c>TYPE</c> attribute, or <c>*</c> for the default of the category.</param>
/// <param name="TypeKey">Revit type key, usually <c>Family::Type</c>.</param>
public sealed record OpeningTypeMapEntry(OpeningCategory Category, string Code, string TypeKey);
