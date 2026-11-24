using netDxf;

namespace DxfToRevit.Core.Dxf;

/// <summary>
/// A loaded DXF drawing and its analysis: the input of the import modules. Walls, columns and
/// openings are analysed together by <see cref="DxfCenterLinePipeline"/> (they depend on each other:
/// openings bridge wall gaps and are attached to wall footprints, columns close wall gaps), so their
/// modules read <see cref="Analysis"/>. A module for another kind of element reads its entities
/// from <see cref="Document"/> with its own reader and layer rule, without changing the analysis.
/// </summary>
public sealed class DxfDrawing
{
    /// <summary>Creates the drawing.</summary>
    /// <param name="path">Path of the DXF file.</param>
    /// <param name="document">Loaded DXF document.</param>
    /// <param name="analysis">Analysis of the walls, columns and openings.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public DxfDrawing(string path, DxfDocument document, DxfCenterLinePipeline.Result analysis)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
    }

    /// <summary>Path of the DXF file.</summary>
    public string Path { get; }

    /// <summary>The loaded DXF document, for modules that read their own entities.</summary>
    public DxfDocument Document { get; }

    /// <summary>Analysis of the walls, columns and openings.</summary>
    public DxfCenterLinePipeline.Result Analysis { get; }
}
