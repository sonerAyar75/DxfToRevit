using DxfToRevit.Core.Walls;
using netDxf;

namespace DxfToRevit.Core.Openings;

/// <summary>
/// Turns opening markers into <see cref="OpeningModel"/>s on the final wall footprints:
/// attaches each marker to its wall, reads its attributes, derives the door and window orientation
/// from the block's circles, rejects
/// overlapping openings and numbers the result per kind (<c>D001</c>, <c>W001</c>, <c>O001</c>).
/// </summary>
public sealed class OpeningInterpreter
{
    private readonly OpeningWallMatcher _matcher;
    private readonly OpeningAttributeParser _attributeParser;

    /// <summary>Creates the interpreter.</summary>
    /// <param name="matcher">Wall matcher; a default matcher is used when <c>null</c>.</param>
    /// <param name="attributeParser">Attribute parser; a default parser is used when <c>null</c>.</param>
    public OpeningInterpreter(OpeningWallMatcher? matcher = null, OpeningAttributeParser? attributeParser = null)
    {
        _matcher = matcher ?? new OpeningWallMatcher();
        _attributeParser = attributeParser ?? new OpeningAttributeParser();
    }

    /// <summary>Interprets the markers against the wall footprints.</summary>
    /// <param name="markers">Opening markers read from the DXF.</param>
    /// <param name="walls">Final wall footprints.</param>
    /// <returns>The openings to create and the issues found.</returns>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public OpeningInterpretation Interpret(IReadOnlyList<OpeningMarker> markers, IReadOnlyList<WallSolid2D> walls)
    {
        ArgumentNullException.ThrowIfNull(markers);
        ArgumentNullException.ThrowIfNull(walls);

        var issues = new List<OpeningIssue>();
        var placed = new List<(OpeningMarker Marker, OpeningPlacement Placement, OpeningAttributes Attributes, OpeningHanding? Handing)>();

        foreach (OpeningMarker marker in markers)
        {
            if (!_matcher.TryMatch(marker, walls, out OpeningPlacement? placement, issues))
                continue;

            OpeningAttributes attributes = _attributeParser.Parse(marker, issues);
            OpeningHanding? handing = marker.Definition.HandingMode == OpeningHandingMode.Hinged
                ? DeriveHanding(marker, placement!, walls[placement!.HostWallIndex], issues)
                : null;

            placed.Add((marker, placement!, attributes, handing));
        }

        var rejected = FindOverlaps(placed.Select(p => (p.Marker, p.Placement)).ToList(), issues);

        var openings = new List<OpeningModel>();
        var counters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in placed)
        {
            if (rejected.Contains(p.Marker))
                continue;

            string prefix = p.Marker.Definition.IdPrefix;
            int number = counters.TryGetValue(prefix, out int current) ? current + 1 : 1;
            counters[prefix] = number;
            openings.Add(new OpeningModel($"{prefix}{number:D3}", p.Marker, p.Placement, p.Attributes, p.Handing));
        }

        return new OpeningInterpretation(openings, issues);
    }

    /// <summary>
    /// Derives the orientation from the one circle of the marker: the wall face it lies on is the
    /// swing side and the half of the opening it lies in is the hinge end. The kind of leaf (double,
    /// sliding, multi-panel) is not read from the block; it comes from the family mapped to the
    /// <c>TYPE</c> code. Returns <c>null</c> (with a warning) when the block has no circle or more
    /// than one; a circle on the wall axis or in the middle of the opening leaves that part as
    /// Revit places it, with a warning.
    /// </summary>
    private OpeningHanding? DeriveHanding(
        OpeningMarker marker, OpeningPlacement placement, WallSolid2D wall, ICollection<OpeningIssue> issues)
    {
        string name = marker.Definition.Name;
        if (marker.HingePoints.Count != 1)
        {
            string reason = marker.HingePoints.Count == 0
                ? "no circle in the block"
                : $"{marker.HingePoints.Count} visible circles in the block, one is expected";
            issues.Add(OpeningIssue.Warning(OpeningIssueCode.MissingHinge, marker.Handle, marker.Center,
                $"{name}: {reason}; the orientation is left as Revit places it."));
            return null;
        }

        Vector2 u = placement.WallDirection;
        Vector2 n = new(-u.Y, u.X);
        Vector2 d = marker.HingePoints[0] - wall.CenterLine.Start;
        double s = Vector2.DotProduct(d, u);
        double t = Vector2.DotProduct(d, n);
        double tolerance = _matcher.ToleranceMm;

        if (Math.Abs(t) < tolerance)
        {
            issues.Add(OpeningIssue.Warning(OpeningIssueCode.MissingHinge, marker.Handle, marker.Center,
                $"{name}: the circle lies on the wall axis, so the swing side is unknown; the orientation is left as Revit places it."));
            return null;
        }

        SwingSide side = t > 0 ? SwingSide.Left : SwingSide.Right;
        double middle = (placement.StartOffsetMm + placement.EndOffsetMm) / 2;

        if (Math.Abs(s - middle) < tolerance)
        {
            issues.Add(OpeningIssue.Warning(OpeningIssueCode.MissingHinge, marker.Handle, marker.Center,
                $"{name}: the circle lies in the middle of the opening, so the hinge end is unknown; only the swing side is applied."));
            return new OpeningHanding(null, side);
        }

        return new OpeningHanding(s < middle ? HingeEnd.Start : HingeEnd.End, side);
    }

    /// <summary>Reports openings that overlap another opening on the same wall; returns their markers.</summary>
    private HashSet<OpeningMarker> FindOverlaps(
        IReadOnlyList<(OpeningMarker Marker, OpeningPlacement Placement)> placed, ICollection<OpeningIssue> issues)
    {
        var rejected = new HashSet<OpeningMarker>();
        foreach (var group in placed.GroupBy(p => p.Placement.HostWallIndex))
        {
            var items = group.OrderBy(p => p.Placement.StartOffsetMm).ToList();
            for (int i = 0; i < items.Count; i++)
            {
                for (int j = i + 1; j < items.Count; j++)
                {
                    if (items[j].Placement.StartOffsetMm >= items[i].Placement.EndOffsetMm - _matcher.ToleranceMm)
                        break;

                    foreach (var item in new[] { items[i], items[j] })
                    {
                        if (rejected.Add(item.Marker))
                        {
                            issues.Add(OpeningIssue.Error(OpeningIssueCode.Overlapping, item.Marker.Handle, item.Marker.Center,
                                $"{item.Marker.Definition.Name}: the marker overlaps another opening on the same wall."));
                        }
                    }
                }
            }
        }

        return rejected;
    }
}
