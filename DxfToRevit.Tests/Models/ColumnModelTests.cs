using DxfToRevit.Core.Models;
using netDxf;
using Xunit;

namespace DxfToRevit.Tests.Models;

/// <summary>
/// ColumnModel centroid: area-weighted (shoelace), not the vertex average.
/// </summary>
public sealed class ColumnModelTests
{
    /// <summary>Birim kare → (0.5, 0.5).</summary>
    [Fact]
    public void Constructor_SquareOutline_CentroidIsCenter()
    {
        var outline = new[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        var col = new ColumnModel("C001", "COLUMN", "H1", outline);
        Assert.Equal(0.5, col.Centroid.X, 6);
        Assert.Equal(0.5, col.Centroid.Y, 6);
    }

    /// <summary>
    /// L-shaped polygon: vertex average ≠ area centroid.
    /// Vertices: (0,0),(4,0),(4,1),(1,1),(1,3),(0,3)
    /// Area = 4*1 + 1*2 = 6; C_x = 1.5, C_y = 1.0 (union of two rectangles).
    /// </summary>
    [Fact]
    public void Constructor_LShape_CentroidIsAreaWeightedNotVertexAverage()
    {
        var outline = new[]
        {
            new Vector2(0, 0),
            new Vector2(4, 0),
            new Vector2(4, 1),
            new Vector2(1, 1),
            new Vector2(1, 3),
            new Vector2(0, 3)
        };

        // Vertex average
        double vx = outline.Average(p => p.X);
        double vy = outline.Average(p => p.Y);

        var col = new ColumnModel("C-L", "COLUMN", "H2", outline);

        // Area centroid should differ from vertex mean for this L
        Assert.NotEqual(vx, col.Centroid.X, 3);
        Assert.NotEqual(vy, col.Centroid.Y, 3);

        // Expected area centroid (shoelace): Cx=1.5, Cy=7/6≈1.166667
        Assert.Equal(1.5, col.Centroid.X, 6);
        Assert.Equal(1.0, col.Centroid.Y, 6);
    }
}
