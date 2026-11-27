namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Viewport performance and geometry statistics snapshot returned by <see cref="WorkbenchViewport.GetStatistics"/>.
/// Absolute frame times depend on the hardware/driver environment (GPU vs. WARP), so use them for relative comparisons.
/// </summary>
/// <param name="TriangleCount">Total number of surface triangles across all parts.</param>
/// <param name="VertexCount">Total number of vertices across all parts (excluding chunk-boundary duplicates).</param>
/// <param name="MeshCount">Number of parts with built GPU resources.</param>
/// <param name="ChunkCount">Total number of vertex/index buffer chunks (excluding LOD chunks).</param>
/// <param name="EdgeSkippedMeshCount">Number of parts where edge extraction was skipped because <see cref="WorkbenchViewport.EdgeExtractionLimit"/> was exceeded.</param>
/// <param name="LodTriangleCount">Total number of triangles in interactive LOD meshes (0 if LOD was not built).</param>
/// <param name="IsLodActive">Whether the most recent frame was rendered using interactive LOD (true during camera interaction or animation).</param>
/// <param name="LastDrawnChunkCount">Number of mesh-pass chunks actually drawn in the most recent frame after frustum culling.</param>
/// <param name="LastGeometryBuildTime">Elapsed time for the most recent geometry build (float conversion, normals, chunking, LOD, and GPU upload).</param>
/// <param name="LastRenderTime">CPU-measured elapsed time for the most recent frame render (including command submission and GPU wait).</param>
public readonly record struct ViewportStatistics(
    long TriangleCount,
    long VertexCount,
    int MeshCount,
    int ChunkCount,
    int EdgeSkippedMeshCount,
    long LodTriangleCount,
    bool IsLodActive,
    int LastDrawnChunkCount,
    TimeSpan LastGeometryBuildTime,
    TimeSpan LastRenderTime);
