using DxfToRevit.Core.Import;

namespace DxfToRevit.Core.Columns;

/// <summary>
/// How columns are imported; set by the host in <see cref="ImportPlanContext.Options"/>. The type of
/// rectangular columns comes from <see cref="ImportSessionOptions"/>.
/// </summary>
public sealed record ColumnImportOptions
{
    /// <summary>Type key of circular columns (see <see cref="ColumnTypeKeys"/>).</summary>
    public string CircularTypeKey { get; init; } = ColumnTypeKeys.AutoRoundFamily;

    /// <summary>Whether circular columns are replaced by square columns.</summary>
    public bool ConvertCircularToSquare { get; init; }

    /// <summary>Type key of the square columns that replace circular ones.</summary>
    public string? SquareTypeKey { get; init; }

    /// <summary>Type key of freeform columns (see <see cref="ColumnTypeKeys"/>).</summary>
    public string FreeformTypeKey { get; init; } = ColumnTypeKeys.FreeformFamily;

    /// <summary>Whether circular columns are imported.</summary>
    public bool IncludeCircular { get; init; } = true;

    /// <summary>Whether freeform columns are imported.</summary>
    public bool IncludeFreeform { get; init; } = true;
}
