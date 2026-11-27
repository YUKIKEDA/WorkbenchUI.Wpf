using System.Numerics;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Viewport3D;

namespace WorkbenchUI.Wpf.Viewport3D.Tests;

public class ViewportGlyphTests
{
    // ================= Arrow Prototype =================

    [Fact]
    public void BuildArrowGeometry_ProducesValidUnitArrow()
    {
        var (vertices, indices) = ViewportGlyph.BuildArrowGeometry();

        Assert.True(vertices.Length > 0);
        Assert.Equal(0, vertices.Length % 6); // position3 + normal3
        Assert.Equal(0, indices.Length % 3);  // Triangle list

        var vertexCount = vertices.Length / 6;
        foreach (var index in indices)
        {
            Assert.InRange(index, 0, vertexCount - 1);
        }

        // Unit arrow: z in [0, 1], radius <= head radius
        for (var v = 0; v < vertexCount; v++)
        {
            var x = vertices[v * 6];
            var y = vertices[v * 6 + 1];
            var z = vertices[v * 6 + 2];
            Assert.InRange(z, 0.0f, 1.0f);
            Assert.True(Math.Sqrt(x * x + y * y) <= 0.2, "Radius exceeds proportion limit");

            // Normals are unit vectors
            var nx = vertices[v * 6 + 3];
            var ny = vertices[v * 6 + 4];
            var nz = vertices[v * 6 + 5];
            Assert.Equal(1.0, Math.Sqrt(nx * nx + ny * ny + nz * nz), 3);
        }

        // Vertices exist at both tip (z=1) and base (z=0)
        var zs = Enumerable.Range(0, vertexCount).Select(v => vertices[v * 6 + 2]).ToArray();
        Assert.Contains(zs, z => z == 1.0f);
        Assert.Contains(zs, z => z == 0.0f);
    }

    [Fact]
    public void BuildArrowGeometry_ClampsSegments()
    {
        var (vertices, indices) = ViewportGlyph.BuildArrowGeometry(1); // Clamped up to 3
        Assert.True(vertices.Length >= 3 * 6);
        Assert.True(indices.Length >= 3 * 3);
    }

    // ================= Rotation Basis (Matches HLSL) =================

    [Theory]
    [InlineData(1.0f, 0.0f, 0.0f)]
    [InlineData(0.0f, 1.0f, 0.0f)]
    [InlineData(0.0f, 0.0f, 1.0f)]
    [InlineData(0.0f, 0.0f, -1.0f)]
    [InlineData(0.577350f, 0.577350f, 0.577350f)]
    [InlineData(-0.267261f, 0.534522f, -0.801784f)]
    public void ComputeBasis_ReturnsOrthonormalBasis(float wx, float wy, float wz)
    {
        var w = Vector3.Normalize(new Vector3(wx, wy, wz));
        var (u, v) = ViewportGlyph.ComputeBasis(w);

        Assert.Equal(1.0f, u.Length(), 4);
        Assert.Equal(1.0f, v.Length(), 4);
        Assert.Equal(0.0f, Vector3.Dot(u, w), 4);
        Assert.Equal(0.0f, Vector3.Dot(v, w), 4);
        Assert.Equal(0.0f, Vector3.Dot(u, v), 4);

        // Right-handed: u x v = w (maps prototype +Z onto vector direction)
        var cross = Vector3.Cross(u, v);
        Assert.Equal(w.X, cross.X, 4);
        Assert.Equal(w.Y, cross.Y, 4);
        Assert.Equal(w.Z, cross.Z, 4);
    }

    // ================= Instance Building =================

    private static ViewportMesh CreateMeshWithVectors() => new()
    {
        Positions =
        [
            0.0, 0.0, 0.0,
            1.0, 0.0, 0.0,
            2.0, 0.0, 0.0,
            3.0, 0.0, 0.0,
        ],
        TriangleIndices = [0, 1, 2],
        VectorValues =
        [
            3.0, 0.0, 4.0,   // |v| = 5
            0.0, 0.0, 0.0,   // zero -> skipped
            0.0, 2.0, 0.0,   // |v| = 2
            double.NaN, 0.0, 0.0, // NaN -> skipped
        ],
    };

    [Fact]
    public void BuildInstances_SkipsZeroAndNaNVectors()
    {
        var mesh = CreateMeshWithVectors();

        var data = ViewportGlyph.BuildInstances(
            mesh, 1, 0.0, 0.0, 0.0, null, new Vector4(1.0f, 0.5f, 0.0f, 1.0f), out var count);

        Assert.Equal(2, count);
        Assert.Equal(2 * ViewportGlyph.FloatsPerInstance, data.Length);

        // 1st glyph: node 0, |v|=5, direction (0.6, 0, 0.8)
        Assert.Equal(0.0f, data[0]); // base x
        Assert.Equal(0.6f, data[6], 4); // direction x
        Assert.Equal(0.0f, data[7], 4);
        Assert.Equal(0.8f, data[8], 4);
        Assert.Equal(5.0f, data[9], 4); // |v|

        // Fallback color (no ColorScale)
        Assert.Equal(1.0f, data[10], 4);
        Assert.Equal(0.5f, data[11], 4);

        // 2nd glyph: node 2, |v|=2, direction (0, 1, 0)
        var o = ViewportGlyph.FloatsPerInstance;
        Assert.Equal(2.0f, data[o], 4); // base x
        Assert.Equal(1.0f, data[o + 7], 4); // direction y
        Assert.Equal(2.0f, data[o + 9], 4); // |v|
    }

