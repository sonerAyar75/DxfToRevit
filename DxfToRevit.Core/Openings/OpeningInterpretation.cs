namespace DxfToRevit.Core.Openings;

/// <summary>Result of <see cref="OpeningInterpreter.Interpret"/>.</summary>
/// <param name="Openings">Openings that can be created, in marker order.</param>
/// <param name="Issues">Errors (opening skipped) and warnings (opening created).</param>
public sealed record OpeningInterpretation(IReadOnlyList<OpeningModel> Openings, IReadOnlyList<OpeningIssue> Issues);
