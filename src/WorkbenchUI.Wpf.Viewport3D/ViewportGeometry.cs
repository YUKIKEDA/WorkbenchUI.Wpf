namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Pure geometry preprocessing utilities for triangle meshes.
/// </summary>
internal static class ViewportGeometry
{
    /// <summary>Computes the bounding box of a flat coordinate array (x, y, z, ...).</summary>
    public static Bounds3D ComputeBounds(double[] positions)
    {
        ArgumentNullException.ThrowIfNull(positions);
        if (positions.Length < 3)
        {
            return Bounds3D.Empty;
        }

        double minX = double.PositiveInfinity, minY = double.PositiveInfinity, minZ = double.PositiveInfinity;
        double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity, maxZ = double.NegativeInfinity;

        for (var i = 0; i + 2 < positions.Length; i += 3)
        {
            var x = positions[i];
            var y = positions[i + 1];
            var z = positions[i + 2];
            if (double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(z))
            {
                continue;
            }

            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            minZ = Math.Min(minZ, z);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
            maxZ = Math.Max(maxZ, z);
        }

        return minX > maxX
            ? Bounds3D.Empty
            : new Bounds3D(minX, minY, minZ, maxX, maxY, maxZ);
    }

    /// <summary>Parallelization threshold (below this vertex count, thread-launch overhead outweighs gains).</summary>
    private const int ParallelThreshold = 65536;

    /// <summary>
    /// Recenters double-precision coordinates around the specified origin (typically the scene center)
    /// and converts them to float. Always use this path to prevent vertex jitter during camera interaction
    /// on large-coordinate models (e.g., geodetic coordinates around 10^6).
    /// Because each vertex transforms independently, parallel execution produces bit-identical results.
    /// </summary>
    public static float[] ToLocalPositions(double[] positions, double originX, double originY, double originZ)
    {
        ArgumentNullException.ThrowIfNull(positions);
        var result = new float[positions.Length];
        var vertexCount = positions.Length / 3;

        if (vertexCount >= ParallelThreshold)
        {
            Parallel.For(0, vertexCount, v =>
            {
                var i = v * 3;
                result[i] = (float)(positions[i] - originX);
                result[i + 1] = (float)(positions[i + 1] - originY);
                result[i + 2] = (float)(positions[i + 2] - originZ);
            });
        }
        else
        {
            for (var i = 0; i + 2 < positions.Length; i += 3)
            {
                result[i] = (float)(positions[i] - originX);
                result[i + 1] = (float)(positions[i + 1] - originY);
                result[i + 2] = (float)(positions[i + 2] - originZ);
            }
        }

        return result;
    }

    /// <summary>
    /// Computes area-weighted vertex normals by accumulating triangle cross products
    /// (proportional to 2x triangle area) so larger triangles contribute proportionally more.
    /// <para>
    /// Accumulation runs sequentially to avoid write races and non-deterministic float summation order;
    /// normalization is element-independent and runs in parallel. Computing normals across the full mesh
    /// independently of chunk boundaries prevents shading seams between chunks.
    /// </para>
    /// </summary>
    public static float[] ComputeVertexNormals(float[] localPositions, int[] triangleIndices)
    {
        ArgumentNullException.ThrowIfNull(localPositions);
        ArgumentNullException.ThrowIfNull(triangleIndices);

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

        NormalizeAll(normals);
        return normals;
    }

