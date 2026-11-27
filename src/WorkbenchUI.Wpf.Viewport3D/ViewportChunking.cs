namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Range metadata for a single mesh chunk. Triangles form a contiguous range in input order,
/// and <paramref name="VertexCount"/> is the number of unique nodes referenced by that range
/// (the size of the chunk-local vertex buffer).
/// </summary>
/// <param name="TriangleStart">Global starting triangle index of the chunk (in triangle units).</param>
/// <param name="TriangleCount">Number of triangles in the chunk.</param>
/// <param name="VertexCount">Number of unique nodes referenced by the chunk.</param>
internal readonly record struct ChunkBoundary(int TriangleStart, int TriangleCount, int VertexCount);

/// <summary>
/// Remaps global node indices to chunk-local vertex indices.
/// Uses an epoch counter to reset between chunks in O(1) without clearing the array
/// (avoids clearing 25M entries x chunk count).
/// </summary>
internal sealed class ChunkVertexRemap(int vertexCount)
{
    private readonly int[] _localIndex = new int[vertexCount];
    private readonly int[] _epoch = new int[vertexCount]; // 0 = unused
    private int _currentEpoch;
    private int _count;

    /// <summary>Number of unique nodes currently assigned to the active chunk.</summary>
    public int Count => _count;

    /// <summary>Starts a new chunk, invalidating all prior assignments.</summary>
    public void BeginChunk()
    {
        _currentEpoch++;
        _count = 0;
    }

    /// <summary>Returns whether the specified global node is already assigned in the current chunk.</summary>
    public bool Contains(int globalVertex) => _epoch[globalVertex] == _currentEpoch;

    /// <summary>
    /// Returns the chunk-local index for <paramref name="globalVertex"/>, assigning a sequential index if unseen.
    /// </summary>
    public int GetOrAdd(int globalVertex)
    {
        if (_epoch[globalVertex] == _currentEpoch)
        {
            return _localIndex[globalVertex];
        }

        _epoch[globalVertex] = _currentEpoch;
        _localIndex[globalVertex] = _count;
        return _count++;
    }
}

/// <summary>
/// Pure chunk-splitting math for large meshes, independent of the GPU for unit testing.
/// <para>
/// Because D3D11 limits single-resource sizes (128 MB guaranteed, ~1-2 GB practical), multi-million-triangle
/// meshes are split into multiple vertex/index buffer chunks. Triangles are greedily packed in input order
/// and referenced nodes are gathered into chunk-local vertex buffers (boundary node duplication on surface
/// meshes is negligible).
/// </para>
/// </summary>
internal static class ViewportChunking
{
    /// <summary>Maximum vertices per chunk (20 B/vertex x 4M = 80 MB after normal compression).</summary>
    public const int DefaultMaxVerticesPerChunk = 4_000_000;

    /// <summary>Maximum triangles per chunk (12 B/triangle x 8M = 96 MB).</summary>
    public const int DefaultMaxTrianglesPerChunk = 8_000_000;

    /// <summary>
    /// Greedily scans the triangle stream to compute contiguous chunk boundaries within vertex and triangle limits.
    /// Matches the scan order of <see cref="BuildChunkData"/> so unique node counts always agree.
    /// </summary>
    public static List<ChunkBoundary> ComputeChunkBoundaries(
        int[] triangleIndices, int vertexCount,
        int maxVerticesPerChunk = DefaultMaxVerticesPerChunk,
        int maxTrianglesPerChunk = DefaultMaxTrianglesPerChunk)
    {
        ArgumentNullException.ThrowIfNull(triangleIndices);
        maxVerticesPerChunk = Math.Max(maxVerticesPerChunk, 3); // Ensure at least one triangle fits
        maxTrianglesPerChunk = Math.Max(maxTrianglesPerChunk, 1);

        var result = new List<ChunkBoundary>();
        var triangleCount = triangleIndices.Length / 3;
        if (triangleCount == 0 || vertexCount == 0)
        {
            return result;
        }

        var remap = new ChunkVertexRemap(vertexCount);
        remap.BeginChunk();
        var chunkStart = 0;
        var chunkTriangles = 0;

        for (var t = 0; t < triangleCount; t++)
        {
            var a = triangleIndices[t * 3];
            var b = triangleIndices[t * 3 + 1];
            var c = triangleIndices[t * 3 + 2];

            // Count newly introduced nodes for this triangle (avoiding double-counting within the triangle)
            var newVertices = 0;
            if (!remap.Contains(a))
            {
                newVertices++;
            }

            if (b != a && !remap.Contains(b))
            {
                newVertices++;
            }

            if (c != a && c != b && !remap.Contains(c))
            {
                newVertices++;
            }

            if (chunkTriangles > 0
                && (remap.Count + newVertices > maxVerticesPerChunk || chunkTriangles + 1 > maxTrianglesPerChunk))
            {
                result.Add(new ChunkBoundary(chunkStart, chunkTriangles, remap.Count));
                chunkStart = t;
                chunkTriangles = 0;
                remap.BeginChunk();
            }

            remap.GetOrAdd(a);
            remap.GetOrAdd(b);
            remap.GetOrAdd(c);
            chunkTriangles++;
        }

        result.Add(new ChunkBoundary(chunkStart, chunkTriangles, remap.Count));
        return result;
    }

    /// <summary>
    /// Builds local triangle indices and the local-to-global vertex map for a single chunk.
    /// The global map is used for gathering vertex attributes and partial displacement buffer updates.
    /// <paramref name="remap"/> is supplied by the caller for reuse across chunks (BeginChunk is called internally).
    /// </summary>
    public static (int[] LocalTriangles, int[] GlobalVertices) BuildChunkData(
        int[] triangleIndices, ChunkBoundary boundary, ChunkVertexRemap remap)
    {
        ArgumentNullException.ThrowIfNull(triangleIndices);
        ArgumentNullException.ThrowIfNull(remap);

        var localTriangles = new int[boundary.TriangleCount * 3];
        var globalVertices = new int[boundary.VertexCount];

        remap.BeginChunk();
        var srcBase = boundary.TriangleStart * 3;
        for (var i = 0; i < localTriangles.Length; i++)
        {
            var global = triangleIndices[srcBase + i];
            var before = remap.Count;
            var local = remap.GetOrAdd(global);
            if (remap.Count > before)
            {
                globalVertices[local] = global;
            }

            localTriangles[i] = local;
        }

        return (localTriangles, globalVertices);
    }
}
