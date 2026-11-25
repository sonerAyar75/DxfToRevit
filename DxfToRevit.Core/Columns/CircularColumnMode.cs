namespace DxfToRevit.Core.Columns;

/// <summary>How circular columns are imported.</summary>
public enum CircularColumnMode
{
    /// <summary>A circular column family (a chosen type, or automatic: loaded or from the library).</summary>
    CircularFamily,

    /// <summary>A cylindrical solid (DirectShape), no family needed.</summary>
    RoundSolid,

    /// <summary>A square column whose side equals the diameter.</summary>
    SquareFallback,

    /// <summary>Circular columns are not imported.</summary>
    Skip
}
