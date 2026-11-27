namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Interactive level-of-detail (LOD) mesh data. Each LOD vertex corresponds to a representative node
/// (the lowest-indexed node in a grid cell) from the source mesh for positions and displacements,
/// while scalar values are averaged per cell to reduce contour flicker.
/// </summary>
internal sealed class LodMeshData
{
    /// <summary>Maps each LOD vertex index to its representative node index in the source mesh.</summary>
    public required int[] RepresentativeNodes { get; init; }

    /// <summary>LOD triangles referencing LOD vertices, with degenerate and duplicate triangles removed.</summary>
    public required int[] TriangleIndices { get; init; }

    /// <summary>Per-LOD-vertex cell-averaged scalar values, or null if the source mesh has no scalars.</summary>
    public required double[]? ScalarValues { get; init; }

    public int VertexCount => RepresentativeNodes.Length;

    public int TriangleCount => TriangleIndices.Length / 3;
}

/// <summary>
/// Grid-based vertex clustering LOD builder, independent of the GPU for unit testing.
/// <para>
/// Collapses vertices within each uniform grid cell to a single representative node (first seen in scan order)
/// and strips degenerate and duplicate triangles. Uses block-parallel computation with order-preserving merges
/// so results are deterministic, core-count independent, and identical to sequential execution.
/// </para>
/// </summary>
internal static class ViewportLod
{
    /// <summary>Target reduction factor (reduces vertex and triangle counts by roughly 1/20).</summary>
    public const int DefaultReductionFactor = 20;

    /// <summary>Maximum grid resolution per axis (must fit in 21 bits per axis when packed into a 64-bit key).</summary>
    public const int MaxGridResolution = 4096;

    /// <summary>Number of vertices or triangles per parallel block.</summary>
    private const int BlockSize = 1 << 20;

    /// <summary>
    /// Computes the uniform grid resolution per axis. Because occupied cells on a surface mesh scale as
    /// resolution^2, setting resolution = sqrt(vertexCount / reductionFactor) yields roughly vertexCount / reductionFactor LOD vertices.
    /// </summary>
    public static int ComputeGridResolution(int vertexCount, int reductionFactor = DefaultReductionFactor)
    {
        var target = Math.Sqrt(vertexCount / (double)Math.Max(reductionFactor, 1));
        return Math.Clamp((int)Math.Ceiling(target), 2, MaxGridResolution);
    }

    /// <summary>
    /// Encodes a 3D coordinate into a packed 21-bit-per-axis cell key.
    /// Cells are cubic (sized by the longest bounding-box axis divided by <paramref name="resolution"/>)
    /// to avoid over-subdividing thin dimensions such as shell thickness.
    /// </summary>
    public static long ComputeCellKey(
        double x, double y, double z, in Bounds3D bounds, int resolution)
    {
        var maxExtent = Math.Max(
            bounds.MaxX - bounds.MinX,
            Math.Max(bounds.MaxY - bounds.MinY, bounds.MaxZ - bounds.MinZ));
        if (maxExtent <= 0.0)
        {
            return 0;
        }

        var invCellSize = resolution / maxExtent;
        var ix = ToCellIndex(x, bounds.MinX, invCellSize, resolution);
        var iy = ToCellIndex(y, bounds.MinY, invCellSize, resolution);
        var iz = ToCellIndex(z, bounds.MinZ, invCellSize, resolution);
        return ((long)ix << 42) | ((long)iy << 21) | (uint)iz;

        static int ToCellIndex(double v, double min, double invCellSize, int resolution)
        {
            // Fold exact max-boundary coordinates into the last cell and clamp out-of-range values safely
            return Math.Clamp((int)((v - min) * invCellSize), 0, resolution - 1);
        }
    }

