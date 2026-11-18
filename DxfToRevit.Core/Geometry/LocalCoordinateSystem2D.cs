using netDxf;

namespace DxfToRevit.Core.Geometry;

/// <summary>
/// 2D local frame defined by an origin and an X-axis direction.
/// Y is the left-hand perpendicular of the unit X axis.
/// </summary>
public sealed class LocalCoordinateSystem2D
{
    private readonly Vector2 _origin;
    private readonly Vector2 _unitX;
    private readonly Vector2 _unitY;

    /// <summary>Creates the frame from an origin and the direction of its X axis.</summary>
    /// <param name="origin">World-space origin of the local frame.</param>
    /// <param name="xAxisDirection">Direction of the local X axis (need not be unit length).</param>
    public LocalCoordinateSystem2D(Vector2 origin, Vector2 xAxisDirection)
    {
        double length = xAxisDirection.Modulus();
        if (length < 1e-12)
            throw new ArgumentException("X axis direction must be non-zero.", nameof(xAxisDirection));

        _origin = origin;
        _unitX = xAxisDirection / length;
        _unitY = new Vector2(-_unitX.Y, _unitX.X);
    }

    /// <summary>Transforms a world point into local coordinates.</summary>
    public Vector2 ToLocal(Vector2 worldPoint)
    {
        Vector2 offset = worldPoint - _origin;
        return new Vector2(
            Vector2.DotProduct(offset, _unitX),
            Vector2.DotProduct(offset, _unitY));
    }

    /// <summary>Transforms a local point into world coordinates.</summary>
    public Vector2 ToWorld(Vector2 localPoint) =>
        _origin + _unitX * localPoint.X + _unitY * localPoint.Y;
}
