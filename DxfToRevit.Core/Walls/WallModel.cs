using DxfToRevit.Core.Geometry;

namespace DxfToRevit.Core.Walls;

/// <summary>
/// Platform-independent wall model (the intermediate representation before Revit).
/// Coordinates and sizes are in millimetres. A wall has a centerline, two faces, or both.
/// </summary>
public sealed class WallModel
{
    /// <summary>Wall identifier (e.g. <c>W001</c>).</summary>
    public string Id { get; }

    /// <summary>Source layer name.</summary>
    public string SourceLayer { get; }

    /// <summary>Wall centerline, or <c>null</c> when only faces are known.</summary>
    public LineSegment2D? CenterLine { get; }

    /// <summary>First wall face (overlapping part or full face), or <c>null</c>.</summary>
    public LineSegment2D? FaceA { get; }

    /// <summary>Second wall face, or <c>null</c>.</summary>
    public LineSegment2D? FaceB { get; }

    /// <summary>Wall thickness in millimetres.</summary>
    public double ThicknessMm { get; }

    /// <summary>Creates a wall model.</summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="sourceLayer">Layer name.</param>
    /// <param name="thicknessMm">Thickness in millimetres; must be positive.</param>
    /// <param name="centerLine">Optional centerline.</param>
    /// <param name="faceA">Optional face A (must be given together with <paramref name="faceB"/>).</param>
    /// <param name="faceB">Optional face B (must be given together with <paramref name="faceA"/>).</param>
    /// <exception cref="ArgumentException">
    /// A text value is empty, only one face is given, or neither a centerline nor two faces are given.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="thicknessMm"/> is not positive.</exception>
    public WallModel(
        string id,
        string sourceLayer,
        double thicknessMm,
        LineSegment2D? centerLine = null,
        LineSegment2D? faceA = null,
        LineSegment2D? faceB = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Wall ID cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(sourceLayer))
            throw new ArgumentException("Source layer cannot be empty.", nameof(sourceLayer));

        if (thicknessMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(thicknessMm), "Wall thickness must be greater than zero.");

        bool hasCenterLine = centerLine is not null;
        bool hasBothFaces = faceA is not null && faceB is not null;
        bool hasAnyFace = faceA is not null || faceB is not null;

        if (hasAnyFace && !hasBothFaces)
            throw new ArgumentException("Both wall faces must be provided together.");

        if (!hasCenterLine && !hasBothFaces)
            throw new ArgumentException("A wall must have either a centerline or two faces.");

        Id = id;
        SourceLayer = sourceLayer;
        ThicknessMm = thicknessMm;
        CenterLine = centerLine;
        FaceA = faceA;
        FaceB = faceB;
    }
}
