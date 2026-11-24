using DxfToRevit.Core.Openings;
using netDxf;
using netDxf.Entities;

namespace DxfToRevit.Core.Dxf;

/// <summary>Result of <see cref="DxfOpeningReader.ReadMarkers"/>.</summary>
/// <param name="Markers">Usable opening markers.</param>
/// <param name="Issues">Markers that could not be read (invalid block content or orientation).</param>
public sealed record DxfOpeningReadResult(IReadOnlyList<OpeningMarker> Markers, IReadOnlyList<OpeningIssue> Issues);

/// <summary>
/// Reads opening marker blocks (doors, windows, empty openings) from the model space.
/// </summary>
/// <remarks>
/// A block reference is a marker when its block name — the true name for dynamic blocks, see
/// <see cref="DynamicBlockNameResolver"/> — or its layer matches an <see cref="OpeningDefinition"/>.
/// The marker geometry is the first closed 4-vertex polyline of the block, transformed to world
/// coordinates with the reference's position, rotation, scale and mirroring, so the width of a
/// stretched dynamic block is read from its actual geometry. The visible circles of the block are
/// hinge marks; circles hidden by a visibility state are ignored, so a block may switch the circle
/// position with named visibility states. <see cref="OpeningInterpreter"/> derives the swing side
/// and hinge end from the circle.
/// </remarks>
public sealed class DxfOpeningReader
{
    private readonly OpeningDefinitionRegistry _registry;

    /// <summary>Creates the reader.</summary>
    /// <param name="registry">Recognised opening kinds; the default kinds are used when <c>null</c>.</param>
    public DxfOpeningReader(OpeningDefinitionRegistry? registry = null)
    {
        _registry = registry ?? OpeningDefinitionRegistry.CreateDefault();
    }

    /// <summary>The recognised opening kinds.</summary>
    public OpeningDefinitionRegistry Registry => _registry;

    /// <summary>Reads every opening marker of the document.</summary>
    /// <param name="document">Loaded DXF document.</param>
    /// <returns>The markers and the issues of markers that could not be read.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public DxfOpeningReadResult ReadMarkers(DxfDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var resolver = new DynamicBlockNameResolver(document);
        var markers = new List<OpeningMarker>();
        var issues = new List<OpeningIssue>();

        foreach (Insert insert in document.Entities.Inserts)
        {
            if (insert.Block is null)
                continue;

            string blockName = resolver.Resolve(insert.Block);
            string layer = insert.Layer?.Name ?? string.Empty;
            OpeningDefinition? definition = _registry.Find(blockName, layer);
            if (definition is null)
                continue;

            var position = new Vector2(insert.Position.X, insert.Position.Y);
            if (Math.Abs(insert.Normal.Z - 1.0) > 1e-6)
            {
                issues.Add(OpeningIssue.Error(OpeningIssueCode.InvalidMarker, insert.Handle, position,
                    $"{definition.Name}: the block reference is not in the XY plane (extrusion direction is not +Z)."));
                continue;
            }

            if (!TryFindRectangle(insert.Block.Entities, out List<Vector2> localRectangle))
            {
                issues.Add(OpeningIssue.Error(OpeningIssueCode.InvalidMarker, insert.Handle, position,
                    $"{definition.Name}: block '{blockName}' has no closed 4-vertex rectangle."));
                continue;
            }

            Vector2 origin = new(insert.Block.Origin.X, insert.Block.Origin.Y);
            Vector2 ToWorld(Vector2 p) => Transform(p - origin, insert);

            // Circles hidden by the current visibility state of a dynamic block carry the
            // invisible flag (group code 60) in the block definition, also for the default state.
            List<Vector2> hingePoints = insert.Block.Entities.OfType<Circle>()
                .Where(c => c.IsVisible)
                .Select(c => ToWorld(new Vector2(c.Center.X, c.Center.Y)))
                .ToList();

            var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (netDxf.Entities.Attribute attribute in insert.Attributes)
            {
                if (!string.IsNullOrWhiteSpace(attribute.Tag))
                    attributes[attribute.Tag.Trim()] = attribute.Value ?? string.Empty;
            }

            markers.Add(new OpeningMarker(
                insert.Handle,
                blockName,
                layer,
                definition,
                localRectangle.Select(ToWorld).ToList(),
                hingePoints,
                attributes));
        }

        return new DxfOpeningReadResult(markers, issues);
    }

    /// <summary>
    /// Finds the first closed polyline with 4 distinct vertices (a repeated closing vertex is
    /// dropped). Bulges are ignored.
    /// </summary>
    private static bool TryFindRectangle(IEnumerable<EntityObject> entities, out List<Vector2> rectangle)
    {
        foreach (Polyline2D polyline in entities.OfType<Polyline2D>())
        {
            var vertices = polyline.Vertexes.Select(v => v.Position).ToList();
            if (vertices.Count == 5 && Vector2.Distance(vertices[0], vertices[4]) < 1e-6)
                vertices.RemoveAt(4);

            bool closed = polyline.IsClosed || vertices.Count == 4;
            if (closed && vertices.Count == 4)
            {
                rectangle = vertices;
                return true;
            }
        }

        rectangle = new List<Vector2>();
        return false;
    }

    /// <summary>Block coordinates (relative to the block origin) → world: scale (with mirroring), rotate, translate.</summary>
    private static Vector2 Transform(Vector2 local, Insert insert)
    {
        double x = local.X * insert.Scale.X;
        double y = local.Y * insert.Scale.Y;
        double angle = insert.Rotation * Math.PI / 180.0;
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);
        return new Vector2(
            insert.Position.X + x * cos - y * sin,
            insert.Position.Y + x * sin + y * cos);
    }
}
