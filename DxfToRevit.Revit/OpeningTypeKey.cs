namespace DxfToRevit.Revit;

/// <summary>
/// A Revit type reference used in the opening type map: <c>Family::Type</c>, or a bare type name
/// (any family). Parsed without the Revit API.
/// </summary>
/// <param name="FamilyName">Family name; <c>null</c> when only a type name was given.</param>
/// <param name="TypeName">Type name.</param>
public sealed record OpeningTypeKey(string? FamilyName, string TypeName)
{
    /// <summary>Separator between family and type names.</summary>
    public const string Separator = "::";

    /// <summary>Parses a key; returns <c>false</c> for a blank key or a blank type name.</summary>
    /// <param name="key">Key text.</param>
    /// <param name="result">Parsed key.</param>
    public static bool TryParse(string? key, out OpeningTypeKey? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(key))
            return false;

        int index = key.IndexOf(Separator, StringComparison.Ordinal);
        if (index < 0)
        {
            result = new OpeningTypeKey(null, key.Trim());
            return true;
        }

        string family = key[..index].Trim();
        string type = key[(index + Separator.Length)..].Trim();
        if (type.Length == 0)
            return false;

        result = new OpeningTypeKey(family.Length == 0 ? null : family, type);
        return true;
    }

    /// <summary>Formats a key from family and type names.</summary>
    /// <param name="familyName">Family name.</param>
    /// <param name="typeName">Type name.</param>
    public static string Format(string familyName, string typeName) => $"{familyName}{Separator}{typeName}";

    /// <summary>Whether a type with these names is the one referenced (case-insensitive).</summary>
    /// <param name="familyName">Candidate family name.</param>
    /// <param name="typeName">Candidate type name.</param>
    public bool Matches(string familyName, string typeName) =>
        string.Equals(TypeName, typeName?.Trim(), StringComparison.OrdinalIgnoreCase) &&
        (FamilyName is null || string.Equals(FamilyName, familyName?.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    public override string ToString() => FamilyName is null ? TypeName : Format(FamilyName, TypeName);
}