    /// <summary>
    /// Computes vertex normals directly from double-precision coordinates by recentering and casting
    /// to float on the fly per triangle, avoiding a full-size intermediate float position array.
    /// Uses the identical conversion formula as <see cref="ToLocalPositions"/> so results match bit-for-bit.
    /// </summary>
    public static float[] ComputeVertexNormalsFromDouble(
        double[] positions, double originX, double originY, double originZ, int[] triangleIndices)
    {
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(triangleIndices);

        var normals = new float[positions.Length];
        for (var t = 0; t + 2 < triangleIndices.Length; t += 3)
        {
            var i0 = triangleIndices[t] * 3;
            var i1 = triangleIndices[t + 1] * 3;
            var i2 = triangleIndices[t + 2] * 3;

            var p0X = (float)(positions[i0] - originX);
            var p0Y = (float)(positions[i0 + 1] - originY);
            var p0Z = (float)(positions[i0 + 2] - originZ);

            var e1X = (float)(positions[i1] - originX) - p0X;
            var e1Y = (float)(positions[i1 + 1] - originY) - p0Y;
            var e1Z = (float)(positions[i1 + 2] - originZ) - p0Z;
            var e2X = (float)(positions[i2] - originX) - p0X;
            var e2Y = (float)(positions[i2 + 1] - originY) - p0Y;
            var e2Z = (float)(positions[i2 + 2] - originZ) - p0Z;

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

        NormalizeAll(normals);
        return normals;
    }

    private static void NormalizeAll(float[] normals)
    {
        var vertexCount = normals.Length / 3;
        if (vertexCount >= ParallelThreshold)
        {
            Parallel.For(0, vertexCount, v => NormalizeAt(normals, v * 3));
        }
        else
        {
            for (var i = 0; i + 2 < normals.Length; i += 3)
            {
                NormalizeAt(normals, i);
            }
        }

        static void NormalizeAt(float[] normals, int i)
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
    }

    // ================= Octahedral normal compression =================
    // Maps a unit normal onto an octahedron and quantizes it to two 16-bit unorms (12 B -> 4 B).
    // Maximum angular error is ~0.02 degrees, visually indistinguishable in shading.

    /// <summary>Encodes a unit normal into octahedral 16-bit x 2 format (low 16 bits = u, high 16 bits = v).</summary>
    public static uint EncodeOctahedralNormal(float x, float y, float z)
    {
        var l1 = MathF.Abs(x) + MathF.Abs(y) + MathF.Abs(z);
        float px, py;
        if (l1 < 1e-20f)
        {
            // Treat zero normals (e.g., unreferenced nodes) as +Z
            px = 0.0f;
            py = 0.0f;
        }
        else
        {
            px = x / l1;
            py = y / l1;
            if (z < 0.0f)
            {
                // Fold the lower hemisphere onto the upper octahedral faces
                var ox = px;
                px = (1.0f - MathF.Abs(py)) * SignNotZero(ox);
                py = (1.0f - MathF.Abs(ox)) * SignNotZero(py);
            }
        }

        var u = (uint)Math.Clamp((int)MathF.Round((px * 0.5f + 0.5f) * 65535.0f), 0, 65535);
        var v = (uint)Math.Clamp((int)MathF.Round((py * 0.5f + 0.5f) * 65535.0f), 0, 65535);
        return u | (v << 16);
    }

    /// <summary>Decodes an octahedral 16-bit x 2 normal into a unit vector (matches HLSL DecodeOctNormal).</summary>
    public static (float X, float Y, float Z) DecodeOctahedralNormal(uint packed)
    {
        var ex = (packed & 0xFFFFu) / 65535.0f * 2.0f - 1.0f;
        var ey = (packed >> 16) / 65535.0f * 2.0f - 1.0f;
        var vz = 1.0f - MathF.Abs(ex) - MathF.Abs(ey);
        var vx = ex;
        var vy = ey;
        if (vz < 0.0f)
        {
            vx = (1.0f - MathF.Abs(ey)) * SignNotZero(ex);
            vy = (1.0f - MathF.Abs(ex)) * SignNotZero(ey);
        }

        var len = MathF.Sqrt(vx * vx + vy * vy + vz * vz);
        return len > 1e-20f ? (vx / len, vy / len, vz / len) : (0.0f, 0.0f, 1.0f);
    }

    /// <summary>Compresses a float3 normal array into an octahedral uint array in parallel.</summary>
    public static uint[] CompressNormals(float[] normals)
    {
        ArgumentNullException.ThrowIfNull(normals);
        var vertexCount = normals.Length / 3;
        var result = new uint[vertexCount];

        if (vertexCount >= ParallelThreshold)
        {
            Parallel.For(0, vertexCount, v =>
                result[v] = EncodeOctahedralNormal(normals[v * 3], normals[v * 3 + 1], normals[v * 3 + 2]));
        }
        else
        {
            for (var v = 0; v < vertexCount; v++)
            {
                result[v] = EncodeOctahedralNormal(normals[v * 3], normals[v * 3 + 1], normals[v * 3 + 2]);
            }
        }

        return result;
    }

    private static float SignNotZero(float v) => v >= 0.0f ? 1.0f : -1.0f;

    /// <summary>
    /// Extracts deduplicated line-segment edge pairs from a triangle list for wireframe overlay rendering.
    /// Returns vertex index pairs (two entries per segment).
    /// </summary>
    public static int[] ExtractEdges(int[] triangleIndices)
    {
        ArgumentNullException.ThrowIfNull(triangleIndices);

        var edges = new HashSet<(int A, int B)>();
        for (var t = 0; t + 2 < triangleIndices.Length; t += 3)
        {
            AddEdge(edges, triangleIndices[t], triangleIndices[t + 1]);
            AddEdge(edges, triangleIndices[t + 1], triangleIndices[t + 2]);
            AddEdge(edges, triangleIndices[t + 2], triangleIndices[t]);
        }

        var result = new int[edges.Count * 2];
        var i = 0;
        foreach (var (a, b) in edges)
        {
            result[i++] = a;
            result[i++] = b;
        }

        return result;

        static void AddEdge(HashSet<(int, int)> edges, int a, int b) =>
            edges.Add(a < b ? (a, b) : (b, a));
    }

    /// <summary>Converts nodal scalar values to float (NaN values are preserved so the shader can handle or discard them).</summary>
    public static float[] ToScalarArray(double[]? scalarValues, int vertexCount)
    {
        var result = new float[vertexCount];
        if (scalarValues is null)
        {
            return result;
        }

        var count = Math.Min(scalarValues.Length, vertexCount);
        for (var i = 0; i < count; i++)
        {
            result[i] = (float)scalarValues[i];
        }

        return result;
    }
}
