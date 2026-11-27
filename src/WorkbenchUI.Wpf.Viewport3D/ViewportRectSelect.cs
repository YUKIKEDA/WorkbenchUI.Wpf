using System.Numerics;

namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Pure CPU-side functions for through-selection rectangle queries, independent of the GPU for unit testing.
/// <para>
/// Unlike visible-only rectangle selection (GPU ID readback), this tests occluded geometry as well
/// via screen-space projection: a node is selected if its projected point lies inside the rectangle;
/// a face is selected if all 3 vertices lie strictly inside; a part is selected if at least one of its
/// triangles meets the face condition. Evaluates deformed positions (matching the GPU vertex shader) and
/// excludes clipped nodes or triangles whose all 3 vertices are clipped by the section plane
/// (partially clipped triangles remain selectable because they are still visible on screen).
/// </para>
/// </summary>
internal static class ViewportRectSelect
{
    /// <summary>
    /// Returns whether a node lies inside the selection rectangle.
    /// Returns false if unprojectable (behind camera) or clipped by the section plane.
    /// </summary>
    public static bool IsNodeInRect(
        double[] positions, double[]? displacements, double deformationScale, int node,
        double originX, double originY, double originZ,
        in Matrix4x4 viewProj, double pixelWidth, double pixelHeight,
        Vector2 rectMin, Vector2 rectMax, Vector4 clipPlane)
    {
        if (node < 0 || node * 3 + 2 >= positions.Length)
        {
            return false;
        }

        var local = ViewportPicking.GetLocalPosition(
            positions, displacements, deformationScale, node, originX, originY, originZ);
        if (ViewportSection.IsClipped(local, clipPlane))
        {
            return false;
        }

        return ViewportPicking.ProjectToPixel(local, in viewProj, pixelWidth, pixelHeight) is { } pixel
            && pixel.X >= rectMin.X && pixel.X <= rectMax.X
            && pixel.Y >= rectMin.Y && pixel.Y <= rectMax.Y;
    }

    /// <summary>
    /// Returns whether a triangle lies strictly inside the selection rectangle (all 3 vertices inside).
    /// Triangles with all 3 vertices clipped by the section plane are excluded (partially clipped triangles remain eligible).
    /// </summary>
    public static bool IsTriangleInRect(
        double[] positions, int[] triangleIndices, double[]? displacements, double deformationScale,
        int triangleIndex,
        double originX, double originY, double originZ,
        in Matrix4x4 viewProj, double pixelWidth, double pixelHeight,
        Vector2 rectMin, Vector2 rectMax, Vector4 clipPlane)
    {
        var baseIndex = triangleIndex * 3;
        if (baseIndex < 0 || baseIndex + 2 >= triangleIndices.Length)
        {
            return false;
        }

        var clippedCorners = 0;
        for (var corner = 0; corner < 3; corner++)
        {
            var node = triangleIndices[baseIndex + corner];
            if (node < 0 || node * 3 + 2 >= positions.Length)
            {
                return false;
            }

            var local = ViewportPicking.GetLocalPosition(
                positions, displacements, deformationScale, node, originX, originY, originZ);
            if (ViewportSection.IsClipped(local, clipPlane))
            {
                clippedCorners++;
            }

            if (ViewportPicking.ProjectToPixel(local, in viewProj, pixelWidth, pixelHeight) is not { } pixel
                || pixel.X < rectMin.X || pixel.X > rectMax.X
                || pixel.Y < rectMin.Y || pixel.Y > rectMax.Y)
            {
                return false;
            }
        }

        return clippedCorners < 3;
    }

    /// <summary>
    /// Coarse chunk AABB screen-bounds check: returns whether the projected bounding box may overlap the rectangle.
    /// Conservatively returns true if any corner lies behind the camera (exact check is deferred to per-vertex tests).
    /// <paramref name="expand"/> accounts for deformation margin (max displacement * |scale|).
    /// </summary>
    public static bool ScreenBoundsMayOverlapRect(
        Vector3 boundsMin, Vector3 boundsMax, float expand,
        in Matrix4x4 viewProj, double pixelWidth, double pixelHeight,
        Vector2 rectMin, Vector2 rectMax)
    {
        var min = boundsMin - new Vector3(expand);
        var max = boundsMax + new Vector3(expand);

        var screenMin = new Vector2(float.PositiveInfinity);
        var screenMax = new Vector2(float.NegativeInfinity);
        for (var i = 0; i < 8; i++)
        {
            var corner = new Vector3(
                (i & 1) == 0 ? min.X : max.X,
                (i & 2) == 0 ? min.Y : max.Y,
                (i & 4) == 0 ? min.Z : max.Z);
            if (ViewportPicking.ProjectToPixel(corner, in viewProj, pixelWidth, pixelHeight) is not { } pixel)
            {
                return true; // Screen bounds are undefined when an AABB straddles behind the camera; pass conservatively
            }

            screenMin = Vector2.Min(screenMin, pixel);
            screenMax = Vector2.Max(screenMax, pixel);
        }

        return screenMin.X <= rectMax.X && screenMax.X >= rectMin.X
            && screenMin.Y <= rectMax.Y && screenMax.Y >= rectMin.Y;
    }

    /// <summary>
    /// Evaluates triangles in the given range sequentially for a single chunk and appends hits to <paramref name="results"/>.
    /// Callers parallelize across chunks.
    /// </summary>
    public static void CollectTrianglesInRect(
        double[] positions, int[] triangleIndices, double[]? displacements, double deformationScale,
        double originX, double originY, double originZ,
        in Matrix4x4 viewProj, double pixelWidth, double pixelHeight,
        Vector2 rectMin, Vector2 rectMax, Vector4 clipPlane,
        int triangleStart, int triangleCount, ICollection<int> results)
    {
        for (var t = triangleStart; t < triangleStart + triangleCount; t++)
        {
            if (IsTriangleInRect(
                positions, triangleIndices, displacements, deformationScale, t,
                originX, originY, originZ, in viewProj, pixelWidth, pixelHeight,
                rectMin, rectMax, clipPlane))
            {
                results.Add(t);
            }
        }
    }
}
