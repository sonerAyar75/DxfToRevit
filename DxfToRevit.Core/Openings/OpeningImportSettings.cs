namespace DxfToRevit.Core.Openings;

/// <summary>
/// The door, window and opening choices of the user, kept for the session: the type map (DXF
/// <c>TYPE</c> code → Revit type), the last type map file and the default heights used when a
/// marker has no height attribute.
/// </summary>
public sealed class OpeningImportSettings
{
    /// <summary>Type map used for doors and windows.</summary>
    public OpeningTypeMap TypeMap { get; set; } = OpeningTypeMap.Empty;

    /// <summary>Last type map file loaded or saved, offered again in the file dialogs.</summary>
    public string? TypeMapFilePath { get; set; }

    /// <summary>Default door height (cm).</summary>
    public double DoorHeightCm { get; set; } = OpeningDefinitionRegistry.DefaultDoorHeightCm;

    /// <summary>Default window height (cm).</summary>
    public double WindowHeightCm { get; set; } = OpeningDefinitionRegistry.DefaultWindowHeightCm;

    /// <summary>Default window sill height (cm).</summary>
    public double WindowSillCm { get; set; } = OpeningDefinitionRegistry.DefaultWindowSillCm;

    /// <summary>Default empty opening height (cm).</summary>
    public double EmptyOpeningHeightCm { get; set; } = OpeningDefinitionRegistry.DefaultEmptyOpeningHeightCm;

    /// <summary>The default heights, to tell whether they changed (cm).</summary>
    public (double Door, double Window, double Sill, double EmptyOpening) DefaultHeights =>
        (DoorHeightCm, WindowHeightCm, WindowSillCm, EmptyOpeningHeightCm);

    /// <summary>The opening kinds with these default heights, for the analysis.</summary>
    public OpeningDefinitionRegistry CreateRegistry() =>
        OpeningDefinitionRegistry.CreateDefault(DoorHeightCm, WindowHeightCm, WindowSillCm, EmptyOpeningHeightCm);

    /// <summary>The options of <see cref="OpeningImportModule"/> for these choices.</summary>
    public OpeningImportOptions ToOptions() => new(TypeMap);
}
