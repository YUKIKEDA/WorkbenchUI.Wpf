namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Pure helper functions for collecting active voxel cells along a slice plane.
/// </summary>
internal static class ViewportVoxelSlice
{
    /// <summary>
    /// Collects active cell slot indices whose centers lie within <paramref name="halfThickness"/>
    /// of the plane |ax + by + cz + d| &lt;= halfThickness, specified by a point and normal (matching <see cref="SectionPlane"/>).
    /// </summary>
    public static int[] CollectActiveSlotsNearPlane(
        ViewportVoxelGrid grid,
        double pointX, double pointY, double pointZ,
        double normalX, double normalY, double normalZ,
        double halfThickness)
    {
        ArgumentNullException.ThrowIfNull(grid);

        var len = Math.Sqrt(normalX * normalX + normalY * normalY + normalZ * normalZ);
        if (len < 1e-15 || halfThickness < 0.0)
        {
            return [];
        }

        var nx = normalX / len;
        var ny = normalY / len;
        var nz = normalZ / len;
        // plane: n·(x - p) = 0 → n·x + d = 0 with d = -n·p
        var d = -(nx * pointX + ny * pointY + nz * pointZ);

        var active = grid.ActiveCellIndices;
        var hits = new List<int>();
        for (var slot = 0; slot < active.Length; slot++)
        {
            if (!grid.TryDecodeIndex(active[slot], out var i, out var j, out var k))
            {
                continue;
            }

            grid.GetCellCenter(i, j, k, out var cx, out var cy, out var cz);
            var dist = Math.Abs(nx * cx + ny * cy + nz * cz + d);
            if (dist <= halfThickness)
            {
                hits.Add(slot);
            }
        }

        return hits.ToArray();
    }

    /// <summary>
    /// Collects active cell slot indices near <paramref name="plane"/> using the default band half-thickness (half a cell diagonal).
    /// </summary>
    public static int[] CollectActiveSlotsNearPlane(ViewportVoxelGrid grid, SectionPlane plane)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(plane);

        var diag = 0.5 * Math.Sqrt(
            grid.CellSizeX * grid.CellSizeX
            + grid.CellSizeY * grid.CellSizeY
            + grid.CellSizeZ * grid.CellSizeZ);

        return CollectActiveSlotsNearPlane(
            grid,
            plane.OriginX, plane.OriginY, plane.OriginZ,
            plane.NormalX, plane.NormalY, plane.NormalZ,
            diag);
    }
}
