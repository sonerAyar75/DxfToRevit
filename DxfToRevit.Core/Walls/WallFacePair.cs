using DxfToRevit.Core.Geometry;

namespace DxfToRevit.Core.Walls;

/// <summary>
/// Two parallel wall faces on the same layer whose distance equals the wall thickness.
/// This is the basic input for computing a wall centerline.
/// </summary>
public sealed class WallFacePair
{
    /// <summary>First wall face.</summary>
    public LineSegment2D FaceA { get; }

    /// <summary>Second wall face.</summary>
    public LineSegment2D FaceB { get; }

    /// <summary>Source layer name.</summary>
    public string SourceLayer { get; }

    /// <summary>Wall thickness in millimetres.</summary>
    public double ThicknessMm { get; }

    /// <summary>DXF handle of face A.</summary>
    public string FaceAHandle { get; }

    /// <summary>DXF handle of face B.</summary>
    public string FaceBHandle { get; }

    /// <summary>All handles that make up face A (several when collinear segments were combined).</summary>
    public IReadOnlyList<string> FaceAHandles { get; }

    /// <summary>All handles that make up face B.</summary>
    public IReadOnlyList<string> FaceBHandles { get; }

    /// <summary>Creates a face pair.</summary>
    /// <param name="faceA">First face.</param>
    /// <param name="faceB">Second face.</param>
    /// <param name="sourceLayer">Layer name.</param>
    /// <param name="thicknessMm">Wall thickness in millimetres; must be positive.</param>
    /// <param name="faceAHandle">Handle of face A.</param>
    /// <param name="faceBHandle">Handle of face B.</param>
    /// <param name="faceAHandles">Optional list of all handles of face A; defaults to <paramref name="faceAHandle"/>.</param>
    /// <param name="faceBHandles">Optional list of all handles of face B; defaults to <paramref name="faceBHandle"/>.</param>
    /// <exception cref="ArgumentNullException">A face is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The layer or a handle is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="thicknessMm"/> is not positive.</exception>
    public WallFacePair(
        LineSegment2D faceA,
        LineSegment2D faceB,
        string sourceLayer,
        double thicknessMm,
        string faceAHandle,
        string faceBHandle,
        IEnumerable<string>? faceAHandles = null,
        IEnumerable<string>? faceBHandles = null)
    {
        ArgumentNullException.ThrowIfNull(faceA);
        ArgumentNullException.ThrowIfNull(faceB);

        if (string.IsNullOrWhiteSpace(sourceLayer))
            throw new ArgumentException("Source layer cannot be empty.", nameof(sourceLayer));

        if (thicknessMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(thicknessMm));

        if (string.IsNullOrWhiteSpace(faceAHandle))
            throw new ArgumentException("Face A handle cannot be empty.", nameof(faceAHandle));

        if (string.IsNullOrWhiteSpace(faceBHandle))
            throw new ArgumentException("Face B handle cannot be empty.", nameof(faceBHandle));

        FaceA = faceA;
        FaceB = faceB;
        SourceLayer = sourceLayer;
        ThicknessMm = thicknessMm;
        FaceAHandle = faceAHandle;
        FaceBHandle = faceBHandle;
        FaceAHandles = (faceAHandles ?? new[] { faceAHandle }).ToList().AsReadOnly();
        FaceBHandles = (faceBHandles ?? new[] { faceBHandle }).ToList().AsReadOnly();
    }
}
