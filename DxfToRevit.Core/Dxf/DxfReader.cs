using netDxf;

namespace DxfToRevit.Core.Dxf;

/// <summary>
/// Loads DXF files from disk into a netDxf <see cref="DxfDocument"/>, with path validation and
/// consistent error reporting.
/// </summary>
public sealed class DxfReader
{
    /// <summary>Loads a DXF file synchronously.</summary>
    /// <param name="filePath">Path of the DXF file.</param>
    /// <returns>The loaded document.</returns>
    /// <exception cref="ArgumentException"><paramref name="filePath"/> is empty.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidDataException">The file could not be parsed as DXF.</exception>
    public DxfDocument Read(string filePath)
    {
        ValidatePath(filePath);
        return Load(filePath);
    }

    /// <summary>
    /// Loads a DXF file on a worker thread so large files do not block the UI thread.
    /// </summary>
    /// <param name="filePath">Path of the DXF file.</param>
    /// <param name="cancellationToken">Cancels the operation before loading starts.</param>
    /// <returns>The loaded document.</returns>
    /// <exception cref="ArgumentException"><paramref name="filePath"/> is empty.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidDataException">The file could not be parsed as DXF.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled before loading started.</exception>
    public async Task<DxfDocument> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ValidatePath(filePath);

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Load(filePath);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Loads the document and wraps parser failures in <see cref="InvalidDataException"/>.</summary>
    private static DxfDocument Load(string filePath)
    {
        try
        {
            return DxfDocument.Load(filePath)
                   ?? throw new InvalidDataException($"DXF file could not be read: {filePath}");
        }
        catch (Exception ex) when (ex is not InvalidDataException and not FileNotFoundException)
        {
            throw new InvalidDataException($"DXF file could not be read or has an invalid format: {filePath}", ex);
        }
    }

    /// <summary>Checks that the path is non-empty and points to an existing file.</summary>
    private static void ValidatePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("DXF file path cannot be empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("DXF file was not found.", filePath);
    }
}
