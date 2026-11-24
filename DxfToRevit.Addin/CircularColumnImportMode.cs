namespace DxfToRevit.Addin;

/// <summary>How circular DXF columns are imported.</summary>
internal enum CircularColumnImportMode
{
    /// <summary>Use a round column family (selected, already loaded or loaded automatically).</summary>
    CircularFamily,

    /// <summary>Create a cylindrical DirectShape without a family.</summary>
    RoundSolid,

    /// <summary>Replace the circle by a square column of the same outside size.</summary>
    SquareFallback,

    /// <summary>Do not import circular columns.</summary>
    Skip
}
