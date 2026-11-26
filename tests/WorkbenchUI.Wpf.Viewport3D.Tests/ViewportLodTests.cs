using System.Numerics;
using WorkbenchUI.Wpf.Viewport3D;

namespace WorkbenchUI.Wpf.Viewport3D.Tests;

/// <summary>Pure-function tests for octahedral normal compression.</summary>
public class OctahedralNormalTests
{
    [Theory]
    [InlineData(1f, 0f, 0f)]
    [InlineData(-1f, 0f, 0f)]
    [InlineData(0f, 1f, 0f)]
    [InlineData(0f, -1f, 0f)]
    [InlineData(0f, 0f, 1f)]
    [InlineData(0f, 0f, -1f)]
    public void EncodeDecode_Axes_Roundtrip(float x, float y, float z)
    {
        var (dx, dy, dz) = ViewportGeometry.DecodeOctahedralNormal(
            ViewportGeometry.EncodeOctahedralNormal(x, y, z));

        Assert.Equal(x, dx, 3);
        Assert.Equal(y, dy, 3);
        Assert.Equal(z, dz, 3);
    }

    [Fact]
    public void EncodeDecode_RandomUnitVectors_SmallAngleError()
    {
        // Max angular error of 16-bit x 2 quantization is ~0.02 deg (~1e-7 cosine error); verify with 0.1 deg tolerance
        var random = new Random(12345);
        for (var i = 0; i < 1000; i++)
        {
            var v = RandomUnit(random);
            var (dx, dy, dz) = ViewportGeometry.DecodeOctahedralNormal(
                ViewportGeometry.EncodeOctahedralNormal(v.X, v.Y, v.Z));

            var dot = Math.Clamp(v.X * dx + v.Y * dy + v.Z * dz, -1.0f, 1.0f);
            var angleDeg = Math.Acos(dot) * 180.0 / Math.PI;
            Assert.True(angleDeg < 0.1, $"Angular error {angleDeg:F4} deg (normal {v})");
        }
    }

    [Fact]
    public void Encode_ZeroNormal_DecodesToUnitZ()
    {
        // Zero normal (unreferenced vertex) maps to +Z without producing NaN
        var (x, y, z) = ViewportGeometry.DecodeOctahedralNormal(
            ViewportGeometry.EncodeOctahedralNormal(0f, 0f, 0f));

        Assert.Equal(0f, x, 3);
        Assert.Equal(0f, y, 3);
        Assert.Equal(1f, z, 3);
    }

    [Fact]
    public void CompressNormals_MatchesElementwiseEncode()
    {
        float[] normals = [1f, 0f, 0f, 0f, 0.6f, 0.8f, -0.577f, -0.577f, -0.577f];
        var packed = ViewportGeometry.CompressNormals(normals);

        Assert.Equal(3, packed.Length);
        for (var v = 0; v < 3; v++)
        {
            Assert.Equal(
                ViewportGeometry.EncodeOctahedralNormal(normals[v * 3], normals[v * 3 + 1], normals[v * 3 + 2]),
                packed[v]);
        }
    }

    [Fact]
    public void ComputeVertexNormalsFromDouble_MatchesFloatPath()
    {
        // Streaming version (direct double read) matches float conversion -> ComputeVertexNormals exactly
        var random = new Random(7);
        var positions = new double[30 * 3];
        for (var i = 0; i < positions.Length; i++)
        {
            positions[i] = 1_000_000.0 + random.NextDouble() * 50.0;
        }

        var triangles = new int[20 * 3];
        for (var i = 0; i < triangles.Length; i++)
        {
            triangles[i] = random.Next(30);
        }

        const double ox = 1_000_025.0, oy = 1_000_012.0, oz = 1_000_037.0;
        var local = ViewportGeometry.ToLocalPositions(positions, ox, oy, oz);
        var expected = ViewportGeometry.ComputeVertexNormals(local, triangles);
        var actual = ViewportGeometry.ComputeVertexNormalsFromDouble(positions, ox, oy, oz, triangles);

        Assert.Equal(expected, actual);
    }

    private static Vector3 RandomUnit(Random random)
    {
        while (true)
        {
            var v = new Vector3(
                (float)(random.NextDouble() * 2.0 - 1.0),
                (float)(random.NextDouble() * 2.0 - 1.0),
                (float)(random.NextDouble() * 2.0 - 1.0));
            var len = v.Length();
            if (len is > 0.01f and < 1.0f)
            {
                return v / len;
            }
        }
    }
}

