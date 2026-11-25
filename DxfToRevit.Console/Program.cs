// DxfToRevit command line tool.
// Runs the centerline pipeline on a DXF file and writes the simulation DXF
// (WALL-CENTERLINE, WALL-SOLID and COLUMN-SIM layers). Services come from the
// dependency injection container (composition root: AddDxfToRevitCore).
//
// Usage: DxfToRevit.Console <input.dxf> [output.dxf]
// Exit code: 0 on success, 1 on a usage error or failure.

using DxfToRevit.Core.Composition;
using DxfToRevit.Core.Dxf;
using Microsoft.Extensions.DependencyInjection;

if (args.Length < 1)
{
    Console.WriteLine("Usage: DxfToRevit.Console <input.dxf> [output.dxf]");
    Console.WriteLine("  Centerline + WALL-SOLID + COLUMN-SIM (DI composition root).");
    return 1;
}

string inputPath = args[0];
string? outputPath = args.Length >= 2 ? args[1] : null;

try
{
    var services = new ServiceCollection();
    services.AddDxfToRevitCore();
    using ServiceProvider provider = services.BuildServiceProvider();

    var pipeline = provider.GetRequiredService<DxfCenterLinePipeline>();
    var result = pipeline.Run(inputPath, outputPath);

    Console.WriteLine($"Loaded {result.InputLineCount} lines ({result.WallLineCount} wall lines).");
    Console.WriteLine($"Found {result.PairCount} wall face pairs.");
    if (result.AmbiguityCount > 0)
    {
        Console.WriteLine($"Ambiguities: {result.AmbiguityCount}");
        foreach (string ambiguity in result.Ambiguities)
            Console.WriteLine($"  - {ambiguity}");
    }

    Console.WriteLine($"Centerlines after junctions: {result.CenterLineCountBeforeMerge}");
    Console.WriteLine($"Centerlines after collinear merge: {result.CenterLineCountAfterMerge}");
    Console.WriteLine($"Wall solids (closed footprints): {result.WallSolidCount}");
    Console.WriteLine($"Columns (closed polylines and circles on COLUMN*): {result.ColumnCount}");
    Console.WriteLine($"Written to: {result.OutputPath}");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return 1;
}
