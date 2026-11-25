namespace DxfToRevit.Revit.Import;

/// <summary>Kind of a Revit warning raised while committing an import.</summary>
public enum RevitWarningKind
{
    /// <summary>
    /// New elements overlap or duplicate elements in the model (overlapping walls, identical
    /// instances in the same place): usually the same drawing imported twice at the same place.
    /// </summary>
    Overlap,

    /// <summary>Any other warning.</summary>
    Other
}
