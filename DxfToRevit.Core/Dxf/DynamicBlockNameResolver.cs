using netDxf;
using netDxf.Blocks;

namespace DxfToRevit.Core.Dxf;

/// <summary>
/// Resolves the true name of dynamic block references. When a dynamic block is stretched, AutoCAD
/// stores the reference as an anonymous block (<c>*U26</c>); its block record carries an
/// <c>AcDbBlockRepBTag</c> extended-data entry with the handle of the original block record, whose
/// name (e.g. <c>door</c>) is the true name.
/// </summary>
public sealed class DynamicBlockNameResolver
{
    /// <summary>Application id of the extended data that links an anonymous block to its dynamic block.</summary>
    public const string BlockRepresentationAppId = "AcDbBlockRepBTag";

    private readonly Dictionary<string, string> _namesByRecordHandle;

    /// <summary>Indexes the block records of a document.</summary>
    /// <param name="document">Loaded DXF document.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public DynamicBlockNameResolver(DxfDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _namesByRecordHandle = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Block block in document.Blocks)
        {
            if (block.Record?.Handle is string handle && !string.IsNullOrEmpty(handle))
                _namesByRecordHandle[handle] = block.Name;
        }
    }

    /// <summary>
    /// Returns the dynamic block name of an anonymous block, or the block's own name for ordinary
    /// blocks (and when the link cannot be resolved).
    /// </summary>
    /// <param name="block">Referenced block.</param>
    /// <exception cref="ArgumentNullException"><paramref name="block"/> is <c>null</c>.</exception>
    public string Resolve(Block block)
    {
        ArgumentNullException.ThrowIfNull(block);

        if (block.Record is null ||
            !block.Record.XData.TryGetValue(BlockRepresentationAppId, out XData? data) ||
            data is null)
        {
            return block.Name;
        }

        foreach (XDataRecord record in data.XDataRecord)
        {
            if (record.Code == XDataCode.DatabaseHandle &&
                record.Value is string handle &&
                _namesByRecordHandle.TryGetValue(handle, out string? name))
            {
                return name;
            }
        }

        return block.Name;
    }
}
