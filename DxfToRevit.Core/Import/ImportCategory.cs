namespace DxfToRevit.Core.Import;

/// <summary>
/// A kind of element the user can choose to import, shown as one check box in the import settings
/// (for example walls or doors). A module imports one or more categories.
/// </summary>
/// <param name="Key">Stable identifier, used to remember the choice.</param>
/// <param name="Name">Plural display name, for example <c>Walls</c>.</param>
public sealed record ImportCategory(string Key, string Name);
