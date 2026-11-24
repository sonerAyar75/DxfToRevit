namespace DxfToRevit.Core.Openings;

/// <summary>
/// Business rules an opening request must satisfy before it is sent to a host: minimum width and
/// height, a non-negative sill, and a Revit type for hosted families.
/// </summary>
public sealed class OpeningCreationValidator
{
    /// <summary>Minimum opening width in millimetres.</summary>
    public double MinWidthMm { get; }

    /// <summary>Minimum opening height in millimetres.</summary>
    public double MinHeightMm { get; }

    /// <summary>Creates the validator.</summary>
    /// <param name="minWidthMm">Minimum width (mm).</param>
    /// <param name="minHeightMm">Minimum height (mm).</param>
    /// <exception cref="ArgumentOutOfRangeException">A limit is negative.</exception>
    public OpeningCreationValidator(double minWidthMm = 100, double minHeightMm = 100)
    {
        if (minWidthMm < 0) throw new ArgumentOutOfRangeException(nameof(minWidthMm));
        if (minHeightMm < 0) throw new ArgumentOutOfRangeException(nameof(minHeightMm));
        MinWidthMm = minWidthMm;
        MinHeightMm = minHeightMm;
    }

    /// <summary>Checks a request.</summary>
    /// <param name="request">Request to check.</param>
    /// <returns><c>null</c> when valid; otherwise an error message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    public string? Validate(OpeningCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.WidthMm < MinWidthMm)
            return $"Opening width must be at least {MinWidthMm} mm (got {request.WidthMm:F0}).";
        if (request.HeightMm < MinHeightMm)
            return $"Opening height must be at least {MinHeightMm} mm (got {request.HeightMm:F0}).";
        if (request.SillHeightMm < 0)
            return "Sill height cannot be negative.";
        if (request.CreationMode == OpeningCreationMode.HostedFamily && request.TypeKey is null)
        {
            string code = string.IsNullOrEmpty(request.TypeCode) ? "(none)" : request.TypeCode;
            return $"No Revit type is mapped for {request.Category} type code {code}. " +
                   "Add it to the opening type table.";
        }

        return null;
    }
}
