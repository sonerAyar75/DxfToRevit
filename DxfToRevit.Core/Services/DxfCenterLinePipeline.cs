using DxfToRevit.Core.Dxf;
using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.IO;
using DxfToRevit.Core.Models;
using netDxf;

namespace DxfToRevit.Core.Services;

/// <summary>
/// End-to-end DXF workflow: wall lines → face pairs → centerlines → junctions → collinear merge
/// → closed wall footprints, plus column reading and an optional DXF simulation output.
/// </summary>
/// <remarks>
/// <para>Stages, in order:</para>
/// <list type="number">
/// <item><description>Load the DXF once (<see cref="DxfReader"/>) and classify its LINE entities by layer (<see cref="DxfLineImportService"/>).</description></item>
/// <item><description>Find parallel face pairs at wall-thickness distance (<see cref="WallFacePairFinder"/>).</description></item>
/// <item><description>Build wall models with centerlines between the overlapping faces (<see cref="WallModelManager"/>).</description></item>
/// <item><description>Bridge centerline gaps that are open on one side only (<see cref="OpenSideCenterLineBridger"/>).</description></item>
/// <item><description>Extend centerlines to form L and T junctions (<see cref="WallCenterLineJunctionProcessor"/>).</description></item>
/// <item><description>Merge collinear pieces of equal thickness (<see cref="CollinearCenterLineMerger"/>).</description></item>
/// <item><description>Offset each centerline into a closed footprint (<see cref="WallFootprintBuilder"/>).</description></item>
/// <item><description>Read columns from COLUMN* layers (<see cref="DxfColumnReader"/>).</description></item>
/// <item><description>Optionally write the simulation DXF (<see cref="DxfSimulationWriter"/>).</description></item>
/// </list>
/// <para>Dependencies are injected through the primary constructor; the parameterless constructor uses default instances.</para>
/// </remarks>
public sealed class DxfCenterLinePipeline
{
    /// <summary>
    /// Thickness (mm) assigned to a centerline that cannot be matched to any wall model.
    /// </summary>
    public const double DefaultThicknessMm = 200.0;

    private readonly DxfLineImportService _importService;
    private readonly WallFacePairFinder _pairFinder;
    private readonly WallModelManager _wallManager;
    private readonly OpenSideCenterLineBridger _bridger;
    private readonly WallCenterLineJunctionProcessor _junctionProcessor;
    private readonly CollinearCenterLineMerger _collinearMerger;
    private readonly WallFootprintBuilder _footprintBuilder;
    private readonly DxfReader _dxfReader;
    private readonly DxfColumnReader _columnReader;
    private readonly DxfSimulationWriter _simulationWriter;

    /// <summary>Summary and products of one pipeline run.</summary>
    public sealed class Result
    {
        /// <summary>Path of the written simulation DXF; <c>null</c> when produced by <see cref="Analyze"/>.</summary>
        public string? OutputPath { get; init; }

        /// <summary>Number of LINE entities read from the DXF.</summary>
        public int InputLineCount { get; init; }

        /// <summary>Number of lines on wall layers.</summary>
        public int WallLineCount { get; init; }

        /// <summary>Number of accepted wall face pairs.</summary>
        public int PairCount { get; init; }

        /// <summary>Number of ambiguity messages (rejected candidate pairs).</summary>
        public int AmbiguityCount { get; init; }

        /// <summary>Number of centerlines after junction processing, before the collinear merge.</summary>
        public int CenterLineCountBeforeMerge { get; init; }

        /// <summary>Number of centerlines after the collinear merge.</summary>
        public int CenterLineCountAfterMerge { get; init; }

        /// <summary>Number of closed wall footprints.</summary>
        public int WallSolidCount { get; init; }

        /// <summary>Number of columns read from COLUMN* layers.</summary>
        public int ColumnCount { get; init; }

        /// <summary>Ambiguity messages from the face pair search.</summary>
        public IReadOnlyList<string> Ambiguities { get; init; } = Array.Empty<string>();

        /// <summary>Wall footprints (centerline + thickness) to create in the host.</summary>
        public IReadOnlyList<WallSolid2D> WallSolids { get; init; } = Array.Empty<WallSolid2D>();

