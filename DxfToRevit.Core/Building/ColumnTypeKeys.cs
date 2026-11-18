namespace DxfToRevit.Core.Building;

/// <summary>
/// Special values for <see cref="ColumnTypeProperties.TypeKey"/>. They are not family or type
/// names; instead they tell the host to choose or build the family itself.
/// </summary>
public static class ColumnTypeKeys
{
    /// <summary>
    /// Automatic round column family. The host uses a loaded column family that has a diameter
    /// parameter; otherwise it loads a round column family shipped with the add-in or found in the
    /// Revit library; if none is available it creates a cylindrical DirectShape instead.
    /// </summary>
    public const string AutoRoundFamily = "<auto-round-family>";

    /// <summary>Create circular columns as true cylindrical solids (DirectShape) without a family.</summary>
    public const string RoundDirectShape = "<round-directshape>";

    /// <summary>
    /// Build a loadable column family from the freeform section so the column can be bound to
    /// levels. If the family cannot be built, the host falls back to a DirectShape.
    /// </summary>
    public const string FreeformFamily = "<freeform-family>";
}
