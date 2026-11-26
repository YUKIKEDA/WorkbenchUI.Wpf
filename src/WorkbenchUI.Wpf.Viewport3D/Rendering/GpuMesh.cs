using System.Numerics;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

namespace WorkbenchUI.Wpf.Viewport3D.Rendering;

/// <summary>
/// GPU resources for a single mesh chunk.
/// Vertex layout is interleaved position (12 B) + octahedral normal (4 B) + scalar (4 B) = 20 B in slot 0;
/// displacements live in a separate Dynamic buffer (12 B/vertex) in slot 1. Meshes without displacements
/// omit the displacement buffer and bind the renderer's shared zero buffer (stride 0) to save GPU memory.
/// Indices are **chunk-local**, with <see cref="TriangleBase"/> providing the global pick ID offset
/// and <see cref="GlobalVertices"/> mapping local vertices back to global nodes for displacement gathering.
/// <see cref="BoundsMin"/> and <see cref="BoundsMax"/> store the undeformed AABB for frustum culling.
/// </summary>
internal sealed unsafe class GpuMeshChunk : IDisposable
{
    private ComPtr<ID3D11Buffer> _vertexBuffer;
    private ComPtr<ID3D11Buffer> _displacementBuffer;
    private ComPtr<ID3D11Buffer> _triangleIndexBuffer;
    private ComPtr<ID3D11Buffer> _edgeIndexBuffer;
    private int _disposed;

    internal GpuMeshChunk(int[] globalVertices, uint triangleBase)
    {
        GlobalVertices = globalVertices;
        TriangleBase = triangleBase;
    }

    /// <summary>Local vertex index -> global node index (used for partial displacement buffer updates).</summary>
    public int[] GlobalVertices { get; }

    /// <summary>Starting global triangle index for this chunk (GPU ID pick offset).</summary>
    public uint TriangleBase { get; }

    /// <summary>Minimum corner of the undeformed AABB in local space (for frustum culling).</summary>
    public Vector3 BoundsMin { get; internal set; }

    /// <summary>Maximum corner of the undeformed AABB in local space (for frustum culling).</summary>
    public Vector3 BoundsMax { get; internal set; }

    public int VertexCount => GlobalVertices.Length;

    public uint TriangleIndexCount { get; internal set; }

    public uint EdgeIndexCount { get; internal set; }

    public ID3D11Buffer* VertexBufferHandle => _vertexBuffer.Handle;

    /// <summary>Displacement buffer handle (null when undeformed; renderer binds the shared zero buffer).</summary>
    public ID3D11Buffer* DisplacementBufferHandle => _displacementBuffer.Handle;

    public ID3D11Buffer* TriangleIndexBufferHandle => _triangleIndexBuffer.Handle;

    public ID3D11Buffer* EdgeIndexBufferHandle => _edgeIndexBuffer.Handle;

    internal void SetVertexBuffer(ComPtr<ID3D11Buffer> buffer)
    {
        ReleaseComPtr(ref _vertexBuffer);
        _vertexBuffer = buffer;
    }

    internal void SetDisplacementBuffer(ComPtr<ID3D11Buffer> buffer)
    {
        ReleaseComPtr(ref _displacementBuffer);
        _displacementBuffer = buffer;
    }

    internal void SetTriangleIndexBuffer(ComPtr<ID3D11Buffer> buffer)
    {
        ReleaseComPtr(ref _triangleIndexBuffer);
        _triangleIndexBuffer = buffer;
    }

    internal void SetEdgeIndexBuffer(ComPtr<ID3D11Buffer> buffer)
    {
        ReleaseComPtr(ref _edgeIndexBuffer);
        _edgeIndexBuffer = buffer;
    }

    public void Dispose()
    {
        // Guard against double-dispose (e.g., race between generation invalidation and cancellation)
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        ReleaseComPtr(ref _vertexBuffer);
        ReleaseComPtr(ref _displacementBuffer);
        ReleaseComPtr(ref _triangleIndexBuffer);
        ReleaseComPtr(ref _edgeIndexBuffer);
    }

