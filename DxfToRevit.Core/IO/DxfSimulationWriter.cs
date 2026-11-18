using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.Models;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;

namespace DxfToRevit.Core.IO;

/// <summary>
/// Writes the pre-Revit DXF simulation: the original drawing plus the computed centerlines,
/// closed wall footprints and column sections on dedicated layers, so the result can be checked
/// in any CAD viewer before importing into Revit.
/// </summary>
public sealed class DxfSimulationWriter
{
    /// <summary>Layer that receives the wall centerlines.</summary>
    public const string CenterLineLayer = "WALL-CENTERLINE";

    /// <summary>Layer that receives the closed wall footprints.</summary>
    public const string WallSolidLayer = "WALL-SOLID";

    /// <summary>Layer that receives the column sections.</summary>
    public const string ColumnSimLayer = "COLUMN-SIM";

    /// <summary>
    /// Loads <paramref name="inputPath"/>, adds the simulation entities and saves the result to
    /// <paramref name="outputPath"/>. The input file is not modified.
    /// </summary>
    /// <param name="inputPath">Source DXF path.</param>
    /// <param name="outputPath">Output DXF path; missing directories are created.</param>
    /// <param name="centerLines">Wall centerlines.</param>
    /// <param name="wallSolids">Wall footprints.</param>
    /// <param name="columns">Column sections.</param>
    /// <exception cref="ArgumentException">A path is empty.</exception>
    /// <exception cref="ArgumentNullException">A collection is <c>null</c>.</exception>
    /// <exception cref="FileNotFoundException">The input file does not exist.</exception>
    public void Write(
        string inputPath,
        string outputPath,
        IEnumerable<LineSegment2D> centerLines,
        IEnumerable<WallSolid2D> wallSolids,
        IEnumerable<ColumnModel> columns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(centerLines);
        ArgumentNullException.ThrowIfNull(wallSolids);
        ArgumentNullException.ThrowIfNull(columns);

        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Input DXF file was not found.", inputPath);

        Write(DxfDocument.Load(inputPath), outputPath, centerLines, wallSolids, columns);
    }

    /// <summary>
    /// Adds the simulation entities to an already loaded document and saves it to
    /// <paramref name="outputPath"/>. The document is modified in place.
    /// </summary>
    /// <param name="document">Document to extend (modified in place).</param>
    /// <param name="outputPath">Output DXF path; missing directories are created.</param>
    /// <param name="centerLines">Wall centerlines; degenerate segments are skipped.</param>
    /// <param name="wallSolids">Wall footprints.</param>
    /// <param name="columns">Column sections.</param>
    /// <exception cref="ArgumentException"><paramref name="outputPath"/> is empty.</exception>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public void Write(
        DxfDocument document,
        string outputPath,
        IEnumerable<LineSegment2D> centerLines,
        IEnumerable<WallSolid2D> wallSolids,
        IEnumerable<ColumnModel> columns)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(centerLines);
        ArgumentNullException.ThrowIfNull(wallSolids);
        ArgumentNullException.ThrowIfNull(columns);

        Layer clLayer = GetOrAddLayer(document, CenterLineLayer);
        Layer solidLayer = GetOrAddLayer(document, WallSolidLayer);
        Layer colLayer = GetOrAddLayer(document, ColumnSimLayer);

        foreach (LineSegment2D segment in centerLines)
        {
            if (segment.IsDegenerate)
                continue;

            document.Entities.Add(new Line(
                new Vector3(segment.Start.X, segment.Start.Y, 0),
                new Vector3(segment.End.X, segment.End.Y, 0))
            {
                Layer = clLayer
            });
        }

        foreach (WallSolid2D solid in wallSolids)
            document.Entities.Add(CreateClosedPolyline(solid.Outline, solidLayer));

        foreach (ColumnModel column in columns)
            document.Entities.Add(CreateClosedPolyline(column.Outline, colLayer));

        string? dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        document.Save(outputPath);
    }

    /// <summary>Returns the layer with the given name, creating it when missing.</summary>
    private static Layer GetOrAddLayer(DxfDocument document, string name)
    {
        if (document.Layers.Contains(name))
            return document.Layers[name];
        return document.Layers.Add(new Layer(name));
    }

    /// <summary>Creates a closed 2D polyline through the outline vertices on the given layer.</summary>
    private static Polyline2D CreateClosedPolyline(
        IReadOnlyList<Vector2> outline,
        Layer layer)
    {
        var points = outline.Select(p => new Vector2(p.X, p.Y)).ToList();
        return new Polyline2D(points, isClosed: true)
        {
            Layer = layer
        };
    }
}
