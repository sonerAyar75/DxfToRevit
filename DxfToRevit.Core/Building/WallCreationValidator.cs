namespace DxfToRevit.Core.Building;

/// <summary>
/// Business rules a wall request must satisfy before it is sent to a host:
/// positive thickness, a minimum centerline length and a positive height when one is given.
/// </summary>
public sealed class WallCreationValidator
{
    /// <summary>Minimum centerline length in millimetres.</summary>
    public double MinLengthMm { get; }

    /// <summary>Creates the validator.</summary>
    /// <param name="minLengthMm">Minimum centerline length in millimetres.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="minLengthMm"/> is negative.</exception>
    public WallCreationValidator(double minLengthMm = 1.0)
    {
        if (minLengthMm < 0)
            throw new ArgumentOutOfRangeException(nameof(minLengthMm));
        MinLengthMm = minLengthMm;
    }

    /// <summary>Checks a request against the rules.</summary>
    /// <param name="request">Request to check.</param>
    /// <returns><c>null</c> when valid; otherwise a human-readable error message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    public string? Validate(WallCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ThicknessMm <= 0)
            return "Thickness must be greater than zero.";

        if (request.CenterLine.IsDegenerate || request.LengthMm < MinLengthMm)
            return $"Centerline length must be at least {MinLengthMm} mm.";

        if (request.Placement.HeightMm is <= 0)
            return "Height must be greater than zero when specified.";

        return null;
    }
}