    /// <summary>
    /// Silk.NET's <see cref="ComPtr{T}.Dispose"/> does not null out Handle,
    /// so detach and release explicitly to prevent double-release.
    /// </summary>
    private static void ReleaseComPtr(ref ComPtr<ID3D11Buffer> buffer)
    {
        var handle = buffer.Detach();
        if (handle is not null)
        {
            ((IUnknown*)handle)->Release();
        }
    }
}

/// <summary>
/// GPU resources for a single <see cref="ViewportMesh"/> part.
/// <para>
/// For large-mesh support, vertex and index buffers are split along <see cref="ViewportChunking"/> boundaries
/// so rendering and picking draw chunk by chunk. Construction runs sequentially across chunks and in parallel
/// within each chunk, keeping only compressed normals (4 B/vertex) as a full-size intermediate array.
/// Meshes exceeding the LOD triangle threshold also build interactive LOD chunks via grid vertex clustering.
/// </para>
/// </summary>
internal sealed unsafe class GpuMesh : IDisposable
{
    /// <summary>Vertex stride: position float3 (12 B) + octahedral normal uint (4 B) + scalar float (4 B).</summary>
    public const uint VertexStride = 20;

    public const uint DisplacementStride = 12;

    /// <summary>Glyph instance stride (<see cref="ViewportGlyph.FloatsPerInstance"/> * 4 bytes).</summary>
    public const uint GlyphInstanceStride = ViewportGlyph.FloatsPerInstance * sizeof(float);

    /// <summary>Maximum glyph instances per instance buffer (56 B * 2M = 112 MB).</summary>
    public const int MaxGlyphInstancesPerBuffer = 2_000_000;

    /// <summary>Minimum vertex count to enable parallel per-chunk gathering.</summary>
    private const int ParallelThreshold = 65536;

    private readonly List<GpuMeshChunk> _chunks = [];
    private readonly List<GpuMeshChunk> _lodChunks = [];
    private readonly List<(ComPtr<ID3D11Buffer> Buffer, uint InstanceCount, int CapacityFloats)> _glyphBuffers = [];

    /// <summary>LOD vertex -> source node index (used when gathering updated displacements and scalars).</summary>
    private int[]? _lodRepresentativeNodes;

    /// <summary>Full-resolution positions (before recentering) retained for partial scalar updates.</summary>
    private double[]? _positions;

    /// <summary>Full-resolution octahedral normals retained for vertex buffer regathering on scalar updates.</summary>
    private uint[]? _octNormals;

    private double[]? _lodPositions;
    private uint[]? _lodOctNormals;
    private double _originX;
    private double _originY;
    private double _originZ;

    /// <summary>Whether vertex buffers are Dynamic (supporting Map/WriteDiscard scalar updates).</summary>
    private bool _dynamicVertices;

    private GpuMesh()
    {
    }

    /// <summary>Full-resolution render chunks.</summary>
    public IReadOnlyList<GpuMeshChunk> Chunks => _chunks;

    /// <summary>Interactive LOD render chunks (empty if below threshold).</summary>
    public IReadOnlyList<GpuMeshChunk> LodChunks => _lodChunks;

    public bool HasLod => _lodChunks.Count > 0;

    /// <summary>Total global triangle count across all chunks.</summary>
    public int TriangleCount { get; private set; }

    /// <summary>Triangle count of the LOD mesh (0 when no LOD is built).</summary>
    public int LodTriangleCount { get; private set; }

    /// <summary>Source mesh node count (excluding chunk-boundary duplicates).</summary>
    public int VertexCount { get; private set; }

    public bool HasScalars { get; private set; }

    /// <summary>Whether vertex scalars can be updated via Map (true when constructed with scalars).</summary>
    public bool SupportsScalarUpdate => _dynamicVertices;

    /// <summary>Whether edge extraction was skipped because <see cref="WorkbenchViewport.EdgeExtractionLimit"/> was exceeded.</summary>
    public bool EdgesSkipped { get; private set; }

