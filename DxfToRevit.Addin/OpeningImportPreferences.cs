using DxfToRevit.Core.Building;
using DxfToRevit.Core.Openings;

namespace DxfToRevit.Addin;

/// <summary>
/// Opening import choices remembered for the current Revit session: whether openings are imported,
/// the type map (DXF <c>TYPE</c> code → Revit type) and the default heights used when a marker has
/// no height attribute.
/// </summary>
internal static class OpeningImportPreferences
{
    /// <summary>Whether doors, windows and empty openings are imported.</summary>
    public static bool IncludeOpenings { get; set; } = true;

    /// <summary>Type map used for doors and windows.</summary>
    public static OpeningTypeMap TypeMap { get; set; } = OpeningTypeMap.Empty;

    /// <summary>Last type map file loaded or saved, offered again in the file dialogs.</summary>
    public static string? TypeMapFilePath { get; set; }

    /// <summary>Default door height in centimetres.</summary>
    public static double DoorHeightCm { get; set; } = OpeningDefinitionRegistry.DefaultDoorHeightCm;

    /// <summary>Default window height in centimetres.</summary>
    public static double WindowHeightCm { get; set; } = OpeningDefinitionRegistry.DefaultWindowHeightCm;

    /// <summary>Default window sill height in centimetres.</summary>
    public static double WindowSillCm { get; set; } = OpeningDefinitionRegistry.DefaultWindowSillCm;

    /// <summary>Default empty opening height in centimetres.</summary>
    public static double EmptyOpeningHeightCm { get; set; } = OpeningDefinitionRegistry.DefaultEmptyOpeningHeightCm;

    /// <summary>Opening kinds with the remembered default heights.</summary>
    public static OpeningDefinitionRegistry CreateRegistry() =>
        OpeningDefinitionRegistry.CreateDefault(DoorHeightCm, WindowHeightCm, WindowSillCm, EmptyOpeningHeightCm);
}
