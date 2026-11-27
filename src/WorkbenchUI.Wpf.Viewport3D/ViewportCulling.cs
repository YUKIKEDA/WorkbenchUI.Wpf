using System.Numerics;

namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Pure frustum culling math for chunked bounding boxes.
/// </summary>
internal static class ViewportCulling
{
    /// <summary>Number of frustum planes (left/right/bottom/top/near/far).</summary>
    public const int PlaneCount = 6;

    /// <summary>
    /// Extracts the 6 frustum planes from a view-projection matrix. Each plane is (a, b, c, d)
    /// where interior points satisfy a·x + b·y + c·z + d >= 0 under Direct3D clip space (z in [0, 1]).
    /// </summary>
    public static void ExtractFrustumPlanes(in Matrix4x4 viewProj, Span<Vector4> planes)
    {
        if (planes.Length < PlaneCount)
        {
            throw new ArgumentException($"Plane buffer requires at least {PlaneCount} elements.", nameof(planes));
        }

        // Under row-vector convention (clip = v * M), clip.x = dot(v, col0), so extract matrix columns.
        var col0 = new Vector4(viewProj.M11, viewProj.M21, viewProj.M31, viewProj.M41);
        var col1 = new Vector4(viewProj.M12, viewProj.M22, viewProj.M32, viewProj.M42);
        var col2 = new Vector4(viewProj.M13, viewProj.M23, viewProj.M33, viewProj.M43);
        var col3 = new Vector4(viewProj.M14, viewProj.M24, viewProj.M34, viewProj.M44);

        planes[0] = Normalize(col3 + col0); // left   (x >= -w)
        planes[1] = Normalize(col3 - col0); // right  (x <= +w)
        planes[2] = Normalize(col3 + col1); // bottom (y >= -w)
        planes[3] = Normalize(col3 - col1); // top    (y <= +w)
        planes[4] = Normalize(col2);        // near   (z >= 0)
        planes[5] = Normalize(col3 - col2); // far    (z <= w)
    }

    /// <summary>
    /// Returns whether an AABB (expanded on all sides by <paramref name="expand"/>) may intersect the frustum.
    /// False guarantees the box is completely outside (safe to skip); true is conservative.
    /// </summary>
    public static bool IntersectsFrustum(
        ReadOnlySpan<Vector4> planes, Vector3 min, Vector3 max, float expand = 0.0f)
    {
        for (var i = 0; i < PlaneCount; i++)
        {
            if (IsOutsidePlane(planes[i], min, max, expand))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns whether an AABB lies completely on the negative (outer) side of a plane using the positive-vertex test:
    /// if the corner furthest along the plane normal is on the negative side, the entire box is outside.
    /// </summary>
    public static bool IsOutsidePlane(in Vector4 plane, Vector3 min, Vector3 max, float expand = 0.0f)
    {
        var px = plane.X >= 0.0f ? max.X : min.X;
        var py = plane.Y >= 0.0f ? max.Y : min.Y;
        var pz = plane.Z >= 0.0f ? max.Z : min.Z;
        return plane.X * px + plane.Y * py + plane.Z * pz + plane.W + expand < 0.0f;
    }

    private static Vector4 Normalize(Vector4 plane)
    {
        var len = MathF.Sqrt(plane.X * plane.X + plane.Y * plane.Y + plane.Z * plane.Z);
        return len > 1e-20f ? plane / len : plane;
    }
}