/// <summary>Pure-function tests for grid-clustering LOD.</summary>
public class ViewportLodTests
{
    [Fact]
    public void ComputeGridResolution_FollowsSquareRootLaw()
    {
        // For surface meshes, occupied cell count scales with resolution^2, so baseline is sqrt(V/20)
        Assert.Equal(2, ViewportLod.ComputeGridResolution(1));
        Assert.Equal(23, ViewportLod.ComputeGridResolution(10_000)); // √500 = 22.36 → 23
        Assert.True(ViewportLod.ComputeGridResolution(100_000_000) <= ViewportLod.MaxGridResolution);

        // Monotonically non-decreasing with vertex count
        var prev = 0;
        foreach (var v in (int[])[100, 10_000, 1_000_000, 100_000_000])
        {
            var r = ViewportLod.ComputeGridResolution(v);
            Assert.True(r >= prev);
            prev = r;
        }
    }

    [Fact]
    public void ComputeCellKey_DistinctCells_DistinctKeys()
    {
        var bounds = new Bounds3D(0, 0, 0, 10, 10, 10);
        var k000 = ViewportLod.ComputeCellKey(0.5, 0.5, 0.5, bounds, 10);
        var k100 = ViewportLod.ComputeCellKey(1.5, 0.5, 0.5, bounds, 10);
        var k010 = ViewportLod.ComputeCellKey(0.5, 1.5, 0.5, bounds, 10);
        var k001 = ViewportLod.ComputeCellKey(0.5, 0.5, 1.5, bounds, 10);

        Assert.NotEqual(k000, k100);
        Assert.NotEqual(k000, k010);
        Assert.NotEqual(k000, k001);
        Assert.NotEqual(k100, k010);

        // Points in the same cell share a key; upper boundary values clamp to max cell
        Assert.Equal(k000, ViewportLod.ComputeCellKey(0.9, 0.9, 0.9, bounds, 10));
        Assert.Equal(
            ViewportLod.ComputeCellKey(10.0, 10.0, 10.0, bounds, 10),
            ViewportLod.ComputeCellKey(9.95, 9.95, 9.95, bounds, 10));
    }

    [Fact]
    public void Build_ClustersVerticesAndDropsDegenerateTriangles()
    {
        // 4 vertices split into 2 cells: (0,1) in left cell, (2,3) in right cell
        // Triangle 0-1-2 maps to (left, left, right) -> collapses to 2 vertices and becomes degenerate.
        // Triangles 0-2-3 and 1-2-3 both map to (left, right, right) -> degenerate and duplicated
        double[] positions =
        [
            0.0, 0.0, 0.0,   // v0: left cell (representative = lowest index)
            0.1, 0.1, 0.0,   // v1: left cell
            9.0, 0.0, 0.0,   // v2: right cell
            9.1, 0.1, 0.0,   // v3: right cell
        ];
        int[] triangles =
        [
            0, 1, 2, // 2 vertices in left cell -> degenerate, removed
            0, 2, 3, // left-right-right -> 2 vertices in right cell, degenerate, removed
            1, 2, 3, // same as above (also duplicate)
        ];
        var bounds = new Bounds3D(0, 0, 0, 10, 10, 10);

        // Resolution 2 -> cell width 5: v0/v1 share a cell, v2/v3 share a cell
        var lod = ViewportLod.Build(positions, triangles, null, bounds, reductionFactor: 2);

        // All triangles degenerate -> returns null
        Assert.Null(lod);
    }

