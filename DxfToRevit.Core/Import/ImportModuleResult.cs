namespace DxfToRevit.Core.Import;

/// <summary>What a module created, failed to create and wants to report.</summary>
public sealed class ImportModuleResult
{
    /// <summary>Creates the result.</summary>
    /// <param name="name">Plural name used in the summary, for example <c>Walls</c>.</param>
    /// <param name="noteTitle">Title of the notes section, for example <c>Wall notes</c>.</param>
    /// <param name="included">Whether any category of the module was chosen for import.</param>
    /// <param name="entries">One entry per creation request.</param>
    /// <param name="sections">Extra summary sections of the module.</param>
    /// <param name="countSuffix">Text appended to the created count, for example the markers in the DXF.</param>
    /// <exception cref="ArgumentNullException">A list or name is <c>null</c>.</exception>
    public ImportModuleResult(
        string name,
        string noteTitle,
        bool included,
        IReadOnlyList<ImportEntry> entries,
        IReadOnlyList<ImportReportSection>? sections = null,
        string? countSuffix = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        NoteTitle = noteTitle ?? throw new ArgumentNullException(nameof(noteTitle));
        Included = included;
        Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        Sections = sections ?? Array.Empty<ImportReportSection>();
        CountSuffix = countSuffix;
    }

    /// <summary>Plural name used in the summary.</summary>
    public string Name { get; }

    /// <summary>Title of the notes section.</summary>
    public string NoteTitle { get; }

    /// <summary>Whether any category of the module was chosen for import.</summary>
    public bool Included { get; }

    /// <summary>One entry per creation request.</summary>
    public IReadOnlyList<ImportEntry> Entries { get; }

    /// <summary>Extra summary sections.</summary>
    public IReadOnlyList<ImportReportSection> Sections { get; }

    /// <summary>Text appended to the created count, or <c>null</c>.</summary>
    public string? CountSuffix { get; }

    /// <summary>Number of created elements.</summary>
    public int Created => Entries.Count(e => e.Success);

    /// <summary>Whether everything requested was created and no section reports an error.</summary>
    public bool IsComplete => Entries.All(e => e.Success) && Sections.All(s => !s.IsError);

    /// <summary>
    /// The result after the host committed: elements the host removed become failures with its
    /// reason, and notes produced at commit (for example a door that cannot flip) are attached to
    /// the created elements, by request id.
    /// </summary>
    /// <param name="removedReason">Failure message for a removed element id, or <c>null</c> when it was kept.</param>
    /// <param name="notes">Notes by request id.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public ImportModuleResult WithCommitOutcome(
        Func<string, string?> removedReason, IReadOnlyDictionary<string, string> notes)
    {
        ArgumentNullException.ThrowIfNull(removedReason);
        ArgumentNullException.ThrowIfNull(notes);

        List<ImportEntry> entries = Entries.Select(e =>
        {
            if (e.ElementId is null)
                return e;
            if (removedReason(e.ElementId) is string reason)
                return new ImportEntry(e.RequestId, false, null, reason);
            return notes.TryGetValue(e.RequestId, out string? note)
                ? e with { Message = e.Message is null ? note : $"{e.Message} {note}" }
                : e;
        }).ToList();

        return new ImportModuleResult(Name, NoteTitle, Included, entries, Sections, CountSuffix);
    }
}
