using System.Numerics;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Pure helper functions for vector glyph rendering, independent of the GPU for unit testing.
/// Builds unit-length arrow prototype geometry, constructs per-instance buffers from nodal vector fields,
/// and computes orthonormal rotation bases matching the HLSL shader.
/// </summary>
internal static class ViewportGlyph
{
    /// <summary>
    /// Number of floats per glyph instance: base position (3) + displacement (3) + unit direction (3) + magnitude (1) + RGBA color (4).
    /// Actual arrow length is |v| * GlyphScale applied in the shader, so scale changes do not rebuild the instance buffer.
    /// </summary>
    public const int FloatsPerInstance = 14;

    // Unit arrow proportions (+Z axis, total length = 1)
    private const float ShaftRadius = 0.045f;
    private const float HeadRadius = 0.13f;
    private const float HeadStart = 0.68f;

    /// <summary>
    /// Builds low-poly geometry for a unit arrow along +Z (total length 1).
    /// Returns interleaved vertices (position3 + normal3) and triangle indices,
    /// consisting of shaft cylinder, bottom cap, cone base disc, and cone side.
    /// </summary>
    public static (float[] Vertices, int[] Indices) BuildArrowGeometry(int segments = 12)
    {
        segments = Math.Max(segments, 3);

        var vertices = new List<float>(segments * 40);
        var indices = new List<int>(segments * 15);

        void AddVertex(float px, float py, float pz, float nx, float ny, float nz)
        {
            vertices.Add(px);
            vertices.Add(py);
            vertices.Add(pz);
            vertices.Add(nx);
            vertices.Add(ny);
            vertices.Add(nz);
        }

        var cos = new float[segments];
        var sin = new float[segments];
        for (var k = 0; k < segments; k++)
        {
            var theta = 2.0 * Math.PI * k / segments;
            cos[k] = (float)Math.Cos(theta);
            sin[k] = (float)Math.Sin(theta);
        }

        // 1. Shaft side (z = 0 to HeadStart, radial normals)
        var shaftBase = 0;
        for (var k = 0; k < segments; k++)
        {
            AddVertex(ShaftRadius * cos[k], ShaftRadius * sin[k], 0.0f, cos[k], sin[k], 0.0f);
            AddVertex(ShaftRadius * cos[k], ShaftRadius * sin[k], HeadStart, cos[k], sin[k], 0.0f);
        }

        for (var k = 0; k < segments; k++)
        {
            var next = (k + 1) % segments;
            var b0 = shaftBase + k * 2;
            var t0 = b0 + 1;
            var b1 = shaftBase + next * 2;
            var t1 = b1 + 1;
            indices.AddRange([b0, b1, t0, t0, b1, t1]);
        }

        // 2. Bottom cap (z = 0, normal -Z)
        var capCenter = vertices.Count / 6;
        AddVertex(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f);
        var capRing = vertices.Count / 6;
        for (var k = 0; k < segments; k++)
        {
            AddVertex(ShaftRadius * cos[k], ShaftRadius * sin[k], 0.0f, 0.0f, 0.0f, -1.0f);
        }

        for (var k = 0; k < segments; k++)
        {
            indices.AddRange([capCenter, capRing + k, capRing + (k + 1) % segments]);
        }

        // 3. Cone base disc (z = HeadStart, normal -Z)
        var discCenter = vertices.Count / 6;
        AddVertex(0.0f, 0.0f, HeadStart, 0.0f, 0.0f, -1.0f);
        var discRing = vertices.Count / 6;
        for (var k = 0; k < segments; k++)
        {
            AddVertex(HeadRadius * cos[k], HeadRadius * sin[k], HeadStart, 0.0f, 0.0f, -1.0f);
        }

        for (var k = 0; k < segments; k++)
        {
            indices.AddRange([discCenter, discRing + k, discRing + (k + 1) % segments]);
        }

        // 4. Cone side (z = HeadStart, r = HeadRadius -> tip z = 1)
        //    Slope normal = normalize((L * cos(theta), L * sin(theta), r)), where L = cone length
        var coneLength = 1.0f - HeadStart;
        var coneRing = vertices.Count / 6;
        for (var k = 0; k < segments; k++)
        {
            var n = Vector3.Normalize(new Vector3(coneLength * cos[k], coneLength * sin[k], HeadRadius));
            AddVertex(HeadRadius * cos[k], HeadRadius * sin[k], HeadStart, n.X, n.Y, n.Z);
        }

        var coneTip = vertices.Count / 6;
        for (var k = 0; k < segments; k++)
        {
            // Duplicate tip vertex per segment with mid-face normal for clean low-poly shading
            var midCos = (float)Math.Cos(2.0 * Math.PI * (k + 0.5) / segments);
            var midSin = (float)Math.Sin(2.0 * Math.PI * (k + 0.5) / segments);
            var n = Vector3.Normalize(new Vector3(coneLength * midCos, coneLength * midSin, HeadRadius));
            AddVertex(0.0f, 0.0f, 1.0f, n.X, n.Y, n.Z);
        }

        for (var k = 0; k < segments; k++)
        {
            indices.AddRange([coneRing + k, coneRing + (k + 1) % segments, coneTip + k]);
        }

        return ([.. vertices], [.. indices]);
    }

    /// <summary>
    /// Builds an orthonormal basis (u, v) from unit direction vector <paramref name="w"/> (matches HLSL VSMain),
    /// mapping prototype (x, y, z) axes onto (u, v, w).
    /// </summary>
    public static (Vector3 U, Vector3 V) ComputeBasis(Vector3 w)
    {
        var helper = Math.Abs(w.Z) < 0.9f ? Vector3.UnitZ : Vector3.UnitX;
        var u = Vector3.Normalize(Vector3.Cross(helper, w));
        var v = Vector3.Cross(w, u);
        return (u, v);
    }