        /// <summary>Columns read from the DXF.</summary>
        public IReadOnlyList<ColumnModel> Columns { get; init; } = Array.Empty<ColumnModel>();
    }

    /// <summary>Creates the pipeline with default collaborators (when there is no composition root).</summary>
    public DxfCenterLinePipeline()
        : this(
            new DxfLineImportService(),
            new WallFacePairFinder(),
            new WallModelManager(),
            new OpenSideCenterLineBridger(),
            new WallCenterLineJunctionProcessor(),
            new CollinearCenterLineMerger(maxGapMm: 5.0),
            new WallFootprintBuilder(),
            new DxfReader(),
            new DxfColumnReader(),
            new DxfSimulationWriter())
    {
    }

    /// <summary>Primary constructor for dependency injection and tests.</summary>
    /// <param name="importService">Classifies DXF lines.</param>
    /// <param name="pairFinder">Finds wall face pairs.</param>
    /// <param name="wallManager">Builds wall models from pairs.</param>
    /// <param name="bridger">Bridges one-side-open gaps.</param>
    /// <param name="junctionProcessor">Forms L/T junctions.</param>
    /// <param name="collinearMerger">Merges collinear pieces.</param>
    /// <param name="footprintBuilder">Builds closed footprints.</param>
    /// <param name="dxfReader">Loads the DXF file.</param>
    /// <param name="columnReader">Reads columns.</param>
    /// <param name="simulationWriter">Writes the simulation DXF.</param>
    /// <exception cref="ArgumentNullException">A collaborator is <c>null</c>.</exception>
    public DxfCenterLinePipeline(
        DxfLineImportService importService,
        WallFacePairFinder pairFinder,
        WallModelManager wallManager,
        OpenSideCenterLineBridger bridger,
        WallCenterLineJunctionProcessor junctionProcessor,
        CollinearCenterLineMerger collinearMerger,
        WallFootprintBuilder footprintBuilder,
        DxfReader dxfReader,
        DxfColumnReader columnReader,
        DxfSimulationWriter simulationWriter)
    {
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
        _pairFinder = pairFinder ?? throw new ArgumentNullException(nameof(pairFinder));
        _wallManager = wallManager ?? throw new ArgumentNullException(nameof(wallManager));
        _bridger = bridger ?? throw new ArgumentNullException(nameof(bridger));
        _junctionProcessor = junctionProcessor ?? throw new ArgumentNullException(nameof(junctionProcessor));
        _collinearMerger = collinearMerger ?? throw new ArgumentNullException(nameof(collinearMerger));
        _footprintBuilder = footprintBuilder ?? throw new ArgumentNullException(nameof(footprintBuilder));
        _dxfReader = dxfReader ?? throw new ArgumentNullException(nameof(dxfReader));
        _columnReader = columnReader ?? throw new ArgumentNullException(nameof(columnReader));
        _simulationWriter = simulationWriter ?? throw new ArgumentNullException(nameof(simulationWriter));
    }

    /// <summary>
    /// Analyses the DXF (wall footprints and columns) without writing anything to disk.
    /// Use this from callers that must not have side effects, such as the Revit add-in.
    /// </summary>
    /// <param name="inputPath">Path of the DXF file.</param>
    /// <returns>The pipeline result; <see cref="Result.OutputPath"/> is <c>null</c>.</returns>
    /// <exception cref="ArgumentException">The path is empty.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    public Result Analyze(string inputPath) => Execute(inputPath, outputPath: null, writeOutput: false);

    /// <summary>
    /// Runs the full pipeline and writes the simulation DXF. When <paramref name="outputPath"/> is
    /// omitted, <c>&lt;input&gt;_centerline.dxf</c> is written next to the input file.
    /// </summary>
    /// <param name="inputPath">Path of the DXF file.</param>
    /// <param name="outputPath">Optional output path.</param>
    /// <returns>The pipeline result, including the written output path.</returns>
    /// <exception cref="ArgumentException">The input path is empty.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    public Result Run(string inputPath, string? outputPath = null) =>
        Execute(inputPath, outputPath, writeOutput: true);

    /// <summary>Shared implementation of <see cref="Analyze"/> and <see cref="Run"/>.</summary>
    private Result Execute(string inputPath, string? outputPath, bool writeOutput)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
            throw new ArgumentException("Input path is required.", nameof(inputPath));

