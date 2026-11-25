using DxfToRevit.Core.Import;

namespace DxfToRevit.Addin.Settings;

/// <summary>A category check box of the import settings: the category and how many the DXF contains.</summary>
/// <param name="Category">Import category of a module.</param>
/// <param name="Count">Elements in the DXF; <c>null</c> when no DXF is loaded.</param>
internal sealed record ImportCategoryChoice(ImportCategory Category, int? Count);
