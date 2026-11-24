using DxfToRevit.Core.Openings;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;

namespace DxfToRevit.Revit.Openings;

/// <summary>
/// Inputs for creating a Revit door, window or wall opening, in Revit internal units (feet).
/// Produced without the Revit API so the conversion can be unit-tested.
/// </summary>
public sealed class OpeningCreateArgs
{
    /// <summary>Identifier of the originating request.</summary>
    public string RequestId { get; }

    /// <summary>Host category.</summary>
    public OpeningCategory Category { get; }

    /// <summary>How the opening is created.</summary>
    public OpeningCreationMode CreationMode { get; }

    /// <summary>Revit type key for hosted families; <c>null</c> for wall openings.</summary>
    public string? TypeKey { get; }

    /// <summary>Opening centre X on the wall centerline, in feet.</summary>
    public double LocationXFeet { get; }

    /// <summary>Opening centre Y on the wall centerline, in feet.</summary>
    public double LocationYFeet { get; }

    /// <summary>Wall direction (unit vector) X component.</summary>
    public double DirectionX { get; }

    /// <summary>Wall direction (unit vector) Y component.</summary>
    public double DirectionY { get; }

    /// <summary>Opening width in feet.</summary>
    public double WidthFeet { get; }

    /// <summary>Opening height in feet.</summary>
    public double HeightFeet { get; }

    /// <summary>Sill height above the level, in feet.</summary>
    public double SillHeightFeet { get; }

    /// <summary>Opening width in millimetres (type matching and naming).</summary>
    public double WidthMm { get; }

    /// <summary>Opening height in millimetres (type matching and naming).</summary>
    public double HeightMm { get; }

    /// <summary>Base level name; <c>null</c> selects the lowest level.</summary>
    public string? LevelName { get; }

    /// <summary>Swing of hinged openings; <c>null</c> otherwise.</summary>
    public OpeningHanding? Handing { get; }

    /// <summary>Creates the arguments.</summary>
    /// <param name="requestId">Originating request id.</param>
    /// <param name="category">Host category.</param>
    /// <param name="creationMode">Creation mode.</param>
    /// <param name="typeKey">Revit type key.</param>
    /// <param name="locationXFeet">Centre X in feet.</param>
    /// <param name="locationYFeet">Centre Y in feet.</param>
    /// <param name="directionX">Wall direction X.</param>
    /// <param name="directionY">Wall direction Y.</param>
    /// <param name="widthFeet">Width in feet.</param>
    /// <param name="heightFeet">Height in feet.</param>
    /// <param name="sillHeightFeet">Sill height in feet.</param>
    /// <param name="widthMm">Width in millimetres.</param>
    /// <param name="heightMm">Height in millimetres.</param>
    /// <param name="levelName">Base level name.</param>
    /// <param name="handing">Swing, for hinged openings.</param>
    /// <exception cref="System.ArgumentException">The request id is empty.</exception>
    public OpeningCreateArgs(
        string requestId,
        OpeningCategory category,
        OpeningCreationMode creationMode,
        string? typeKey,
        double locationXFeet,
        double locationYFeet,
        double directionX,
        double directionY,
        double widthFeet,
        double heightFeet,
        double sillHeightFeet,
        double widthMm,
        double heightMm,
        string? levelName,
        OpeningHanding? handing)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request id cannot be empty.", nameof(requestId));

        RequestId = requestId;
        Category = category;
        CreationMode = creationMode;
        TypeKey = typeKey;
        LocationXFeet = locationXFeet;
        LocationYFeet = locationYFeet;
        DirectionX = directionX;
        DirectionY = directionY;
        WidthFeet = widthFeet;
        HeightFeet = heightFeet;
        SillHeightFeet = sillHeightFeet;
        WidthMm = widthMm;
        HeightMm = heightMm;
        LevelName = levelName;
        Handing = handing;
    }
}
