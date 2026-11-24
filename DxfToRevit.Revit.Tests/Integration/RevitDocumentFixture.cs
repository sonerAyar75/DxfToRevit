namespace DxfToRevit.Revit.Tests.Integration;

/// <summary>
/// Source of the document context for integration tests.
/// An add-in host binds its <see cref="RevitApiDocumentContext"/> instance here.
/// </summary>
public sealed class RevitDocumentFixture
{
    /// <summary>
    /// Test run’a enjekte edilen context; yoksa null.
    /// </summary>
    public IRevitDocumentContext? Context { get; init; }

    public bool IsAvailable => Context?.IsAvailable == true;

    public static string SkipReason { get; } =
        "Revit Document integration requires injecting RevitApiDocumentContext with an open Document.";
}
