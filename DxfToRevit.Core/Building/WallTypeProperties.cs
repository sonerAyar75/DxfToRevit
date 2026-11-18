namespace DxfToRevit.Core.Building;

/// <summary>
/// Host wall type description (Basic Wall oriented): which type to use and the thickness it must have.
/// </summary>
public sealed class WallTypeProperties
{
    /// <summary>Wall type key: a family name such as <c>Basic Wall</c>, or a specific type name.</summary>
    public string TypeKey { get; }

    /// <summary>Wall thickness in millimetres.</summary>
    public double ThicknessMm { get; }

    /// <summary>Whether the wall is structural.</summary>
    public bool IsStructural { get; }

    /// <summary>Creates the type description.</summary>
    /// <param name="thicknessMm">Wall thickness in millimetres; must be positive.</param>
    /// <param name="typeKey">Wall type key.</param>
    /// <param name="isStructural">Whether the wall is structural.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="thicknessMm"/> is not positive.</exception>
    /// <exception cref="ArgumentException"><paramref name="typeKey"/> is empty.</exception>
    public WallTypeProperties(
        double thicknessMm,
        string typeKey = "Basic Wall",
        bool isStructural = false)
    {
        if (thicknessMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(thicknessMm));
        if (string.IsNullOrWhiteSpace(typeKey))
            throw new ArgumentException("Type key cannot be empty.", nameof(typeKey));

        ThicknessMm = thicknessMm;
        TypeKey = typeKey;
        IsStructural = isStructural;
    }
}
