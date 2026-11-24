using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Models;
using netDxf;

namespace DxfToRevit.Core.Openings;

/// <summary>Result of <see cref="OpeningInterpreter.Interpret"/>.</summary>
/// <param name="Openings">Openings that can be created, in marker order.</param>
/// <param name="Issues">Errors (opening skipped) and warnings (opening created).</param>
public sealed record OpeningInterpretation(IReadOnlyList<OpeningModel> Openings, IReadOnlyList<OpeningIssue> Issues);

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
    /// Part of the opening width, at each end, in which a circle counts as a hinge at that end
    /// (default one third). A circle between the two end zones marks a side only.
    /// </summary>
    public double HingeEndZoneFraction { get; init; } = 1.0 / 3.0;

    /// <summary>
    /// Derives the orientation from the circles of the marker:
    /// <list type="bullet">
    /// <item>the swing side is the wall face on which the circles lie;</item>
    /// <item>circles near both ends → <see cref="OpeningLeafLayout.DoubleHinged"/> (side only);</item>
    /// <item>circles near one end only → <see cref="OpeningLeafLayout.SingleHinged"/> with that hinge end;</item>
    /// <item>circles only in the middle → <see cref="OpeningLeafLayout.SideOnly"/>.</item>
    /// </list>
    /// Returns <c>null</c> (with a warning) when the block has no circle.
    /// </summary>
    private OpeningHanding? DeriveHanding(
        OpeningMarker marker, OpeningPlacement placement, WallSolid2D wall, ICollection<OpeningIssue> issues)
    {
        string name = marker.Definition.Name;
        if (!marker.HasHingeCircle)
        {
            issues.Add(OpeningIssue.Warning(OpeningIssueCode.MissingHinge, marker.Handle, marker.Center,
                $"{name}: no circle in the block; the orientation is left as Revit places it."));
            return null;
        }

        Vector2 u = placement.WallDirection;
        Vector2 n = new(-u.Y, u.X);
        double zone = placement.WidthMm * HingeEndZoneFraction;
        int left = 0, right = 0;
        bool atStart = false, atEnd = false;

        foreach (Vector2 hinge in marker.HingePoints)
        {
            Vector2 d = hinge - wall.CenterLine.Start;
            double s = Vector2.DotProduct(d, u);
            double t = Vector2.DotProduct(d, n);

            if (t >= _matcher.ToleranceMm)
                left++;
            else if (t <= -_matcher.ToleranceMm)
                right++;

            if (s <= placement.StartOffsetMm + zone)
                atStart = true;
            else if (s >= placement.EndOffsetMm - zone)
                atEnd = true;
        }

        if (left == 0 && right == 0)
        {
            issues.Add(OpeningIssue.Warning(OpeningIssueCode.MissingHinge, marker.Handle, marker.Center,
                $"{name}: the circle lies on the wall axis, so the swing side is unknown; the left side is assumed."));
        }
        else if (left > 0 && right > 0)
        {
            issues.Add(OpeningIssue.Warning(OpeningIssueCode.MissingHinge, marker.Handle, marker.Center,
                $"{name}: circles lie on both faces of the wall; the face with more circles is used " +
                "(the left face on a tie)."));
        }

        SwingSide side = right > left ? SwingSide.Right : SwingSide.Left;

        if (atStart && atEnd)
            return OpeningHanding.Double(side);
        if (atStart)
            return OpeningHanding.Single(HingeEnd.Start, side);
        if (atEnd)
            return OpeningHanding.Single(HingeEnd.End, side);
        return OpeningHanding.SideOnly(side);
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
