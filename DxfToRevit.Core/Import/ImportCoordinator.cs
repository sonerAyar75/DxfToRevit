namespace DxfToRevit.Core.Import;

/// <summary>
/// Runs import modules in order: first every module plans (nothing is written), then, inside the
/// host's transaction, every module creates, with a model regeneration after the modules that ask
/// for one. Host-independent, so the whole flow can be tested with the in-memory creators.
/// </summary>
public sealed class ImportCoordinator
{
    private readonly IReadOnlyList<IImportModule> _modules;
    private readonly ImportBag _shared = new();

    /// <summary>Creates the coordinator.</summary>
    /// <param name="modules">Modules of this import; one instance per import.</param>
    /// <exception cref="ArgumentNullException"><paramref name="modules"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Two modules offer the same category.</exception>
    public ImportCoordinator(IEnumerable<IImportModule> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);
        _modules = modules.OrderBy(m => m.Order).ToList();

        string? duplicate = _modules.SelectMany(m => m.Categories).GroupBy(c => c.Key)
            .FirstOrDefault(g => g.Count() > 1)?.Key;
        if (duplicate is not null)
            throw new ArgumentException($"Import category '{duplicate}' is offered by two modules.", nameof(modules));
    }

    /// <summary>The modules, in import order.</summary>
    public IReadOnlyList<IImportModule> Modules => _modules;

    /// <summary>Every category of every module, in import order.</summary>
    public IReadOnlyList<ImportCategory> Categories => _modules.SelectMany(m => m.Categories).ToList();

    /// <summary>Elements already in the model, per category, after <see cref="Plan"/>.</summary>
    public IReadOnlyDictionary<ImportCategory, int> Skipped { get; private set; } = new Dictionary<ImportCategory, int>();

    /// <summary>Number of elements the modules will try to create, after <see cref="Plan"/>.</summary>
    public int PlannedCount { get; private set; }

    /// <summary>Lets every module plan its requests.</summary>
    /// <param name="context">Plan context.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
    public void Plan(ImportPlanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var skipped = new Dictionary<ImportCategory, int>();
        int planned = 0;
        foreach (IImportModule module in _modules)
        {
            ImportModulePlan plan = module.Plan(context, _shared);
            planned += plan.ToCreate;
            foreach (KeyValuePair<ImportCategory, int> s in plan.Skipped.Where(s => s.Value > 0))
                skipped[s.Key] = skipped.GetValueOrDefault(s.Key) + s.Value;
        }

        Skipped = skipped;
        PlannedCount = planned;
    }

    /// <summary>Lets every module create its elements; call inside the host's transaction, after <see cref="Plan"/>.</summary>
    /// <param name="regenerate">Regenerates the host model; called after modules that ask for it.</param>
    /// <returns>One result per module, in import order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="regenerate"/> is <c>null</c>.</exception>
    public IReadOnlyList<ImportModuleResult> Create(Action regenerate)
    {
        ArgumentNullException.ThrowIfNull(regenerate);

        var results = new List<ImportModuleResult>();
        foreach (IImportModule module in _modules)
        {
            results.Add(module.Create(_shared));
            if (module.RegenerateAfter)
                regenerate();
        }

        return results;
    }
}