    /// <summary>Maximum displacement magnitude (AABB expansion margin for frustum culling).</summary>
    public float MaxDisplacementMagnitude { get; private set; }

    public Vector4 Color { get; set; }

    public bool ShowEdges { get; set; }

    /// <summary>Whether this part is subject to section clipping (synced from <see cref="ViewportMesh.IsClippable"/>).</summary>
    public bool IsClippable { get; set; } = true;

    public bool IsTransparent => Color.W < 0.999f;

    /// <summary>Total number of glyph instances (0 if no glyphs).</summary>
    public uint GlyphInstanceCount { get; private set; }

    /// <summary>Glyph instance buffers (split into multiple buffers for large vector fields).</summary>
    public IReadOnlyList<(ComPtr<ID3D11Buffer> Buffer, uint InstanceCount, int CapacityFloats)> GlyphBuffers => _glyphBuffers;

    /// <summary>
    /// Builds GPU resources from a <see cref="ViewportMesh"/>. Coordinates are recentered around the scene origin
    /// and converted to float, while normals are computed across the entire mesh before chunking to avoid shading seams.
    /// Safe to invoke from a background thread (ID3D11Device is free-threaded).
    /// </summary>
    /// <param name="device">Direct3D 11 device used to create GPU buffers.</param>
    /// <param name="mesh">Source mesh model.</param>
    /// <param name="originX">Scene recentering origin X.</param>
    /// <param name="originY">Scene recentering origin Y.</param>
    /// <param name="originZ">Scene recentering origin Z.</param>
    /// <param name="edgeExtractionLimit">Meshes exceeding this triangle count skip edge extraction.</param>
    /// <param name="interactiveLodThreshold">Meshes exceeding this triangle count build an interactive LOD mesh.</param>
    /// <param name="meshBounds">Bounding box of the mesh (used to define the LOD clustering grid).</param>
    /// <param name="maxVerticesPerChunk">Maximum vertices allowed per chunk.</param>
    /// <param name="maxTrianglesPerChunk">Maximum triangles allowed per chunk.</param>
    /// <param name="cancellationToken">Cancellation token checked at chunk boundaries.</param>
    /// <param name="progress">Optional progress callback in [0, 1] invoked on the builder thread per completed chunk.</param>
    public static GpuMesh? Create(
        ComPtr<ID3D11Device> device,
        ViewportMesh mesh,
        double originX, double originY, double originZ,
        int edgeExtractionLimit = int.MaxValue,
        int interactiveLodThreshold = int.MaxValue,
        Bounds3D meshBounds = default,
        int maxVerticesPerChunk = ViewportChunking.DefaultMaxVerticesPerChunk,
        int maxTrianglesPerChunk = ViewportChunking.DefaultMaxTrianglesPerChunk,
        CancellationToken cancellationToken = default,
        Action<double>? progress = null)
    {
        var vertexCount = mesh.VertexCount;
        var triangles = mesh.TriangleIndices;
        if (vertexCount == 0 || triangles.Length < 3)
        {
            return null;
        }

        var scalars = mesh.ScalarValues;
        var displacements = mesh.Displacements;
        var triangleCount = triangles.Length / 3;
        var extractEdges = triangleCount <= edgeExtractionLimit;

        // Use Dynamic VB when scalars exist so playback updates can use Map/WriteDiscard
        var dynamicVertices = scalars is not null;
        var result = new GpuMesh
        {
            TriangleCount = triangleCount,
            VertexCount = vertexCount,
            HasScalars = scalars is not null,
            EdgesSkipped = !extractEdges,
            MaxDisplacementMagnitude =
                (float)ViewportDeformation.GetMaxDisplacementMagnitude(displacements),
            _positions = mesh.Positions,
            _originX = originX,
            _originY = originY,
            _originZ = originZ,
            _dynamicVertices = dynamicVertices,
        };

        try
        {
            // Progress weights: when LOD is built, 70% for full mesh and 30% for LOD clustering + chunks
            var buildLod = triangleCount > interactiveLodThreshold && !meshBounds.IsEmpty;
            var mainWeight = buildLod ? 0.7 : 1.0;

            result._octNormals = BuildChunkSet(
                device, result._chunks, mesh.Positions, triangles, scalars, displacements,
                displacementSourceMap: null, originX, originY, originZ, extractEdges,
                maxVerticesPerChunk, maxTrianglesPerChunk, dynamicVertices,
                cancellationToken, p => progress?.Invoke(p * mainWeight));

            // Interactive LOD (~1/20 decimation via grid clustering) built with the same chunk pipeline (no edges)
            if (buildLod)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var lod = ViewportLod.Build(mesh.Positions, triangles, scalars, meshBounds);
                if (lod is not null)
                {
                    var lodPositions = GatherLodPositions(mesh.Positions, lod.RepresentativeNodes);
                    var lodDisplacements = displacements is null
                        ? null
                        : GatherLodDoubles(displacements, lod.RepresentativeNodes);

                    result._lodOctNormals = BuildChunkSet(
                        device, result._lodChunks, lodPositions, lod.TriangleIndices,
                        lod.ScalarValues, lodDisplacements,
                        displacementSourceMap: null, originX, originY, originZ, extractEdges: false,
                        maxVerticesPerChunk, maxTrianglesPerChunk, dynamicVertices,
                        cancellationToken, p => progress?.Invoke(mainWeight + p * (1.0 - mainWeight)));

                    result._lodPositions = lodPositions;
                    result._lodRepresentativeNodes = lod.RepresentativeNodes;
                    result.LodTriangleCount = lod.TriangleCount;
                }
            }

            progress?.Invoke(1.0);
        }
        catch
        {
            result.Dispose();
            throw;
        }

