using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;

namespace DxfToRevit.Revit.Walls;

/// <summary>
/// Values passed to <c>Wall.Create</c>, testable without the Revit API. Lengths are in
/// <b>feet</b>; names are resolved by the host to levels and wall types.
/// </summary>
public sealed class WallCreateArgs
{
    /// <summary>Identifier of the originating request.</summary>
    public string RequestId { get; }

    /// <summary>Centerline start X in feet.</summary>
    public double StartXFeet { get; }

    /// <summary>Centerline start Y in feet.</summary>
    public double StartYFeet { get; }

    /// <summary>Centerline end X in feet.</summary>
    public double EndXFeet { get; }

    /// <summary>Centerline end Y in feet.</summary>
    public double EndYFeet { get; }

    /// <summary>Unconnected wall height in feet.</summary>
    public double HeightFeet { get; }

    /// <summary>Base offset in feet.</summary>
    public double BaseOffsetFeet { get; }

    /// <summary>Base level name; <c>null</c> selects the host default level.</summary>
    public string? LevelName { get; }

    /// <summary>Wall type key (e.g. <c>Basic Wall</c>).</summary>
    public string WallTypeKey { get; }

    /// <summary>Top level name; <c>null</c> means "Unconnected".</summary>
    public string? TopLevelName { get; }

    /// <summary>Structural flag passed to <c>Wall.Create</c>.</summary>
    public bool IsStructural { get; }

    /// <summary>Wall thickness in millimetres, used to resolve the wall type (not part of the curve).</summary>
    public double ThicknessMm { get; }

    /// <summary>Centerline length in feet.</summary>
    public double LengthFeet { get; }

    /// <summary>Creates the arguments.</summary>
    /// <param name="requestId">Identifier of the originating request.</param>
    /// <param name="startXFeet">Start X in feet.</param>
    /// <param name="startYFeet">Start Y in feet.</param>
    /// <param name="endXFeet">End X in feet.</param>
    /// <param name="endYFeet">End Y in feet.</param>
    /// <param name="heightFeet">Height in feet; must be positive.</param>
    /// <param name="baseOffsetFeet">Base offset in feet.</param>
    /// <param name="levelName">Base level name.</param>
    /// <param name="wallTypeKey">Wall type key.</param>
    /// <param name="isStructural">Structural flag.</param>
    /// <param name="thicknessMm">Thickness in millimetres; must be positive.</param>
    /// <param name="lengthFeet">Centerline length in feet.</param>
    /// <param name="topLevelName">Top level name; blank means "Unconnected".</param>
    /// <exception cref="System.ArgumentException">The request id or type key is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">The height or thickness is not positive.</exception>
    public WallCreateArgs(
        string requestId,
        double startXFeet,
        double startYFeet,
        double endXFeet,
        double endYFeet,
        double heightFeet,
        double baseOffsetFeet,
        string? levelName,
        string wallTypeKey,
        bool isStructural,
        double thicknessMm,
        double lengthFeet,
        string? topLevelName = null)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request id cannot be empty.", nameof(requestId));
        if (string.IsNullOrWhiteSpace(wallTypeKey))
            throw new ArgumentException("Wall type key cannot be empty.", nameof(wallTypeKey));
        if (heightFeet <= 0)
            throw new ArgumentOutOfRangeException(nameof(heightFeet));
        if (thicknessMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(thicknessMm));

        RequestId = requestId;
        StartXFeet = startXFeet;
        StartYFeet = startYFeet;
        EndXFeet = endXFeet;
        EndYFeet = endYFeet;
        HeightFeet = heightFeet;
        BaseOffsetFeet = baseOffsetFeet;
        LevelName = levelName;
        TopLevelName = string.IsNullOrWhiteSpace(topLevelName) ? null : topLevelName;
        WallTypeKey = wallTypeKey;
        IsStructural = isStructural;
        ThicknessMm = thicknessMm;
        LengthFeet = lengthFeet;
    }
}
