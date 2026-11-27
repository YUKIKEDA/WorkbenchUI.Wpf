using System.Numerics;

namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// CPU-side picking math, independent of the GPU for unit testing.
/// The GPU ID pass identifies the hit triangle, and node picking projects its 3 vertices
/// to screen space to find the node closest to the cursor (avoiding a separate vertex ID pass).
/// </summary>
internal static class ViewportPicking
{
    /// <summary>
    /// Projects a recentered local-space point into viewport pixel coordinates.
    /// Returns null if the point lies behind the camera (w &lt;= 0).
    /// </summary>
    public static Vector2? ProjectToPixel(
        Vector3 localPosition, in Matrix4x4 viewProj, double pixelWidth, double pixelHeight)
    {
        var clip = Vector4.Transform(new Vector4(localPosition, 1.0f), viewProj);
        if (clip.W <= 1e-9f)
        {
            return null;
        }

        var ndcX = clip.X / clip.W;
        var ndcY = clip.Y / clip.W;
        return new Vector2(
            (float)((ndcX + 1.0) * 0.5 * pixelWidth),
            (float)((1.0 - ndcY) * 0.5 * pixelHeight)); // Screen Y points downward
    }

    /// <summary>
    /// Returns the node index among the picked triangle's 3 vertices closest in screen distance to the cursor.
    /// </summary>
    /// <param name="mesh">Target mesh (node coordinates are in world/model space).</param>
    /// <param name="triangleIndex">Triangle index (in units of TriangleIndices / 3).</param>
    /// <param name="originX">Scene recentering origin X.</param>
    /// <param name="originY">Scene recentering origin Y.</param>
    /// <param name="originZ">Scene recentering origin Z.</param>
    /// <param name="viewProj">Combined view-projection matrix.</param>
    /// <param name="pixelWidth">Viewport width in pixels.</param>
    /// <param name="pixelHeight">Viewport height in pixels.</param>
    /// <param name="cursorPixel">Cursor position in viewport pixel coordinates.</param>
    /// <param name="deformationScale">Active deformation scale applied to displacements.</param>
    public static int? FindNearestNodeOnTriangle(
        ViewportMesh mesh, int triangleIndex,
        double originX, double originY, double originZ,
        in Matrix4x4 viewProj, double pixelWidth, double pixelHeight,
        Vector2 cursorPixel, double deformationScale = 0.0)
    {
        var triangles = mesh.TriangleIndices;
        var positions = mesh.Positions;
        var baseIndex = triangleIndex * 3;
        if (baseIndex < 0 || baseIndex + 2 >= triangles.Length)
        {
            return null;
        }

        int? best = null;
        var bestDistance = double.MaxValue;
        for (var corner = 0; corner < 3; corner++)
        {
            var node = triangles[baseIndex + corner];
            if (node < 0 || node * 3 + 2 >= positions.Length)
            {
                continue;
            }

            var local = GetLocalPosition(
                positions, mesh.Displacements, deformationScale, node, originX, originY, originZ);
            var pixel = ProjectToPixel(local, in viewProj, pixelWidth, pixelHeight);
            if (pixel is null)
            {
                continue;
            }

            var distance = (pixel.Value - cursorPixel).LengthSquared();
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = node;
            }
        }

        return best;
    }

    /// <summary>
    /// Collects nodes from hit triangles whose screen projections fall inside the selection rectangle
    /// (filtering via visible triangles prevents occluded back-side nodes from being included).
    /// </summary>
    public static HashSet<int> FindNodesInRectangle(
        ViewportMesh mesh, IReadOnlyCollection<int> hitTriangles,
        double originX, double originY, double originZ,
        in Matrix4x4 viewProj, double pixelWidth, double pixelHeight,
        Vector2 rectMin, Vector2 rectMax, double deformationScale = 0.0)
    {
        var result = new HashSet<int>();
        var triangles = mesh.TriangleIndices;
        var positions = mesh.Positions;

        var candidates = new HashSet<int>();
        foreach (var tri in hitTriangles)
        {
            var baseIndex = tri * 3;
            if (baseIndex < 0 || baseIndex + 2 >= triangles.Length)
            {
                continue;
            }

            candidates.Add(triangles[baseIndex]);
            candidates.Add(triangles[baseIndex + 1]);
            candidates.Add(triangles[baseIndex + 2]);
        }

        foreach (var node in candidates)
        {
            if (node < 0 || node * 3 + 2 >= positions.Length)
            {
                continue;
            }

            var local = GetLocalPosition(
                positions, mesh.Displacements, deformationScale, node, originX, originY, originZ);
            var pixel = ProjectToPixel(local, in viewProj, pixelWidth, pixelHeight);
            if (pixel is null)
            {
                continue;
            }

            if (pixel.Value.X >= rectMin.X && pixel.Value.X <= rectMax.X
                && pixel.Value.Y >= rectMin.Y && pixel.Value.Y <= rectMax.Y)
            {
                result.Add(node);
            }
        }

        return result;
    }

    /// <summary>
    /// Computes the recentered and deformed local-space position of a node, matching the GPU vertex shader
    /// (pos + disp * scale) so node picking, probing, and annotation anchors align with rendered geometry.
    /// </summary>
    internal static Vector3 GetLocalPosition(
        double[] positions, double[]? displacements, double deformationScale,
        int node, double originX, double originY, double originZ)
    {
        double dx = 0.0, dy = 0.0, dz = 0.0;
        if (deformationScale != 0.0 && displacements is not null && node * 3 + 2 < displacements.Length)
        {
            dx = displacements[node * 3] * deformationScale;
            dy = displacements[node * 3 + 1] * deformationScale;
            dz = displacements[node * 3 + 2] * deformationScale;
        }

        return new(
            (float)(positions[node * 3] - originX + dx),
            (float)(positions[node * 3 + 1] - originY + dy),
            (float)(positions[node * 3 + 2] - originZ + dz));
    }
}
