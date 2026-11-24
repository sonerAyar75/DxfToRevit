using DxfToRevit.Core.Openings;
using netDxf;

namespace DxfToRevit.Core.Building;

/// <summary>A straight wall already in the model.</summary>
/// <param name="ElementId">Element id in the host application.</param>
/// <param name="LevelName">Base level name.</param>
/// <param name="StartMm">Location line start (millimetres).</param>
/// <param name="EndMm">Location line end (millimetres).</param>
/// <param name="ThicknessMm">Wall thickness (millimetres).</param>
public sealed record ExistingWall(string ElementId, string? LevelName, Vector2 StartMm, Vector2 EndMm, double ThicknessMm);

/// <summary>A column already in the model.</summary>
/// <param name="ElementId">Element id in the host application.</param>
/// <param name="LevelName">Base level name.</param>
/// <param name="LocationMm">Plan location (millimetres).</param>
public sealed record ExistingColumn(string ElementId, string? LevelName, Vector2 LocationMm);

/// <summary>A door, window or wall opening already in the model.</summary>
/// <param name="ElementId">Element id in the host application.</param>
/// <param name="Category">Door, window or empty opening.</param>
/// <param name="LevelName">Level name (of the host wall for wall openings).</param>
/// <param name="LocationMm">Plan location (millimetres): the insertion point, or the centre of a wall opening.</param>
public sealed record ExistingOpening(string ElementId, OpeningCategory Category, string? LevelName, Vector2 LocationMm);

/// <summary>Snapshot of the walls, columns and openings of a model, read before an import.</summary>
public sealed class ExistingElements
{
    /// <summary>A model without elements.</summary>
    public static ExistingElements Empty { get; } = new(
        Array.Empty<ExistingWall>(), Array.Empty<ExistingColumn>(), Array.Empty<ExistingOpening>());

    /// <summary>Straight walls.</summary>
    public IReadOnlyList<ExistingWall> Walls { get; }

    /// <summary>Columns.</summary>
    public IReadOnlyList<ExistingColumn> Columns { get; }

    /// <summary>Doors, windows and wall openings.</summary>
    public IReadOnlyList<ExistingOpening> Openings { get; }

    /// <summary>Creates the snapshot.</summary>
    /// <exception cref="ArgumentNullException">A list is <c>null</c>.</exception>
    public ExistingElements(
        IReadOnlyList<ExistingWall> walls, IReadOnlyList<ExistingColumn> columns, IReadOnlyList<ExistingOpening> openings)
    {
        Walls = walls ?? throw new ArgumentNullException(nameof(walls));
        Columns = columns ?? throw new ArgumentNullException(nameof(columns));
        Openings = openings ?? throw new ArgumentNullException(nameof(openings));
    }
}

/// <summary>Requests split into those to create and those already in the model.</summary>
/// <typeparam name="T">Request type.</typeparam>
/// <param name="New">Requests with no matching element in the model.</param>
/// <param name="Existing">Request id → id of the element that is already in the model.</param>
public sealed record ExistingSplit<T>(IReadOnlyList<T> New, IReadOnlyDictionary<string, string> Existing);

/// <summary>
/// Finds the model element that an import request would duplicate, so that importing an updated
/// drawing again only creates what is new. Matching is geometric, on the same level:
/// <list type="bullet">
/// <item>a wall matches an existing wall of the same thickness whose location line covers the
/// requested centerline;</item>
/// <item>a column matches an existing column at the same plan location;</item>
/// <item>a door, window or opening matches an element of the same category at the same location.</item>
/// </list>
/// A wall that was lengthened or moved in the drawing does not match, so it is created again.
/// </summary>
public sealed class ExistingElementMatcher
{
    private readonly ExistingElements _existing;

    /// <summary>Creates the matcher.</summary>
    /// <param name="existing">Elements of the model.</param>
    /// <exception cref="ArgumentNullException"><paramref name="existing"/> is <c>null</c>.</exception>
    public ExistingElementMatcher(ExistingElements existing)
    {
        _existing = existing ?? throw new ArgumentNullException(nameof(existing));
    }

    /// <summary>Largest distance between a requested and an existing location (default 10 mm).</summary>
    public double ToleranceMm { get; init; } = 10.0;

    /// <summary>Largest thickness difference between a requested and an existing wall (default 1 mm).</summary>
    public double ThicknessToleranceMm { get; init; } = 1.0;

    /// <summary>The existing wall that covers the requested wall, if any.</summary>
    /// <param name="request">Wall request.</param>
    public string? FindWall(WallCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _existing.Walls.FirstOrDefault(w =>
                SameLevel(w.LevelName, request.Placement.LevelName) &&
                Math.Abs(w.ThicknessMm - request.Type.ThicknessMm) <= ThicknessToleranceMm &&
                DistanceToSegment(request.StartMm, w.StartMm, w.EndMm) <= ToleranceMm &&
                DistanceToSegment(request.EndMm, w.StartMm, w.EndMm) <= ToleranceMm)
            ?.ElementId;
    }

    /// <summary>The existing column at the requested location, if any.</summary>
    /// <param name="request">Column request.</param>
    public string? FindColumn(ColumnCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _existing.Columns.FirstOrDefault(c =>
                SameLevel(c.LevelName, request.Placement.LevelName) &&
                Vector2.Distance(c.LocationMm, request.CentroidMm) <= ToleranceMm)
            ?.ElementId;
    }

    /// <summary>The existing door, window or opening of the same category at the requested location, if any.</summary>
    /// <param name="request">Opening request.</param>
    public string? FindOpening(OpeningCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _existing.Openings.FirstOrDefault(o =>
                o.Category == request.Category &&
                SameLevel(o.LevelName, request.Placement.LevelName) &&
                Vector2.Distance(o.LocationMm, request.LocationMm) <= ToleranceMm)
            ?.ElementId;
    }

    /// <summary>Splits requests into new ones and ones already in the model.</summary>
    /// <typeparam name="T">Request type.</typeparam>
    /// <param name="requests">Requests.</param>
    /// <param name="id">Request id.</param>
    /// <param name="find">Existing element id for a request, or <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static ExistingSplit<T> Split<T>(IEnumerable<T> requests, Func<T, string> id, Func<T, string?> find)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(find);

        var fresh = new List<T>();
        var existing = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (T request in requests)
        {
            if (find(request) is string elementId)
                existing[id(request)] = elementId;
            else
                fresh.Add(request);
        }

        return new ExistingSplit<T>(fresh, existing);
    }

    private static bool SameLevel(string? a, string? b) =>
        string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static double DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        double lengthSquared = Vector2.DotProduct(ab, ab);
        if (lengthSquared < 1e-12)
            return Vector2.Distance(p, a);

        double t = Math.Clamp(Vector2.DotProduct(p - a, ab) / lengthSquared, 0.0, 1.0);
        return Vector2.Distance(p, a + ab * t);
    }
}