        return result;
    }

    /// <summary>
    /// Builds vertex, index, and edge chunk lists (shared by full-resolution and LOD paths).
    /// Only normals are held at full size, and their float array is released right after octahedral compression (4 B/vertex).
    /// Coordinate recentering and float conversion happen on the fly during per-chunk vertex gathering.
    /// </summary>
    private static uint[] BuildChunkSet(
        ComPtr<ID3D11Device> device,
        List<GpuMeshChunk> target,
        double[] positions, int[] triangles, double[]? scalars, double[]? displacements,
        int[]? displacementSourceMap,
        double originX, double originY, double originZ,
        bool extractEdges, int maxVerticesPerChunk, int maxTrianglesPerChunk,
        bool dynamicVertices,
        CancellationToken cancellationToken = default, Action<double>? progress = null)
    {
        var vertexCount = positions.Length / 3;

        cancellationToken.ThrowIfCancellationRequested();

        // Normals: accumulate in full-size float, then immediately compress to octahedral uint (4 B/vertex)
        uint[] octNormals;
        {
            var normals = ViewportGeometry.ComputeVertexNormalsFromDouble(
                positions, originX, originY, originZ, triangles);
            octNormals = ViewportGeometry.CompressNormals(normals);
        }

        var boundaries = ViewportChunking.ComputeChunkBoundaries(
            triangles, vertexCount, maxVerticesPerChunk, maxTrianglesPerChunk);
        var remap = new ChunkVertexRemap(vertexCount);

        // Sequential across chunks, parallel within each chunk; intermediate buffers are sized to one chunk
        // so peak memory stays independent of total mesh size.
        var builtChunks = 0;
        foreach (var boundary in boundaries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (localTriangles, globalVertices) = ViewportChunking.BuildChunkData(triangles, boundary, remap);
            var chunk = new GpuMeshChunk(globalVertices, (uint)boundary.TriangleStart)
            {
                TriangleIndexCount = (uint)localTriangles.Length,
            };

            // Gather interleaved vertices (position + octahedral normal + scalar), recentering on the fly
            var vertexData = GatherInterleavedVertices(
                positions, octNormals, scalars, globalVertices, originX, originY, originZ);

            var (min, max) = ComputeChunkBounds(vertexData);
            chunk.BoundsMin = min;
            chunk.BoundsMax = max;

            fixed (float* pVertices = vertexData)
            {
                chunk.SetVertexBuffer(dynamicVertices
                    ? CreateDynamicVertexBuffer(
                        device, pVertices, (uint)(vertexData.Length * sizeof(float)))
                    : CreateImmutableBuffer(
                        device, pVertices, (uint)(vertexData.Length * sizeof(float)), BindFlag.VertexBuffer));
            }

            // Create Dynamic displacement buffers only when displacements exist; otherwise bind the shared zero buffer
            if (displacements is not null)
            {
                var chunkDisplacements = GatherDisplacements(displacements, globalVertices, displacementSourceMap);
                chunk.SetDisplacementBuffer(CreateDisplacementBuffer(device, chunkDisplacements));
            }

            fixed (int* pIndices = localTriangles)
            {
                chunk.SetTriangleIndexBuffer(CreateImmutableBuffer(
                    device, pIndices, (uint)(localTriangles.Length * sizeof(int)), BindFlag.IndexBuffer));
            }

            // Edges in chunk-local indices; boundary edges appear in both adjacent chunks without visual artifacts
            if (extractEdges)
            {
                var edges = ViewportGeometry.ExtractEdges(localTriangles);
                if (edges.Length > 0)
                {
                    chunk.EdgeIndexCount = (uint)edges.Length;
                    fixed (int* pEdges = edges)
                    {
                        chunk.SetEdgeIndexBuffer(CreateImmutableBuffer(
                            device, pEdges, (uint)(edges.Length * sizeof(int)), BindFlag.IndexBuffer));
                    }
                }
            }

            target.Add(chunk);
            builtChunks++;
            progress?.Invoke((double)builtChunks / boundaries.Count);
        }

        return octNormals;
    }

    /// <summary>
    /// Replaces only the displacement buffer (gathered per chunk and uploaded via Map/WriteDiscard)
    /// without rebuilding base geometry, keeping transient playback lightweight.
    /// Lazily allocates the buffer when displacements are first supplied, and gathers LOD displacements
    /// via the representative node map.
    /// </summary>
    public void UpdateDisplacements(
        ComPtr<ID3D11Device> device, ComPtr<ID3D11DeviceContext> context, double[]? displacements)
    {
        MaxDisplacementMagnitude = (float)ViewportDeformation.GetMaxDisplacementMagnitude(displacements);
        UpdateChunkDisplacements(device, context, _chunks, displacements, sourceMap: null);
        UpdateChunkDisplacements(device, context, _lodChunks, displacements, _lodRepresentativeNodes);
    }

    /// <summary>
    /// Replaces only vertex scalars (preserving surface topology, normals, and indices)
    /// for voxel filling/solidification animation and contour playback.
    /// Only supported for meshes constructed with scalars (<see cref="_dynamicVertices"/>).
    /// </summary>
    public void UpdateScalars(
        ComPtr<ID3D11Device> device, ComPtr<ID3D11DeviceContext> context, double[]? scalars)
    {
        if (_positions is null || _octNormals is null || !_dynamicVertices)
        {
            return;
        }

        HasScalars = scalars is not null;
        UpdateChunkScalars(
            device, context, _chunks, _positions, _octNormals, scalars,
            _originX, _originY, _originZ);

        if (_lodPositions is null || _lodOctNormals is null || _lodChunks.Count == 0)
        {
            return;
        }

        double[]? lodScalars = null;
        if (scalars is not null && _lodRepresentativeNodes is not null)
        {
            lodScalars = GatherLodScalars(scalars, _lodRepresentativeNodes);
        }

        UpdateChunkScalars(
            device, context, _lodChunks, _lodPositions, _lodOctNormals, lodScalars,
            _originX, _originY, _originZ);
    }

    private static void UpdateChunkScalars(
        ComPtr<ID3D11Device> device, ComPtr<ID3D11DeviceContext> context,
        List<GpuMeshChunk> chunks, double[] positions, uint[] octNormals, double[]? scalars,
        double originX, double originY, double originZ)
    {
        foreach (var chunk in chunks)
        {
            var vertexData = GatherInterleavedVertices(
                positions, octNormals, scalars, chunk.GlobalVertices, originX, originY, originZ);
            var bufferHandle = chunk.VertexBufferHandle;
            if (bufferHandle is null)
            {
                fixed (float* pVertices = vertexData)
                {
                    chunk.SetVertexBuffer(CreateDynamicVertexBuffer(
                        device, pVertices, (uint)(vertexData.Length * sizeof(float))));
                }

                continue;
            }

            MappedSubresource mapped = default;
            SilkMarshal.ThrowHResult(context.Map(
                (ID3D11Resource*)bufferHandle, 0, Map.WriteDiscard, 0, ref mapped));
            try
            {
                fixed (float* pData = vertexData)
                {
                    System.Buffer.MemoryCopy(
                        pData, mapped.PData,
                        vertexData.Length * sizeof(float), vertexData.Length * sizeof(float));
                }
            }
            finally
            {
                context.Unmap((ID3D11Resource*)bufferHandle, 0);
            }
        }
    }

    private static float[] GatherInterleavedVertices(
        double[] positions, uint[] octNormals, double[]? scalars, int[] globalVertices,
        double originX, double originY, double originZ)
    {
        var vertexData = new float[globalVertices.Length * 5];
        ForEachVertex(globalVertices.Length, localIndex =>
        {
            var gv = globalVertices[localIndex];
            var g = gv * 3;
            var dst = localIndex * 5;
            vertexData[dst] = (float)(positions[g] - originX);
            vertexData[dst + 1] = (float)(positions[g + 1] - originY);
            vertexData[dst + 2] = (float)(positions[g + 2] - originZ);
            vertexData[dst + 3] = BitConverter.UInt32BitsToSingle(
                gv < octNormals.Length ? octNormals[gv] : 0u);
            vertexData[dst + 4] = scalars is not null && gv < scalars.Length
                ? (float)scalars[gv]
                : 0.0f;
        });

        return vertexData;
    }

    private static void UpdateChunkDisplacements(
        ComPtr<ID3D11Device> device, ComPtr<ID3D11DeviceContext> context,
        List<GpuMeshChunk> chunks, double[]? displacements, int[]? sourceMap)
    {
        foreach (var chunk in chunks)
        {
            if (chunk.DisplacementBufferHandle is null)
            {
                if (displacements is null)
                {
                    continue; // Remain undeformed (bound to shared zero buffer)
                }

                // First time displacements are assigned: lazily create the buffer
                var initial = GatherDisplacements(displacements, chunk.GlobalVertices, sourceMap);
                chunk.SetDisplacementBuffer(CreateDisplacementBuffer(device, initial));
                continue;
            }

            var data = GatherDisplacements(displacements, chunk.GlobalVertices, sourceMap);
            MappedSubresource mapped = default;
            // Map via raw pointer: wrapping in new ComPtr(handle) AddRefs, which would leak or double-release
            var bufferHandle = chunk.DisplacementBufferHandle;
            SilkMarshal.ThrowHResult(context.Map(
                (ID3D11Resource*)bufferHandle, 0, Map.WriteDiscard, 0, ref mapped));
            try
            {
                fixed (float* pData = data)
                {
                    System.Buffer.MemoryCopy(pData, mapped.PData, data.Length * sizeof(float), data.Length * sizeof(float));
                }
            }
            finally
            {
                context.Unmap((ID3D11Resource*)bufferHandle, 0);
            }
        }
    }

    /// <summary>
    /// Replaces glyph instance buffers, splitting large vector fields into multiple buffers of
    /// <see cref="MaxGlyphInstancesPerBuffer"/> instances. Reuses existing buffers via Map/WriteDiscard when sufficient.
    /// </summary>
    public void UpdateGlyphInstances(
        ComPtr<ID3D11Device> device, ComPtr<ID3D11DeviceContext> context, float[] data, int instanceCount)
    {
        GlyphInstanceCount = (uint)Math.Max(instanceCount, 0);
        if (instanceCount <= 0 || data.Length == 0)
        {
            return;
        }

        var requiredBuffers = (instanceCount + MaxGlyphInstancesPerBuffer - 1) / MaxGlyphInstancesPerBuffer;
        var reusable = _glyphBuffers.Count == requiredBuffers;
        if (reusable)
        {
            for (var i = 0; i < requiredBuffers; i++)
            {
                var count = Math.Min(instanceCount - i * MaxGlyphInstancesPerBuffer, MaxGlyphInstancesPerBuffer);
                if (_glyphBuffers[i].CapacityFloats < count * ViewportGlyph.FloatsPerInstance)
                {
                    reusable = false;
                    break;
                }
            }
        }

        if (!reusable)
        {
            ReleaseGlyphBuffers();
        }

        for (var i = 0; i < requiredBuffers; i++)
        {
            var count = Math.Min(instanceCount - i * MaxGlyphInstancesPerBuffer, MaxGlyphInstancesPerBuffer);
            var floatOffset = i * MaxGlyphInstancesPerBuffer * ViewportGlyph.FloatsPerInstance;
            var floatCount = count * ViewportGlyph.FloatsPerInstance;

            fixed (float* pData = &data[floatOffset])
            {
                if (reusable)
                {
                    var (buffer, _, capacity) = _glyphBuffers[i];
                    MappedSubresource mapped = default;
                    SilkMarshal.ThrowHResult(context.Map(buffer, 0, Map.WriteDiscard, 0, ref mapped));
                    try
                    {
                        System.Buffer.MemoryCopy(pData, mapped.PData, capacity * sizeof(float), floatCount * sizeof(float));
                    }
                    finally
                    {
                        context.Unmap(buffer, 0);
                    }

                    _glyphBuffers[i] = (buffer, (uint)count, capacity);
                }
                else
                {
                    var desc = new BufferDesc
                    {
                        ByteWidth = (uint)(floatCount * sizeof(float)),
                        Usage = Usage.Dynamic,
                        BindFlags = (uint)BindFlag.VertexBuffer,
                        CPUAccessFlags = (uint)CpuAccessFlag.Write,
                    };
                    var subresource = new SubresourceData { PSysMem = pData };
                    ComPtr<ID3D11Buffer> buffer = default;
                    SilkMarshal.ThrowHResult(device.CreateBuffer(in desc, in subresource, ref buffer));
                    _glyphBuffers.Add((buffer, (uint)count, floatCount));
                }
            }
        }
    }

    /// <summary>
    /// Gathers chunk displacements (global array -> local float, parallelized for large chunks).
    /// <paramref name="sourceMap"/> is used for LOD chunks (LOD vertex -> representative node).
    /// </summary>
    private static float[] GatherDisplacements(double[]? displacements, int[] globalVertices, int[]? sourceMap)
    {
        var result = new float[globalVertices.Length * 3];
        if (displacements is null)
        {
            return result;
        }

        ForEachVertex(globalVertices.Length, localIndex =>
        {
            var node = globalVertices[localIndex];
            if (sourceMap is not null)
            {
                node = sourceMap[node];
            }

            var g = node * 3;
            if (g + 2 < displacements.Length)
            {
                var dst = localIndex * 3;
                result[dst] = (float)displacements[g];
                result[dst + 1] = (float)displacements[g + 1];
                result[dst + 2] = (float)displacements[g + 2];
            }
        });

        return result;
    }

    /// <summary>Gathers LOD vertex positions from representative nodes.</summary>
    private static double[] GatherLodPositions(double[] positions, int[] representativeNodes)
    {
        var result = new double[representativeNodes.Length * 3];
        ForEachVertex(representativeNodes.Length, i =>
        {
            var g = representativeNodes[i] * 3;
            result[i * 3] = positions[g];
            result[i * 3 + 1] = positions[g + 1];
            result[i * 3 + 2] = positions[g + 2];
        });

        return result;
    }

    /// <summary>Gathers LOD per-vertex scalars from representative nodes.</summary>
    private static double[] GatherLodScalars(double[] scalars, int[] representativeNodes)
    {
        var result = new double[representativeNodes.Length];
        ForEachVertex(representativeNodes.Length, i =>
        {
            var node = representativeNodes[i];
            result[i] = node < scalars.Length ? scalars[node] : 0.0;
        });

        return result;
    }

    /// <summary>Gathers LOD 3-component vectors (displacements) from representative nodes.</summary>
    private static double[] GatherLodDoubles(double[] values, int[] representativeNodes)
    {
        var result = new double[representativeNodes.Length * 3];
        ForEachVertex(representativeNodes.Length, i =>
        {
            var g = representativeNodes[i] * 3;
            if (g + 2 < values.Length)
            {
                result[i * 3] = values[g];
                result[i * 3 + 1] = values[g + 1];
                result[i * 3 + 2] = values[g + 2];
            }
        });

        return result;
    }

    /// <summary>Computes an AABB from interleaved vertices (5 floats per vertex).</summary>
    private static (Vector3 Min, Vector3 Max) ComputeChunkBounds(float[] vertexData)
    {
        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);
        for (var i = 0; i + 2 < vertexData.Length; i += 5)
        {
            var p = new Vector3(vertexData[i], vertexData[i + 1], vertexData[i + 2]);
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        return min.X <= max.X ? (min, max) : (Vector3.Zero, Vector3.Zero);
    }

    private static ComPtr<ID3D11Buffer> CreateDisplacementBuffer(
        ComPtr<ID3D11Device> device, float[] data)
    {
        fixed (float* pData = data)
        {
            return CreateDynamicVertexBuffer(device, pData, (uint)(data.Length * sizeof(float)));
        }
    }

    private static ComPtr<ID3D11Buffer> CreateDynamicVertexBuffer(
        ComPtr<ID3D11Device> device, float* data, uint byteWidth)
    {
        var desc = new BufferDesc
        {
            ByteWidth = byteWidth,
            Usage = Usage.Dynamic,
            BindFlags = (uint)BindFlag.VertexBuffer,
            CPUAccessFlags = (uint)CpuAccessFlag.Write,
        };
        var subresource = new SubresourceData { PSysMem = data };
        ComPtr<ID3D11Buffer> buffer = default;
        SilkMarshal.ThrowHResult(device.CreateBuffer(in desc, in subresource, ref buffer));
        return buffer;
    }

    private static void ForEachVertex(int count, Action<int> body)
    {
        if (count >= ParallelThreshold)
        {
            Parallel.For(0, count, body);
        }
        else
        {
            for (var i = 0; i < count; i++)
            {
                body(i);
            }
        }
    }

    private static ComPtr<ID3D11Buffer> CreateImmutableBuffer(
        ComPtr<ID3D11Device> device, void* data, uint byteWidth, BindFlag bindFlag)
    {
        var desc = new BufferDesc
        {
            ByteWidth = byteWidth,
            Usage = Usage.Immutable,
            BindFlags = (uint)bindFlag,
        };
        var subresource = new SubresourceData { PSysMem = data };

        ComPtr<ID3D11Buffer> buffer = default;
        SilkMarshal.ThrowHResult(device.CreateBuffer(in desc, in subresource, ref buffer));
        return buffer;
    }

    private void ReleaseGlyphBuffers()
    {
        for (var i = 0; i < _glyphBuffers.Count; i++)
        {
            var handle = _glyphBuffers[i].Buffer.Detach();
            if (handle is not null)
            {
                ((IUnknown*)handle)->Release();
            }
        }

        _glyphBuffers.Clear();
    }

    public void Dispose()
    {
        foreach (var chunk in _chunks)
        {
            chunk.Dispose();
        }

        foreach (var chunk in _lodChunks)
        {
            chunk.Dispose();
        }

        _chunks.Clear();
        _lodChunks.Clear();
        _lodRepresentativeNodes = null;
        _positions = null;
        _octNormals = null;
        _lodPositions = null;
        _lodOctNormals = null;
        LodTriangleCount = 0;
        ReleaseGlyphBuffers();
        GlyphInstanceCount = 0;
    }
}
