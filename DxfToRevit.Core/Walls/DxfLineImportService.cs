using DxfToRevit.Core.Dxf;
using netDxf;

namespace DxfToRevit.Core.Walls;

/// <summary>
/// Reads a DXF file (or an already loaded document), extracts its LINE entities and classifies
/// them by layer, producing <see cref="ClassifiedDxfLine"/>s.
/// </summary>
public sealed class DxfLineImportService
{
    private readonly DxfReader _dxfReader;
    private readonly DxfGeometryReader _geometryReader;
    private readonly DxfLineClassificationService _classificationService;

    /// <summary>Creates the service with default collaborators (for scripts and simple callers).</summary>
    public DxfLineImportService()
        : this(new DxfReader(), new DxfGeometryReader(), new DxfLineClassificationService())
    {
    }

    /// <summary>Primary constructor for composition / dependency injection.</summary>
    /// <param name="dxfReader">Loads DXF files.</param>
    /// <param name="geometryReader">Extracts LINE entities.</param>
    /// <param name="classificationService">Classifies lines by layer.</param>
    /// <exception cref="ArgumentNullException">A collaborator is <c>null</c>.</exception>
    public DxfLineImportService(
        DxfReader dxfReader,
        DxfGeometryReader geometryReader,
        DxfLineClassificationService classificationService)
    {
        _dxfReader = dxfReader ?? throw new ArgumentNullException(nameof(dxfReader));
        _geometryReader = geometryReader ?? throw new ArgumentNullException(nameof(geometryReader));
        _classificationService = classificationService ?? throw new ArgumentNullException(nameof(classificationService));
    }

    /// <summary>Loads the file, extracts its LINE entities and classifies them by layer.</summary>
    /// <param name="filePath">Path of the DXF file.</param>
    /// <returns>Classified lines.</returns>
    /// <exception cref="ArgumentException">The path is empty.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidDataException">The file is not valid DXF.</exception>
    public IReadOnlyList<ClassifiedDxfLine> Import(string filePath)
    {
        var document = _dxfReader.Read(filePath);
        return Import(document);
    }

    /// <summary>
    /// Extracts and classifies the LINE entities of an already loaded document, so the same DXF
    /// does not have to be read from disk again.
    /// </summary>
    /// <param name="document">Loaded DXF document.</param>
    /// <returns>Classified lines.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public IReadOnlyList<ClassifiedDxfLine> Import(DxfDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var lines = _geometryReader.ReadLines(document);
        return _classificationService.Classify(lines);
    }
}
