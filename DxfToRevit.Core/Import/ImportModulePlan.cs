namespace DxfToRevit.Core.Import;

/// <summary>What a module plans to create, and what it skips because it is already in the model.</summary>
/// <param name="ToCreate">Number of elements the module will try to create.</param>
/// <param name="Skipped">Elements already in the model, per category.</param>
public sealed record ImportModulePlan(int ToCreate, IReadOnlyDictionary<ImportCategory, int> Skipped)
{
    /// <summary>A plan that creates nothing and skips nothing.</summary>
    public static ImportModulePlan Nothing { get; } = new(0, new Dictionary<ImportCategory, int>());
}
