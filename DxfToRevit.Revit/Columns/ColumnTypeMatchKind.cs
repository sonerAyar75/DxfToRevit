namespace DxfToRevit.Revit.Columns;

/// <summary>How a column type was matched.</summary>
public enum ColumnTypeMatchKind
{
    /// <summary>No type or family matched the key.</summary>
    NotFound,

    /// <summary>The key equals a type name (dimensions are not checked).</summary>
    ExactName,

    /// <summary>The key is a family name and a type with the right dimensions exists.</summary>
    FamilySizeMatch,

    /// <summary>The family exists but no type has the dimensions; a type should be duplicated and resized.</summary>
    DuplicateFromFamily
}
