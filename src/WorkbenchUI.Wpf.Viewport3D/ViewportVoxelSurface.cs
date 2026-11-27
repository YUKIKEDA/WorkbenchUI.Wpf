namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Triangulates exposed outer faces of active cells in a sparse voxel grid.
/// </summary>
internal static class ViewportVoxelSurface
{
    /// <summary>Extracted surface mesh data and triangle-to-cell mapping arrays.</summary>
    internal sealed record Result(
        double[] Positions,
        int[] TriangleIndices,
        double[]? ScalarValues,
        int[] TriangleToActiveSlot,
        int[] TriangleToLinearIndex);

    /// <summary>
    /// Extracts the exposed outer surface mesh from active cells in <paramref name="grid"/>.
    /// When <paramref name="visibleRegionIds"/> is non-null, only cells whose region ID is included are rendered.
    /// </summary>
    public static Result Extract(ViewportVoxelGrid grid, IReadOnlySet<int>? visibleRegionIds = null)
    {
        ArgumentNullException.ThrowIfNull(grid);

        var active = grid.ActiveCellIndices;
        var nx = grid.ResolutionX;
        var ny = grid.ResolutionY;
        var nz = grid.ResolutionZ;
        var regionIds = grid.RegionIds;
        var scalars = grid.ScalarValues;

        var activeSet = new HashSet<int>(active.Length);
        for (var a = 0; a < active.Length; a++)
        {
            if (visibleRegionIds is not null)
            {
                var region = regionIds is not null && a < regionIds.Length ? regionIds[a] : 0;
                if (!visibleRegionIds.Contains(region))
                {
                    continue;
                }
            }

            activeSet.Add(active[a]);
        }

        // Linear index -> active slot (re-scan so original active array slot indices are preserved after filtering)
        var linearToSlot = new Dictionary<int, int>(active.Length);
        for (var a = 0; a < active.Length; a++)
        {
            if (activeSet.Contains(active[a]))
            {
                linearToSlot[active[a]] = a;
            }
        }

        var estimatedFaces = Math.Min(activeSet.Count, 100_000) * 2;
        var positions = new List<double>(estimatedFaces * 4 * 3);
        var triangles = new List<int>(estimatedFaces * 2 * 3);
        var triSlot = new List<int>(estimatedFaces * 2);
        var triLinear = new List<int>(estimatedFaces * 2);
        List<double>? scalarOut = scalars is null ? null : new List<double>(estimatedFaces * 4);

        foreach (var linear in activeSet)
        {
            if (!ViewportVoxelIndexing.TryDecode(linear, nx, ny, nz, out var i, out var j, out var k))
            {
                continue;
            }

            var slot = linearToSlot[linear];
            double? scalar = null;
            if (scalars is not null && slot < scalars.Length)
            {
                scalar = scalars[slot];
            }

            // 6 neighbors: (-x, +x, -y, +y, -z, +z)
            TryEmitFace(i - 1, j, k, nx, ny, nz, activeSet, 0, i, j, k, grid, slot, linear, scalar,
                positions, triangles, triSlot, triLinear, scalarOut);
            TryEmitFace(i + 1, j, k, nx, ny, nz, activeSet, 1, i, j, k, grid, slot, linear, scalar,
                positions, triangles, triSlot, triLinear, scalarOut);
            TryEmitFace(i, j - 1, k, nx, ny, nz, activeSet, 2, i, j, k, grid, slot, linear, scalar,
                positions, triangles, triSlot, triLinear, scalarOut);
            TryEmitFace(i, j + 1, k, nx, ny, nz, activeSet, 3, i, j, k, grid, slot, linear, scalar,
                positions, triangles, triSlot, triLinear, scalarOut);
            TryEmitFace(i, j, k - 1, nx, ny, nz, activeSet, 4, i, j, k, grid, slot, linear, scalar,
                positions, triangles, triSlot, triLinear, scalarOut);
            TryEmitFace(i, j, k + 1, nx, ny, nz, activeSet, 5, i, j, k, grid, slot, linear, scalar,
                positions, triangles, triSlot, triLinear, scalarOut);
        }

        return new Result(
            positions.ToArray(),
            triangles.ToArray(),
            scalarOut?.ToArray(),
            triSlot.ToArray(),
            triLinear.ToArray());
    }