    /// <summary>
    /// Builds an LOD mesh. Returns null if no non-degenerate triangles remain (e.g., all vertices fall in one cell).
    /// </summary>
    public static LodMeshData? Build(
        double[] positions, int[] triangleIndices, double[]? scalarValues,
        in Bounds3D bounds, int reductionFactor = DefaultReductionFactor)
    {
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(triangleIndices);

        var vertexCount = positions.Length / 3;
        if (vertexCount == 0 || triangleIndices.Length < 3 || bounds.IsEmpty)
        {
            return null;
        }

        var resolution = ComputeGridResolution(vertexCount, reductionFactor);

        // --- Vertex clustering ---
        // Build per-block cell->local maps in parallel, then merge sequentially in block order
        // so resulting LOD vertex indices and representative nodes match sequential scanning exactly.
        var vertexBlocks = SplitBlocks(vertexCount);
        var vertexLocal = new int[vertexCount];
        var blockKeys = new List<long>[vertexBlocks.Length];
        var blockFirsts = new List<int>[vertexBlocks.Length];

        var boundsCopy = bounds; // Copy for lambda capture (in parameters cannot be captured)
        Parallel.For(0, vertexBlocks.Length, b =>
        {
            var (start, count) = vertexBlocks[b];
            var dict = new Dictionary<long, int>();
            var keys = new List<long>();
            var firsts = new List<int>();
            for (var i = start; i < start + count; i++)
            {
                var key = ComputeCellKey(
                    positions[i * 3], positions[i * 3 + 1], positions[i * 3 + 2], boundsCopy, resolution);
                if (!dict.TryGetValue(key, out var local))
                {
                    local = dict.Count;
                    dict[key] = local;
                    keys.Add(key);
                    firsts.Add(i);
                }

                vertexLocal[i] = local;
            }

            blockKeys[b] = keys;
            blockFirsts[b] = firsts;
        });

        var globalCells = new Dictionary<long, int>();
        var representatives = new List<int>();
        var blockRemaps = new int[vertexBlocks.Length][];
        for (var b = 0; b < vertexBlocks.Length; b++)
        {
            var keys = blockKeys[b];
            var firsts = blockFirsts[b];
            var remap = new int[keys.Count];
            for (var k = 0; k < keys.Count; k++)
            {
                if (!globalCells.TryGetValue(keys[k], out var lod))
                {
                    lod = representatives.Count;
                    globalCells[keys[k]] = lod;
                    representatives.Add(firsts[k]);
                }

                remap[k] = lod;
            }

            blockRemaps[b] = remap;
        }

        var lodVertexCount = representatives.Count;

        // Remap source node -> LOD vertex in parallel
        Parallel.For(0, vertexBlocks.Length, b =>
        {
            var (start, count) = vertexBlocks[b];
            var remap = blockRemaps[b];
            for (var i = start; i < start + count; i++)
            {
                vertexLocal[i] = remap[vertexLocal[i]];
            }
        });

        // Cell-averaged scalars (sequential for deterministic floating-point summation order)
        double[]? lodScalars = null;
        if (scalarValues is not null)
        {
            var sums = new double[lodVertexCount];
            var counts = new int[lodVertexCount];
            var n = Math.Min(vertexCount, scalarValues.Length);
            for (var i = 0; i < n; i++)
            {
                var lod = vertexLocal[i];
                sums[lod] += scalarValues[i];
                counts[lod]++;
            }

            lodScalars = new double[lodVertexCount];
            for (var i = 0; i < lodVertexCount; i++)
            {
                lodScalars[i] = counts[i] > 0 ? sums[i] / counts[i] : 0.0;
            }
        }

        // --- Triangle remapping (degenerate + duplicate removal) ---
        // Deduplicate within blocks first and merge in block order so surviving triangles match sequential order.
        var triangleCount = triangleIndices.Length / 3;
        var triangleBlocks = SplitBlocks(triangleCount);
        var blockTriangles = new List<int>[triangleBlocks.Length];
        var blockCanonicals = new List<(int A, int B, int C)>[triangleBlocks.Length];

        Parallel.For(0, triangleBlocks.Length, b =>
        {
            var (start, count) = triangleBlocks[b];
            var seen = new HashSet<(int, int, int)>();
            var tris = new List<int>();
            var canonicals = new List<(int, int, int)>();
            for (var t = start; t < start + count; t++)
            {
                var a = vertexLocal[triangleIndices[t * 3]];
                var bb = vertexLocal[triangleIndices[t * 3 + 1]];
                var c = vertexLocal[triangleIndices[t * 3 + 2]];
                if (a == bb || bb == c || a == c)
                {
                    continue; // Collapsed to degenerate triangle during clustering
                }

                // Order-independent duplicate check; preserves winding of the first occurrence
                var canonical = Canonical(a, bb, c);
                if (seen.Add(canonical))
                {
                    tris.Add(a);
                    tris.Add(bb);
                    tris.Add(c);
                    canonicals.Add(canonical);
                }
            }

            blockTriangles[b] = tris;
            blockCanonicals[b] = canonicals;
        });

        var globalSeen = new HashSet<(int, int, int)>();
        var lodTriangles = new List<int>();
        for (var b = 0; b < triangleBlocks.Length; b++)
        {
            var tris = blockTriangles[b];
            var canonicals = blockCanonicals[b];
            for (var k = 0; k < canonicals.Count; k++)
            {
                if (globalSeen.Add(canonicals[k]))
                {
                    lodTriangles.Add(tris[k * 3]);
                    lodTriangles.Add(tris[k * 3 + 1]);
                    lodTriangles.Add(tris[k * 3 + 2]);
                }
            }
        }

        if (lodTriangles.Count < 3)
        {
            return null;
        }

        return new LodMeshData
        {
            RepresentativeNodes = [.. representatives],
            TriangleIndices = [.. lodTriangles],
            ScalarValues = lodScalars,
        };
    }

    private static (int Start, int Count)[] SplitBlocks(int total)
    {
        var blockCount = Math.Max((total + BlockSize - 1) / BlockSize, 1);
        var result = new (int, int)[blockCount];
        for (var b = 0; b < blockCount; b++)
        {
            var start = b * BlockSize;
            result[b] = (start, Math.Min(BlockSize, total - start));
        }

        return result;
    }

    private static (int, int, int) Canonical(int a, int b, int c)
    {
        // Sort 3 values with minimal branching
        if (a > b)
        {
            (a, b) = (b, a);
        }

        if (b > c)
        {
            (b, c) = (c, b);
        }

        if (a > b)
        {
            (a, b) = (b, a);
        }

        return (a, b, c);
    }
}
