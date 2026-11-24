namespace DxfToRevit.Core.Columns;

/// <summary>
/// Host column type description: which family/type to use and the section dimensions it must have.
/// </summary>
/// <remarks>
/// Meaningful dimensions depend on <see cref="SectionKind"/>:
/// <list type="bullet">
/// <item><description><see cref="ColumnSectionKind.Rectangular"/>: <see cref="WidthMm"/> and <see cref="DepthMm"/>.</description></item>
/// <item><description><see cref="ColumnSectionKind.Circular"/>: <see cref="RadiusMm"/> (width and depth hold the diameter).</description></item>
/// <item><description><see cref="ColumnSectionKind.Freeform"/>: the request outline; width/depth are the bounding box.</description></item>
/// </list>
/// </remarks>
public sealed class ColumnTypeProperties
{
    /// <summary>
    /// Family/type key. Either a family or type name, a <c>Family::Type</c> pair selected in the UI,
    /// or one of the special values in <see cref="ColumnTypeKeys"/>.
    /// </summary>
    public string TypeKey { get; }

    /// <summary>Section width in millimetres (local X of the column), when known.</summary>
    public double? WidthMm { get; }

    /// <summary>Section depth in millimetres (local Y of the column), when known.</summary>
    public double? DepthMm { get; }

    /// <summary>Radius in millimetres for circular sections; otherwise <c>null</c>.</summary>
    public double? RadiusMm { get; }

    /// <summary>Whether the column should be created as a structural column.</summary>
    public bool IsStructural { get; }

    /// <summary>Section classification.</summary>
    public ColumnSectionKind SectionKind { get; }

    /// <summary>Diameter in millimetres (twice <see cref="RadiusMm"/>), or <c>null</c> for non-circular sections.</summary>
    public double? DiameterMm => RadiusMm is double r ? 2 * r : null;

    /// <summary>Creates the type description.</summary>
    /// <param name="typeKey">Family/type key (see <see cref="TypeKey"/>).</param>
    /// <param name="widthMm">Section width in millimetres; must be positive when given.</param>
    /// <param name="depthMm">Section depth in millimetres; must be positive when given.</param>
    /// <param name="isStructural">Whether the column is structural.</param>
    /// <param name="sectionKind">Section classification.</param>
    /// <param name="radiusMm">Radius for circular sections; must be positive when given.</param>
    /// <exception cref="ArgumentException"><paramref name="typeKey"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A given dimension is zero or negative.</exception>
    public ColumnTypeProperties(
        string typeKey = "Rectangular Column",
        double? widthMm = null,
        double? depthMm = null,
        bool isStructural = true,
        ColumnSectionKind sectionKind = ColumnSectionKind.Rectangular,
        double? radiusMm = null)
    {
        if (string.IsNullOrWhiteSpace(typeKey))
            throw new ArgumentException("Type key cannot be empty.", nameof(typeKey));
        if (widthMm is <= 0)
            throw new ArgumentOutOfRangeException(nameof(widthMm));
        if (depthMm is <= 0)
            throw new ArgumentOutOfRangeException(nameof(depthMm));
        if (radiusMm is <= 0)
            throw new ArgumentOutOfRangeException(nameof(radiusMm));

        TypeKey = typeKey;
        WidthMm = widthMm;
        DepthMm = depthMm;
        RadiusMm = radiusMm;
        IsStructural = isStructural;
        SectionKind = sectionKind;
    }
}