        if (!File.Exists(inputPath))
            throw new FileNotFoundException("DXF file not found.", inputPath);

        if (writeOutput)
        {
            outputPath ??= Path.Combine(
                Path.GetDirectoryName(inputPath) ?? ".",
                Path.GetFileNameWithoutExtension(inputPath) + "_centerline.dxf");
        }

        // The DXF is loaded once; lines, columns and the simulation output share the same document.
        DxfDocument document = _dxfReader.Read(inputPath);

        IReadOnlyList<ClassifiedDxfLine> classifiedLines = _importService.Import(document);
        int wallCount = classifiedLines.Count(l => l.IsWallLayer);

        WallFacePairFinderResult pairResult = _pairFinder.FindPairs(classifiedLines);

        var walls = new List<WallModel>();
        int index = 0;
        foreach (WallFacePair pair in pairResult.Pairs)
            walls.Add(_wallManager.CreateFromFacePair($"W{++index:D3}", pair));

        IReadOnlyList<LineSegment2D> bridgedCenterLines = _bridger.Bridge(walls, pairResult.Pairs);
        var thicknesses = AssignThicknesses(bridgedCenterLines, walls);

        // Junction processing keeps order and count, so the thickness list stays aligned.
        IReadOnlyList<LineSegment2D> afterJunction =
            _junctionProcessor.Process(bridgedCenterLines, thicknesses);

        // Thickness-aware merge: collinear walls of different thickness stay separate.
        IReadOnlyList<CollinearCenterLineMerger.MergedSegment> mergedWithThickness =
            _collinearMerger.Merge(afterJunction, thicknesses);

        var merged = mergedWithThickness.Select(m => m.Segment).ToList();
        var mergedThicknesses = mergedWithThickness.Select(m => m.ThicknessMm).ToList();
        IReadOnlyList<WallSolid2D> wallSolids = _footprintBuilder.BuildMany(merged, mergedThicknesses);

        IReadOnlyList<ColumnModel> columns = _columnReader.ReadColumns(document);

        if (writeOutput)
            _simulationWriter.Write(document, outputPath!, merged, wallSolids, columns);

        return new Result
        {
            OutputPath = writeOutput ? outputPath : null,
            InputLineCount = classifiedLines.Count,
            WallLineCount = wallCount,
            PairCount = pairResult.Pairs.Count,
            AmbiguityCount = pairResult.Ambiguities.Count,
            CenterLineCountBeforeMerge = afterJunction.Count,
            CenterLineCountAfterMerge = merged.Count,
            WallSolidCount = wallSolids.Count,
            ColumnCount = columns.Count,
            Ambiguities = pairResult.Ambiguities.ToList(),
            WallSolids = wallSolids,
            Columns = columns
        };
    }

    /// <summary>
    /// Assigns a thickness to every centerline by finding the closest parallel wall centerline
    /// (within 1 mm) that overlaps it. Ties prefer the thicker wall; centerlines without a match
    /// get <see cref="DefaultThicknessMm"/>.
    /// </summary>
    private static IReadOnlyList<double> AssignThicknesses(
        IReadOnlyList<LineSegment2D> centerLines,
        IReadOnlyList<WallModel> walls)
    {
        var result = new double[centerLines.Count];
        for (int i = 0; i < centerLines.Count; i++)
        {
            LineSegment2D cl = centerLines[i];
            double best = 0;
            double bestDist = double.MaxValue;

            foreach (WallModel wall in walls)
            {
                if (wall.CenterLine is null)
                    continue;
                if (!cl.IsParallelTo(wall.CenterLine))
                    continue;
                double d = cl.GetDistanceToParallelLine(wall.CenterLine);
                if (d > GeometryTolerance.PointDistanceMm)
                    continue;
                if (!cl.OverlapsAlongLength(wall.CenterLine) &&
                    !wall.CenterLine.OverlapsAlongLength(cl))
                    continue;

                if (d < bestDist)
                {
                    bestDist = d;
                    best = wall.ThicknessMm;
                }
                else if (Math.Abs(d - bestDist) < 1e-6)
                    best = Math.Max(best, wall.ThicknessMm);
            }

            result[i] = best > 0 ? best : DefaultThicknessMm;
        }

        return result;
    }
}
