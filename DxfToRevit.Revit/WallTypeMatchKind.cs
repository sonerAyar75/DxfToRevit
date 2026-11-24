namespace DxfToRevit.Revit;

/// <summary>How a wall type was matched.</summary>
public enum WallTypeMatchKind
{
    /// <summary>No type or family matched the key.</summary>
    NotFound,

    /// <summary>The key equals a type name (thickness is not checked).</summary>
    ExactName,

    /// <summary>The key is a family name and a type with the right thickness exists.</summary>
    FamilyWidthMatch,

    /// <summary>The family exists but no type has the thickness; a type should be duplicated and resized.</summary>
    DuplicateFromFamily
}