    /// <summary>
    /// Builds glyph instance data (<see cref="FloatsPerInstance"/> floats per instance) from a mesh's nodal vector field.
    /// Samples every <paramref name="stride"/>-th node, skipping zero and non-finite vectors.
    /// Positions are recentered at the scene origin and displacements are packed so the shader tracks deformation.
    /// Color is mapped from |v| via <paramref name="colorScale"/> (or <paramref name="fallbackColor"/> when null).
    /// </summary>
    public static float[] BuildInstances(
        ViewportMesh mesh, int stride,
        double originX, double originY, double originZ,
        ColorScale? colorScale, Vector4 fallbackColor, out int instanceCount)
    {
        instanceCount = 0;
        var vectors = mesh.VectorValues;
        var positions = mesh.Positions;
        if (vectors is null || vectors.Length < 3 || positions.Length < 3)
        {
            return [];
        }

        stride = Math.Max(stride, 1);
        var nodeCount = Math.Min(positions.Length, vectors.Length) / 3;
        var data = new List<float>(nodeCount / stride * FloatsPerInstance + FloatsPerInstance);

        for (var node = 0; node < nodeCount; node += stride)
        {
            var vx = vectors[node * 3];
            var vy = vectors[node * 3 + 1];
            var vz = vectors[node * 3 + 2];
            var magnitude = Math.Sqrt(vx * vx + vy * vy + vz * vz);
            if (!double.IsFinite(magnitude) || magnitude <= 0.0)
            {
                continue; // Skip zero or NaN vectors to avoid degenerate rotations
            }

            var color = fallbackColor;
            if (colorScale is not null)
            {
                // Delegate log scale, out-of-range, and discrete level handling to ColorScale
                var sampled = colorScale.GetColor(magnitude);
                color = new Vector4(sampled.R / 255.0f, sampled.G / 255.0f, sampled.B / 255.0f, sampled.A / 255.0f);
            }

            // Base position (recentered local coordinates)
            data.Add((float)(positions[node * 3] - originX));
            data.Add((float)(positions[node * 3 + 1] - originY));
            data.Add((float)(positions[node * 3 + 2] - originZ));

            // Displacement (for deformation tracking; zero if absent)
            var displacements = mesh.Displacements;
            if (displacements is not null && node * 3 + 2 < displacements.Length)
            {
                data.Add((float)displacements[node * 3]);
                data.Add((float)displacements[node * 3 + 1]);
                data.Add((float)displacements[node * 3 + 2]);
            }
            else
            {
                data.Add(0.0f);
                data.Add(0.0f);
                data.Add(0.0f);
            }

            // Unit direction + magnitude (shader computes length as |v| * GlyphScale)
            data.Add((float)(vx / magnitude));
            data.Add((float)(vy / magnitude));
            data.Add((float)(vz / magnitude));
            data.Add((float)magnitude);

            data.Add(color.X);
            data.Add(color.Y);
            data.Add(color.Z);
            data.Add(color.W);
            instanceCount++;
        }

        return [.. data];
    }

    /// <summary>
    /// Builds glyph instances from a vector field at active voxel cell centers,
    /// sampling every <paramref name="stride"/>-th active slot (LOD-adjusted stride is computed by the caller).
    /// </summary>
    public static float[] BuildVoxelInstances(
        ViewportVoxelGrid grid, int stride,
        double originX, double originY, double originZ,
        ColorScale? colorScale, Vector4 fallbackColor, out int instanceCount)
    {
        instanceCount = 0;
        ArgumentNullException.ThrowIfNull(grid);
        var vectors = grid.VectorValues;
        var active = grid.ActiveCellIndices;
        if (vectors is null || vectors.Length < 3 || active.Length == 0)
        {
            return [];
        }

        stride = Math.Max(stride, 1);
        var data = new List<float>(active.Length / stride * FloatsPerInstance + FloatsPerInstance);

        for (var slot = 0; slot < active.Length; slot += stride)
        {
            if (slot * 3 + 2 >= vectors.Length)
            {
                break;
            }

            if (!grid.TryDecodeIndex(active[slot], out var i, out var j, out var k))
            {
                continue;
            }

            var vx = vectors[slot * 3];
            var vy = vectors[slot * 3 + 1];
            var vz = vectors[slot * 3 + 2];
            var magnitude = Math.Sqrt(vx * vx + vy * vy + vz * vz);
            if (!double.IsFinite(magnitude) || magnitude <= 0.0)
            {
                continue;
            }

            var color = fallbackColor;
            if (colorScale is not null)
            {
                var sampled = colorScale.GetColor(magnitude);
                color = new Vector4(sampled.R / 255.0f, sampled.G / 255.0f, sampled.B / 255.0f, sampled.A / 255.0f);
            }

            grid.GetCellCenter(i, j, k, out var cx, out var cy, out var cz);
            data.Add((float)(cx - originX));
            data.Add((float)(cy - originY));
            data.Add((float)(cz - originZ));

            // Voxel grids do not carry displacements (deformation applies only to triangle meshes)
            data.Add(0.0f);
            data.Add(0.0f);
            data.Add(0.0f);

            data.Add((float)(vx / magnitude));
            data.Add((float)(vy / magnitude));
            data.Add((float)(vz / magnitude));
            data.Add((float)magnitude);

            data.Add(color.X);
            data.Add(color.Y);
            data.Add(color.Z);
            data.Add(color.W);
            instanceCount++;
        }

        return [.. data];
    }
}