    [Fact]
    public void BuildInstances_AppliesStride()
    {
        // All 8 nodes have the same non-zero vector -> stride=3 yields 3 glyphs at nodes 0, 3, 6
        var positions = new double[8 * 3];
        var vectors = new double[8 * 3];
        for (var node = 0; node < 8; node++)
        {
            positions[node * 3] = node;
            vectors[node * 3 + 2] = 1.0;
        }

        var mesh = new ViewportMesh
        {
            Positions = positions,
            TriangleIndices = [0, 1, 2],
            VectorValues = vectors,
        };

        var data = ViewportGlyph.BuildInstances(
            mesh, 3, 0.0, 0.0, 0.0, null, Vector4.One, out var count);

        Assert.Equal(3, count);
        Assert.Equal(0.0f, data[0]); // node 0
        Assert.Equal(3.0f, data[ViewportGlyph.FloatsPerInstance], 4); // node 3
        Assert.Equal(6.0f, data[ViewportGlyph.FloatsPerInstance * 2], 4); // node 6
    }

    [Fact]
    public void BuildInstances_RecentersByOrigin()
    {
        var mesh = CreateMeshWithVectors();

        var data = ViewportGlyph.BuildInstances(
            mesh, 1, 10.0, 20.0, 30.0, null, Vector4.One, out var count);

        Assert.Equal(2, count);
        Assert.Equal(-10.0f, data[0], 4);
        Assert.Equal(-20.0f, data[1], 4);
        Assert.Equal(-30.0f, data[2], 4);
    }

    [Fact]
    public void BuildInstances_IncludesDisplacements()
    {
        var mesh = CreateMeshWithVectors();
        mesh.Displacements =
        [
            0.1, 0.2, 0.3,
            0.0, 0.0, 0.0,
            0.4, 0.5, 0.6,
            0.0, 0.0, 0.0,
        ];

        var data = ViewportGlyph.BuildInstances(
            mesh, 1, 0.0, 0.0, 0.0, null, Vector4.One, out _);

        Assert.Equal(0.1f, data[3], 4); // node 0 displacement
        Assert.Equal(0.2f, data[4], 4);
        Assert.Equal(0.3f, data[5], 4);
        var o = ViewportGlyph.FloatsPerInstance;
        Assert.Equal(0.4f, data[o + 3], 4); // node 2 displacement
    }

    [Fact]
    public void BuildInstances_WithColorScale_MapsMagnitudeToColor()
    {
        var mesh = CreateMeshWithVectors();
        var scale = new ColorScale { ColorMap = ColorMap.Grayscale, Minimum = 0.0, Maximum = 5.0 };

        var data = ViewportGlyph.BuildInstances(
            mesh, 1, 0.0, 0.0, 0.0, scale, Vector4.Zero, out var count);

        Assert.Equal(2, count);

        // 1st glyph |v|=5 = max -> near-white, 2nd |v|=2 (u=0.4) -> mid-gray; verify monotonicity
        var brightness1 = data[10] + data[11] + data[12];
        var o = ViewportGlyph.FloatsPerInstance;
        var brightness2 = data[o + 10] + data[o + 11] + data[o + 12];
        Assert.True(brightness1 > brightness2, $"Larger |v| should be brighter: {brightness1} vs {brightness2}");
        Assert.Equal(1.0f, data[13], 4); // alpha is opaque
    }

    [Fact]
    public void BuildInstances_NoVectors_ReturnsEmpty()
    {
        var mesh = new ViewportMesh
        {
            Positions = [0.0, 0.0, 0.0],
            TriangleIndices = [0, 0, 0],
        };

        var data = ViewportGlyph.BuildInstances(
            mesh, 1, 0.0, 0.0, 0.0, null, Vector4.One, out var count);

        Assert.Equal(0, count);
        Assert.Empty(data);
    }

    [Fact]
    public void BuildInstances_VectorArrayShorterThanNodes_UsesCommonLength()
    {
        // 3 positions but vectors only for 2 nodes -> capped at 2 glyphs
        var mesh = new ViewportMesh
        {
            Positions = [0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 2.0, 0.0, 0.0],
            TriangleIndices = [0, 1, 2],
            VectorValues = [0.0, 0.0, 1.0, 0.0, 0.0, 2.0],
        };

        var data = ViewportGlyph.BuildInstances(
            mesh, 1, 0.0, 0.0, 0.0, null, Vector4.One, out var count);

        Assert.Equal(2, count);
        Assert.Equal(2 * ViewportGlyph.FloatsPerInstance, data.Length);
    }

    // ================= Suggested Glyph Scale (Same Basis as GetSuggestedDeformationScale) =================

    [Fact]
    public void SuggestedScale_MakesMaxVectorFractionOfModel()
    {
        // Max |v| = 5, characteristic size 200 -> 5% target gives 200 * 0.05 / 5 = 2
        var max = ViewportDeformation.GetMaxDisplacementMagnitude([3.0, 0.0, 4.0, 0.0, 1.0, 0.0]);
        Assert.Equal(5.0, max, 6);
        Assert.Equal(2.0, ViewportDeformation.ComputeSuggestedScale(max, 200.0), 6);
    }
}