    /// <summary>Converts an extracted surface result into a <see cref="ViewportMesh"/> for the existing render pipeline.</summary>
    public static ViewportMesh ToMesh(ViewportVoxelGrid grid, Result surface)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(surface);

        return new ViewportMesh
        {
            Name = grid.Name,
            Positions = surface.Positions,
            TriangleIndices = surface.TriangleIndices,
            ScalarValues = surface.ScalarValues,
            Color = grid.Color,
            IsVisible = grid.IsVisible,
            ShowEdges = grid.ShowEdges,
            IsClippable = grid.IsClippable,
        };
    }

    /// <summary>
    /// Expands cell-centered scalars onto outer-surface vertices for topology-preserving field updates.
    /// Writes the active slot's scalar value to all 3 vertices of each emitted triangle.
    /// </summary>
    public static double[] BuildVertexScalars(Result surface, double[]? cellScalars)
    {
        ArgumentNullException.ThrowIfNull(surface);

        var vertexCount = surface.Positions.Length / 3;
        var result = new double[vertexCount];
        if (cellScalars is null || surface.TriangleToActiveSlot.Length == 0)
        {
            return result;
        }

        var tris = surface.TriangleIndices;
        var slots = surface.TriangleToActiveSlot;
        for (var t = 0; t < slots.Length; t++)
        {
            var slot = slots[t];
            var s = slot >= 0 && slot < cellScalars.Length ? cellScalars[slot] : 0.0;
            var baseIndex = t * 3;
            result[tris[baseIndex]] = s;
            result[tris[baseIndex + 1]] = s;
            result[tris[baseIndex + 2]] = s;
        }

        return result;
    }

    public static ViewportMesh ToMesh(ViewportVoxelGrid grid, IReadOnlySet<int>? visibleRegionIds = null)
        => ToMesh(grid, Extract(grid, visibleRegionIds));

    private static void TryEmitFace(
        int ni, int nj, int nk,
        int nx, int ny, int nz,
        HashSet<int> activeSet,
        int face,
        int i, int j, int k,
        ViewportVoxelGrid grid,
        int slot,
        int linear,
        double? scalar,
        List<double> positions,
        List<int> triangles,
        List<int> triSlot,
        List<int> triLinear,
        List<double>? scalarOut)
    {
        var covered = ni >= 0 && nj >= 0 && nk >= 0
                      && ni < nx && nj < ny && nk < nz
                      && activeSet.Contains(ViewportVoxelIndexing.Encode(ni, nj, nk, nx, ny));
        if (covered)
        {
            return;
        }

        grid.GetCellMinCorner(i, j, k, out var x0, out var y0, out var z0);
        var x1 = x0 + grid.CellSizeX;
        var y1 = y0 + grid.CellSizeY;
        var z1 = z0 + grid.CellSizeZ;

        // 4 vertices per face in right-handed outward winding order
        double[] corners = face switch
        {
            0 => [x0, y0, z0, x0, y1, z0, x0, y1, z1, x0, y0, z1], // -X
            1 => [x1, y0, z0, x1, y0, z1, x1, y1, z1, x1, y1, z0], // +X
            2 => [x0, y0, z0, x0, y0, z1, x1, y0, z1, x1, y0, z0], // -Y
            3 => [x0, y1, z0, x1, y1, z0, x1, y1, z1, x0, y1, z1], // +Y
            4 => [x0, y0, z0, x1, y0, z0, x1, y1, z0, x0, y1, z0], // -Z
            _ => [x0, y0, z1, x0, y1, z1, x1, y1, z1, x1, y0, z1], // +Z
        };

        var baseIndex = positions.Count / 3;
        positions.AddRange(corners);
        if (scalarOut is not null)
        {
            var s = scalar ?? 0.0;
            scalarOut.Add(s);
            scalarOut.Add(s);
            scalarOut.Add(s);
            scalarOut.Add(s);
        }

        // 2 triangles
        triangles.Add(baseIndex);
        triangles.Add(baseIndex + 1);
        triangles.Add(baseIndex + 2);
        triangles.Add(baseIndex);
        triangles.Add(baseIndex + 2);
        triangles.Add(baseIndex + 3);
        triSlot.Add(slot);
        triSlot.Add(slot);
        triLinear.Add(linear);
        triLinear.Add(linear);
    }
}
