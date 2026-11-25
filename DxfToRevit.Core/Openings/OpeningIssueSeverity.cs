namespace DxfToRevit.Core.Openings;

/// <summary>Severity of an <see cref="OpeningIssue"/>.</summary>
public enum OpeningIssueSeverity
{
    /// <summary>The opening is created; the user should check it.</summary>
    Warning,

    /// <summary>The opening is not created.</summary>
    Error
}
