namespace DxfToRevit.Revit;

/// <summary>Warnings with the same kind and text, counted together.</summary>
/// <param name="Kind">Warning kind.</param>
/// <param name="Text">Revit's description text.</param>
/// <param name="Count">How many times Revit raised it.</param>
/// <param name="ElementCount">Number of distinct elements involved.</param>
public sealed record RevitWarningGroup(RevitWarningKind Kind, string Text, int Count, int ElementCount);
