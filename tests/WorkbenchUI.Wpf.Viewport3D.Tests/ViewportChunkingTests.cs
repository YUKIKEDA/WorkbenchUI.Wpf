using WorkbenchUI.Wpf.Viewport3D;

namespace WorkbenchUI.Wpf.Viewport3D.Tests;

/// <summary>
/// Tests chunk-splitting math and sequential equivalence of parallel geometry construction.
/// </summary>
public class ViewportChunkingTests
{
    /// <summary>Creates an n x n grid mesh (2(n-1)^2 triangles).</summary>
    private static (double[] Positions, int[] Triangles) CreateGrid(int n)
    {
        var positions = new double[n * n * 3];
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                var v = i * n + j;
                positions[v * 3] = i;
                positions[v * 3 + 1] = j;
                positions[v * 3 + 2] = Math.Sin(i * 0.3) * Math.Cos(j * 0.2);
            }
        }

        var side = n - 1;
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

        return (positions, triangles);
    }

    // ================= ComputeChunkBoundaries =================

    [Fact]
    public void Boundaries_SmallMesh_SingleChunk()
    {
        var (_, triangles) = CreateGrid(10);
        var boundaries = ViewportChunking.ComputeChunkBoundaries(triangles, 100);

        var boundary = Assert.Single(boundaries);
        Assert.Equal(0, boundary.TriangleStart);
        Assert.Equal(triangles.Length / 3, boundary.TriangleCount);
        Assert.Equal(100, boundary.VertexCount); // All grid vertices are referenced
    }

    [Fact]
    public void Boundaries_EmptyTriangles_ReturnsEmpty()
    {
        Assert.Empty(ViewportChunking.ComputeChunkBoundaries([], 10));
        Assert.Empty(ViewportChunking.ComputeChunkBoundaries([0, 1, 2], 0));
    }

    [Fact]
    public void Boundaries_TriangleLimit_SplitsContiguously()
    {
        var (_, triangles) = CreateGrid(10); // 162 triangles
        var boundaries = ViewportChunking.ComputeChunkBoundaries(
            triangles, 100, maxTrianglesPerChunk: 50);

        Assert.Equal(4, boundaries.Count); // 50+50+50+12

        // Contiguous partition: covers all triangles without gaps or overlaps
        var expectedStart = 0;
        foreach (var b in boundaries)
        {
            Assert.Equal(expectedStart, b.TriangleStart);
            Assert.True(b.TriangleCount is > 0 and <= 50);
            expectedStart += b.TriangleCount;
        }

        Assert.Equal(triangles.Length / 3, expectedStart);
    }

    [Fact]
    public void Boundaries_VertexLimit_NeverExceeded()
    {
        var (_, triangles) = CreateGrid(20);
        const int maxVertices = 64;
        var boundaries = ViewportChunking.ComputeChunkBoundaries(
            triangles, 400, maxVerticesPerChunk: maxVertices);

        Assert.True(boundaries.Count > 1);
        foreach (var b in boundaries)
        {
            Assert.True(b.VertexCount <= maxVertices);
        }
    }

    [Fact]
    public void Boundaries_TinyLimits_AreClampedToOneTriangle()
    {
        // Always advances by at least 1 triangle even if limit is below 1 triangle (no infinite loop)
        int[] triangles = [0, 1, 2, 2, 1, 3];
        var boundaries = ViewportChunking.ComputeChunkBoundaries(
            triangles, 4, maxVerticesPerChunk: 1, maxTrianglesPerChunk: 1);

        Assert.Equal(2, boundaries.Count);
        Assert.All(boundaries, b => Assert.Equal(1, b.TriangleCount));
    }

    [Fact]
    public void Boundaries_DegenerateTriangle_CountsUniqueVerticesOnly()
    {
        // A degenerate triangle referencing vertex 0 three times counts as 1 new vertex
        int[] triangles = [0, 0, 0];
        var boundary = Assert.Single(ViewportChunking.ComputeChunkBoundaries(triangles, 1));
        Assert.Equal(1, boundary.VertexCount);
    }

    // ================= BuildChunkData =================

    [Fact]
    public void BuildChunkData_RemapsBackToOriginalTriangles()
    {
        var (_, triangles) = CreateGrid(20);
        var vertexCount = 400;
        var boundaries = ViewportChunking.ComputeChunkBoundaries(
            triangles, vertexCount, maxVerticesPerChunk: 100, maxTrianglesPerChunk: 120);
        Assert.True(boundaries.Count > 1);

        var remap = new ChunkVertexRemap(vertexCount);
        foreach (var boundary in boundaries)
        {
            var (localTriangles, globalVertices) = ViewportChunking.BuildChunkData(triangles, boundary, remap);

            Assert.Equal(boundary.TriangleCount * 3, localTriangles.Length);
            Assert.Equal(boundary.VertexCount, globalVertices.Length);

            // Inverse local-to-global mapping matches the original triangle list exactly
            for (var i = 0; i < localTriangles.Length; i++)
            {
                var local = localTriangles[i];
                Assert.InRange(local, 0, globalVertices.Length - 1);
                Assert.Equal(triangles[boundary.TriangleStart * 3 + i], globalVertices[local]);
            }

            // Global map has no duplicates (unique vertices)
            Assert.Equal(globalVertices.Length, globalVertices.Distinct().Count());
        }
    }

    [Fact]
    public void BuildChunkData_SharedRemap_IsIsolatedBetweenChunks()
    {
        // Reusing the same remap instance does not leak prior chunk assignments (epoch scheme)
        int[] triangles = [0, 1, 2, 3, 4, 5];
        var remap = new ChunkVertexRemap(6);

        var (local1, global1) = ViewportChunking.BuildChunkData(
            triangles, new ChunkBoundary(0, 1, 3), remap);
        var (local2, global2) = ViewportChunking.BuildChunkData(
            triangles, new ChunkBoundary(1, 1, 3), remap);

        Assert.Equal([0, 1, 2], local1);
        Assert.Equal([0, 1, 2], global1);
        Assert.Equal([0, 1, 2], local2);
        Assert.Equal([3, 4, 5], global2);
    }

    // ================= Pick ID Offset =================

    [Fact]
    public void PickIdOffset_LocalPrimitiveIdPlusBase_RecoversGlobalTriangle()
    {
        var (_, triangles) = CreateGrid(15);
        var triangleCount = triangles.Length / 3;
        var boundaries = ViewportChunking.ComputeChunkBoundaries(
            triangles, 225, maxTrianglesPerChunk: 60);
        Assert.True(boundaries.Count > 1);

        // Matches shader primId + PickParams.x calculation to recover every global triangle uniquely
        var seen = new HashSet<int>();
        foreach (var b in boundaries)
        {
            for (var localPrim = 0; localPrim < b.TriangleCount; localPrim++)
            {
                var globalTriangle = b.TriangleStart + localPrim;
                Assert.InRange(globalTriangle, 0, triangleCount - 1);
                Assert.True(seen.Add(globalTriangle));
            }
        }

        Assert.Equal(triangleCount, seen.Count);
    }

    // ================= Parallel Geometry Build Equivalence =================

    [Fact]
    public void ToLocalPositions_LargeInput_MatchesSequentialReference()
    {
        // Above the parallel threshold (65,536 vertices), matches sequential reference identically
        var (positions, _) = CreateGrid(300); // 90,000 vertices
        const double ox = 12.5, oy = -3.25, oz = 100.0;

        var actual = ViewportGeometry.ToLocalPositions(positions, ox, oy, oz);

        var expected = new float[positions.Length];
        for (var i = 0; i + 2 < positions.Length; i += 3)
        {
            expected[i] = (float)(positions[i] - ox);
            expected[i + 1] = (float)(positions[i + 1] - oy);
            expected[i + 2] = (float)(positions[i + 2] - oz);
        }

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComputeVertexNormals_LargeInput_MatchesSequentialReference()
    {
        var (positions, triangles) = CreateGrid(300);
        var local = ViewportGeometry.ToLocalPositions(positions, 0.0, 0.0, 0.0);

        var actual = ViewportGeometry.ComputeVertexNormals(local, triangles);
        var expected = SequentialNormalsReference(local, triangles);

        Assert.Equal(expected, actual);
    }

    /// <summary>Sequential reference matching pre-parallel implementation (sequential accumulation and normalization).</summary>
    private static float[] SequentialNormalsReference(float[] localPositions, int[] triangleIndices)
    {
        var normals = new float[localPositions.Length];
        for (var t = 0; t + 2 < triangleIndices.Length; t += 3)
        {
            var i0 = triangleIndices[t] * 3;
            var i1 = triangleIndices[t + 1] * 3;
            var i2 = triangleIndices[t + 2] * 3;

            var e1X = localPositions[i1] - localPositions[i0];
            var e1Y = localPositions[i1 + 1] - localPositions[i0 + 1];
            var e1Z = localPositions[i1 + 2] - localPositions[i0 + 2];
            var e2X = localPositions[i2] - localPositions[i0];
            var e2Y = localPositions[i2 + 1] - localPositions[i0 + 1];
            var e2Z = localPositions[i2 + 2] - localPositions[i0 + 2];

            var nx = e1Y * e2Z - e1Z * e2Y;
            var ny = e1Z * e2X - e1X * e2Z;
            var nz = e1X * e2Y - e1Y * e2X;

            normals[i0] += nx;
            normals[i0 + 1] += ny;
            normals[i0 + 2] += nz;
            normals[i1] += nx;
            normals[i1 + 1] += ny;
            normals[i1 + 2] += nz;
            normals[i2] += nx;
            normals[i2 + 1] += ny;
            normals[i2 + 2] += nz;
        }

        for (var i = 0; i + 2 < normals.Length; i += 3)
        {
            var len = MathF.Sqrt(
                normals[i] * normals[i] +
                normals[i + 1] * normals[i + 1] +
                normals[i + 2] * normals[i + 2]);
            if (len > 1e-20f)
            {
                normals[i] /= len;
                normals[i + 1] /= len;
                normals[i + 2] /= len;
            }
        }

        return normals;
    }

    // ================= ViewportStatistics =================

    [Fact]
    public void ViewportStatistics_RecordEquality()
    {
        var a = new ViewportStatistics(
            50_000_000, 25_010_001, 1, 13, 1,
            2_500_000, true, 7,
            TimeSpan.FromSeconds(3.2), TimeSpan.FromMilliseconds(33.0));
        var b = a with { };

        Assert.Equal(a, b);
        Assert.Equal(50_000_000, a.TriangleCount);
        Assert.Equal(13, a.ChunkCount);
        Assert.Equal(1, a.EdgeSkippedMeshCount);
        Assert.Equal(2_500_000, a.LodTriangleCount);
        Assert.True(a.IsLodActive);
        Assert.Equal(7, a.LastDrawnChunkCount);
    }

    [Fact]
    public void InteractiveLodThreshold_Behavior()
    {
        // Matches GpuMesh.Create boundary condition (triangleCount > threshold builds LOD)
        static bool ShouldBuildLod(int triangleCount, int threshold) => triangleCount > threshold;

        Assert.False(ShouldBuildLod(5_000_000, 5_000_000)); // Exactly 5M -> no LOD
        Assert.True(ShouldBuildLod(5_000_001, 5_000_000));  // Exceeds -> build LOD
        Assert.False(ShouldBuildLod(int.MaxValue, int.MaxValue)); // Effectively disabled at int.MaxValue
    }

    [Fact]
    public void EdgeExtractionSkip_ThresholdBehavior()
    {
        // Matches GpuMesh.Create boundary condition (triangleCount > limit skips)
        static bool ShouldSkip(int triangleCount, int limit) => triangleCount > limit;

        Assert.False(ShouldSkip(5_000_000, 5_000_000)); // Exactly 5M -> extracted
        Assert.True(ShouldSkip(5_000_001, 5_000_000));  // Exceeds -> skipped
        Assert.False(ShouldSkip(int.MaxValue, int.MaxValue)); // Effectively unlimited
    }
}
