using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Walls;

namespace DxfToRevit.Core.Dxf;

/// <summary>
/// The layer naming rules of a drawing: which layers hold wall faces and which hold columns.
/// Passed to the analysis (<see cref="DxfCenterLinePipeline"/>), so another office standard is a
/// different set of rules, not a code change. Doors, windows and openings are recognised by block
/// name through their own data-driven registry (<c>OpeningDefinitionRegistry</c>).
/// </summary>
/// <param name="Walls">Wall layer rule.</param>
/// <param name="Columns">Column layer rule.</param>
public sealed record LayerRules(WallLayerRule Walls, ColumnLayerRule Columns)
{
    /// <summary>The default standard: <c>WALL-&lt;cm&gt;</c> walls, <c>COLUMN</c>, <c>COLUMNS</c> and <c>COLUMN-*</c> columns.</summary>
    public static LayerRules Default { get; } = new(new WallLayerRule(), new ColumnLayerRule());
}