    [Fact]
    public void Build_RepresentativeIsLowestIndex_AndScalarIsAveraged()
    {
        // Triangle mesh with 1-2 vertices placed in each of 3 separated cells
        double[] positions =
        [
            0.0, 0.0, 0.0,   // v0: cell A
            0.2, 0.0, 0.0,   // v1: cell A (same cell as v0)
            9.0, 0.0, 0.0,   // v2: cell B
            0.0, 9.0, 0.0,   // v3: cell C
        ];
        int[] triangles = [0, 2, 3, 1, 2, 3];
        double[] scalars = [1.0, 3.0, 5.0, 7.0];
        var bounds = new Bounds3D(0, 0, 0, 10, 10, 10);

        var lod = ViewportLod.Build(positions, triangles, scalars, bounds, reductionFactor: 8);

        Assert.NotNull(lod);
        Assert.Equal(3, lod.VertexCount);

        // Representative vertex = lowest index in cell (v0/v1 -> v0), numbered in discovery order
        Assert.Equal([0, 2, 3], lod.RepresentativeNodes);

        // Scalar is averaged per cell: cell A = (1+3)/2 = 2
        Assert.NotNull(lod.ScalarValues);
        Assert.Equal(2.0, lod.ScalarValues[0], 12);
        Assert.Equal(5.0, lod.ScalarValues[1], 12);
        Assert.Equal(7.0, lod.ScalarValues[2], 12);

        // Both triangles map to the same cluster triangle -> deduplicated to 1 (preserving first winding)
        Assert.Equal([0, 1, 2], lod.TriangleIndices);
    }

    [Fact]
    public void Build_LargeMesh_ReducesTriangles_Deterministic()
    {
        // 100x100 grid (19,602 triangles) -> 1/20 target reduces to roughly 1/10 to 1/40
        const int side = 100;
        const int n = side + 1;
        var positions = new double[n * n * 3];
        var scalars = new double[n * n];
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                var v = i * n + j;
                positions[v * 3] = i;
                positions[v * 3 + 1] = j;
                positions[v * 3 + 2] = Math.Sin(i * 0.1) * 3.0;
                scalars[v] = i + j;
            }
        }

        var triangles = new int[side * side * 6];
        for (var i = 0; i < side; i++)
        {
            for (var j = 0; j < side; j++)
            {
                var i00 = i * n + j;
                var dst = (i * side + j) * 6;
                triangles[dst] = i00;
                triangles[dst + 1] = i00 + n;
                triangles[dst + 2] = i00 + n + 1;
                triangles[dst + 3] = i00;
                triangles[dst + 4] = i00 + n + 1;
                triangles[dst + 5] = i00 + 1;
            }
        }

        var bounds = ViewportGeometry.ComputeBounds(positions);
        var lod = ViewportLod.Build(positions, triangles, scalars, bounds);

        Assert.NotNull(lod);
        var sourceTriangles = triangles.Length / 3;
        Assert.InRange(lod.TriangleCount, sourceTriangles / 40, sourceTriangles / 10);
        Assert.InRange(lod.VertexCount, n * n / 40, n * n / 10);

        // Every LOD triangle references valid vertices with no degenerates or duplicates
        var seen = new HashSet<(int, int, int)>();
        for (var t = 0; t < lod.TriangleCount; t++)
        {
            int a = lod.TriangleIndices[t * 3], b = lod.TriangleIndices[t * 3 + 1], c = lod.TriangleIndices[t * 3 + 2];
            Assert.InRange(a, 0, lod.VertexCount - 1);
            Assert.InRange(b, 0, lod.VertexCount - 1);
            Assert.InRange(c, 0, lod.VertexCount - 1);
            Assert.True(a != b && b != c && a != c);

            Span<int> sorted = [a, b, c];
            sorted.Sort();
            Assert.True(seen.Add((sorted[0], sorted[1], sorted[2])), "Duplicate triangle remains");
        }

        // Representative vertices are discovered in ascending order (deterministic block + scan order)
        for (var i = 1; i < lod.RepresentativeNodes.Length; i++)
        {
            Assert.True(lod.RepresentativeNodes[i] > lod.RepresentativeNodes[i - 1]);
        }

        // Determinism: building twice yields identical output
        var lod2 = ViewportLod.Build(positions, triangles, scalars, bounds);
        Assert.NotNull(lod2);
        Assert.Equal(lod.RepresentativeNodes, lod2.RepresentativeNodes);
        Assert.Equal(lod.TriangleIndices, lod2.TriangleIndices);
        Assert.Equal(lod.ScalarValues, lod2.ScalarValues);
    }

    [Fact]
    public void Build_EmptyOrTiny_ReturnsNull()
    {
        var bounds = new Bounds3D(0, 0, 0, 1, 1, 1);
        Assert.Null(ViewportLod.Build([], [], null, bounds));
        Assert.Null(ViewportLod.Build([0.0, 0.0, 0.0], [], null, bounds));
        Assert.Null(ViewportLod.Build([0.0, 0.0, 0.0], [0, 0, 0], null, Bounds3D.Empty));
    }
}

