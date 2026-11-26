using System.Numerics;

namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Pure helper functions for section plane clipping calculations.
/// </summary>
internal static class ViewportSection
{
    /// <summary>Shader clip coefficients indicating clipping is disabled (signed distance is always +1).</summary>
    public static readonly Vector4 DisabledClip = new(0.0f, 0.0f, 0.0f, 1.0f);

    /// <summary>
    /// Converts a <see cref="SectionPlane"/> into shader coefficients (nx, ny, nz, d).
    /// For a recentered local coordinate p, signed distance = dot(p, xyz) + w (positive = visible, negative = clipped).
    /// The normal is normalized, and the plane origin is shifted to local space by subtracting the scene origin
    /// (<paramref name="sceneOriginX"/>, etc.) in double precision before converting to float.
    /// Returns null if <paramref name="plane"/> is null or has a zero-length normal (clipping disabled).
    /// </summary>
    public static Vector4? ComputeClipCoefficients(
        SectionPlane? plane, double sceneOriginX, double sceneOriginY, double sceneOriginZ)
    {
        if (plane is null)
        {
            return null;
        }

        var nx = plane.NormalX;
        var ny = plane.NormalY;
        var nz = plane.NormalZ;
        var length = Math.Sqrt(nx * nx + ny * ny + nz * nz);
        if (length < 1e-300 || !double.IsFinite(length))
        {
            return null;
        }

        nx /= length;
        ny /= length;
        nz /= length;

        // With localized plane point o_l, d = -dot(n, o_l)
        var ox = plane.OriginX - sceneOriginX;
        var oy = plane.OriginY - sceneOriginY;
        var oz = plane.OriginZ - sceneOriginZ;
        var d = -(nx * ox + ny * oy + nz * oz);

        return new Vector4((float)nx, (float)ny, (float)nz, (float)d);
    }

    /// <summary>
    /// Returns whether a local-space point lies on the clipped side of the section plane.
    /// Uses the same expression as GPU SV_ClipDistance (dot(p, xyz) + w &lt; 0), ensuring
    /// automatic annotation hiding matches visual clipping. Always false for <see cref="DisabledClip"/>.
    /// </summary>
    public static bool IsClipped(Vector3 localPoint, Vector4 clipCoefficients) =>
        localPoint.X * clipCoefficients.X
        + localPoint.Y * clipCoefficients.Y
        + localPoint.Z * clipCoefficients.Z
        + clipCoefficients.W < 0.0f;

    /// <summary>
    /// Builds vertices for the section plane indicator (semi-transparent quad plus outline).
    /// <para>
    /// Returns 14 vertices x 6 floats (position plus zero-padded displacement to satisfy the
    /// two-slot line shader input layout from a single buffer). The first 6 vertices form the
    /// quad triangle list, followed by 8 vertices for the outline line list. The quad is centered
    /// at the local origin projected onto the plane with half-extent <paramref name="sceneRadius"/> * 1.15.
    /// </para>
    /// </summary>
    public static float[] BuildIndicatorVertices(Vector4 clipCoefficients, float sceneRadius)
    {
        var normal = new Vector3(clipCoefficients.X, clipCoefficients.Y, clipCoefficients.Z);
        if (normal.LengthSquared() < 1e-12f)
        {
            return [];
        }

        normal = Vector3.Normalize(normal);

        // Projection of local origin onto the plane = -d * n_hat
        var center = -clipCoefficients.W * normal;
        var reference = Math.Abs(normal.Z) > 0.9f ? Vector3.UnitY : Vector3.UnitZ;
        var u = Vector3.Normalize(Vector3.Cross(reference, normal));
        var v = Vector3.Cross(normal, u);
        var half = Math.Max(sceneRadius, 1e-3f) * 1.15f;

        var p00 = center - u * half - v * half;
        var p10 = center + u * half - v * half;
        var p11 = center + u * half + v * half;
        var p01 = center - u * half + v * half;

        // Quad (2 triangles) + outline (4 line segments)
        Vector3[] points =
        [
            p00, p10, p11, p00, p11, p01,
            p00, p10, p10, p11, p11, p01, p01, p00,
        ];

        var result = new float[points.Length * 6];
        for (var i = 0; i < points.Length; i++)
        {
            result[i * 6] = points[i].X;
            result[i * 6 + 1] = points[i].Y;
            result[i * 6 + 2] = points[i].Z;
            // Displacement (TEXCOORD1) stays zero
        }

        return result;
    }
}
