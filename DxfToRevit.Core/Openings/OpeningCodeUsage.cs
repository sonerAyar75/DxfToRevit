namespace DxfToRevit.Core.Openings;

/// <summary>How often a <c>TYPE</c> code of one opening kind occurs in a DXF, and in which sizes.</summary>
/// <param name="Category">Opening category.</param>
/// <param name="Code">Trimmed <c>TYPE</c> code; empty when the blocks have no <c>TYPE</c> attribute.</param>
/// <param name="Count">Number of openings with this code.</param>
/// <param name="Sizes">Distinct sizes as <c>&lt;width&gt;x&lt;height&gt;</c> in millimetres, ascending.</param>
public sealed record OpeningCodeUsage(OpeningCategory Category, string Code, int Count, IReadOnlyList<string> Sizes);