/// <summary>Tests for frustum culling math.</summary>
public class ViewportCullingTests
{
    [Fact]
    public void ExtractFrustumPlanes_Orthographic_UnitCube()
    {
        // Orthographic projection mapping [-1,1]^3 to [-1,1]^2 x [0,1] (D3D convention: z' = z*0.5+0.5)
        var ortho = Matrix4x4.CreateOrthographic(2.0f, 2.0f, -1.0f, 1.0f);
        Span<Vector4> planes = stackalloc Vector4[ViewportCulling.PlaneCount];
        ViewportCulling.ExtractFrustumPlanes(in ortho, planes);

        // Origin is inside all 6 frustum planes
        for (var i = 0; i < ViewportCulling.PlaneCount; i++)
        {
            Assert.True(planes[i].W >= 0.0f, $"Plane {i} classified the origin as outside");
        }

        // Inner AABB is visible; AABB shifted outside along x is culled
        Assert.True(ViewportCulling.IntersectsFrustum(
            planes, new Vector3(-0.5f), new Vector3(0.5f)));
        Assert.False(ViewportCulling.IntersectsFrustum(
            planes, new Vector3(2.0f, -0.5f, -0.5f), new Vector3(3.0f, 0.5f, 0.5f)));
        Assert.False(ViewportCulling.IntersectsFrustum(
            planes, new Vector3(-0.5f, 2.0f, -0.5f), new Vector3(0.5f, 3.0f, 0.5f)));
    }

    [Fact]
    public void IntersectsFrustum_Perspective_BehindCameraIsCulled()
    {
        // View + perspective projection looking from (0,0,5) toward -Z (row-vector convention: view * proj)
        var view = Matrix4x4.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.UnitY);
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4.0f, 1.0f, 0.1f, 100.0f);
        var viewProj = view * proj;

        Span<Vector4> planes = stackalloc Vector4[ViewportCulling.PlaneCount];
        ViewportCulling.ExtractFrustumPlanes(in viewProj, planes);

        // Region near look-at target is visible
        Assert.True(ViewportCulling.IntersectsFrustum(
            planes, new Vector3(-0.5f), new Vector3(0.5f)));

        // Region behind camera is culled
        Assert.False(ViewportCulling.IntersectsFrustum(
            planes, new Vector3(-0.5f, -0.5f, 9.0f), new Vector3(0.5f, 0.5f, 10.0f)));

        // Region outside FOV (to the side) is culled
        Assert.False(ViewportCulling.IntersectsFrustum(
            planes, new Vector3(50.0f, -0.5f, -0.5f), new Vector3(51.0f, 0.5f, 0.5f)));

        // Region beyond far plane is culled
        Assert.False(ViewportCulling.IntersectsFrustum(
            planes, new Vector3(-0.5f, -0.5f, -200.0f), new Vector3(0.5f, 0.5f, -150.0f)));
    }

    [Fact]
    public void IntersectsFrustum_ExpandMakesBorderlineVisible()
    {
        var ortho = Matrix4x4.CreateOrthographic(2.0f, 2.0f, -1.0f, 1.0f);
        Span<Vector4> planes = stackalloc Vector4[ViewportCulling.PlaneCount];
        ViewportCulling.ExtractFrustumPlanes(in ortho, planes);

        // Slightly past right edge (x in [1.2, 2.0]) is culled at expand=0, visible at expand=0.5 (max displacement)
        var min = new Vector3(1.2f, -0.5f, -0.5f);
        var max = new Vector3(2.0f, 0.5f, 0.5f);
        Assert.False(ViewportCulling.IntersectsFrustum(planes, min, max));
        Assert.True(ViewportCulling.IntersectsFrustum(planes, min, max, expand: 0.5f));
    }

    [Fact]
    public void IsOutsidePlane_PositiveVertexTest()
    {
        // Plane x = 0 (normal +X): x < 0 side is outside
        var plane = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
        Assert.True(ViewportCulling.IsOutsidePlane(plane, new Vector3(-2f), new Vector3(-1f)));
        Assert.False(ViewportCulling.IsOutsidePlane(plane, new Vector3(-1f), new Vector3(1f))); // Straddles plane
        Assert.False(ViewportCulling.IsOutsidePlane(plane, new Vector3(1f), new Vector3(2f)));
    }
}
