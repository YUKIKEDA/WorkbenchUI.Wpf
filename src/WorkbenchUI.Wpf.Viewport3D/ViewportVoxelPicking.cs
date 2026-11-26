namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Ray intersection and cell probing algorithms for sparse voxel grids.
/// </summary>
internal static class ViewportVoxelPicking
{
    /// <summary>
    /// Recovers voxel cell information from an outer-surface triangle index
    /// (where <paramref name="triangleIndex"/> is in units of <see cref="ViewportVoxelSurface.Result.TriangleIndices"/> / 3).
    /// </summary>
    public static bool TryMapTriangle(
        ViewportVoxelGrid grid,
        ViewportVoxelSurface.Result surface,
        int triangleIndex,
        out int activeSlot,
        out int linearIndex,
        out int i, out int j, out int k)
    {
        activeSlot = linearIndex = i = j = k = 0;
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(surface);

        if (triangleIndex < 0 || triangleIndex >= surface.TriangleToLinearIndex.Length)
        {
            return false;
        }

        activeSlot = surface.TriangleToActiveSlot[triangleIndex];
        linearIndex = surface.TriangleToLinearIndex[triangleIndex];
        return grid.TryDecodeIndex(linearIndex, out i, out j, out k);
    }

    /// <summary>
    /// Finds the closest intersection between a ray and active cell AABBs on the CPU (for medium grids;
    /// large grids should use GPU surface picking with triangle-to-cell lookup).
    /// </summary>
    public static bool TryPickCell(
        ViewportVoxelGrid grid,
        double rayOriginX, double rayOriginY, double rayOriginZ,
        double rayDirX, double rayDirY, double rayDirZ,
        out int activeSlot,
        out int linearIndex,
        out int i, out int j, out int k,
        out double hitX, out double hitY, out double hitZ,
        out double distance)
    {
        activeSlot = linearIndex = i = j = k = 0;
        hitX = hitY = hitZ = distance = 0;
        ArgumentNullException.ThrowIfNull(grid);

        var active = grid.ActiveCellIndices;
        var bestT = double.PositiveInfinity;
        var found = false;
        var bestSlot = 0;
        var bestLinear = 0;
        var bestI = 0;
        var bestJ = 0;
        var bestK = 0;
        double bestHx = 0, bestHy = 0, bestHz = 0;

        for (var slot = 0; slot < active.Length; slot++)
        {
            if (!grid.TryDecodeIndex(active[slot], out var ci, out var cj, out var ck))
            {
                continue;
            }

            grid.GetCellMinCorner(ci, cj, ck, out var minX, out var minY, out var minZ);
            var maxX = minX + grid.CellSizeX;
            var maxY = minY + grid.CellSizeY;
            var maxZ = minZ + grid.CellSizeZ;

            if (!RayAabb(rayOriginX, rayOriginY, rayOriginZ, rayDirX, rayDirY, rayDirZ,
                    minX, minY, minZ, maxX, maxY, maxZ, out var t, out var hx, out var hy, out var hz)
                || t < 0.0 || t >= bestT)
            {
                continue;
            }

            bestT = t;
            found = true;
            bestSlot = slot;
            bestLinear = active[slot];
            bestI = ci;
            bestJ = cj;
            bestK = ck;
            bestHx = hx;
            bestHy = hy;
            bestHz = hz;
        }

        if (!found)
        {
            return false;
        }

        activeSlot = bestSlot;
        linearIndex = bestLinear;
        i = bestI;
        j = bestJ;
        k = bestK;
        hitX = bestHx;
        hitY = bestHy;
        hitZ = bestHz;
        distance = bestT;
        return true;
    }

    /// <summary>Creates a <see cref="VoxelProbeResult"/> for the specified cell and hit coordinates.</summary>
    public static VoxelProbeResult CreateProbeResult(
        ViewportVoxelGrid grid, int activeSlot, int linearIndex, int i, int j, int k,
        double hitX, double hitY, double hitZ)
    {
        ArgumentNullException.ThrowIfNull(grid);
        double? scalar = null;
        if (grid.ScalarValues is { } s && activeSlot >= 0 && activeSlot < s.Length)
        {
            scalar = s[activeSlot];
        }

        var region = 0;
        if (grid.RegionIds is { } r && activeSlot >= 0 && activeSlot < r.Length)
        {
            region = r[activeSlot];
        }

        return new VoxelProbeResult(grid, activeSlot, linearIndex, i, j, k, hitX, hitY, hitZ, scalar, region);
    }

    private static bool RayAabb(
        double ox, double oy, double oz,
        double dx, double dy, double dz,
        double minX, double minY, double minZ,
        double maxX, double maxY, double maxZ,
        out double tHit, out double hx, out double hy, out double hz)
    {
        tHit = 0;
        hx = hy = hz = 0;

        var tMin = double.NegativeInfinity;
        var tMax = double.PositiveInfinity;

        if (!Slab(ox, dx, minX, maxX, ref tMin, ref tMax)) return false;
        if (!Slab(oy, dy, minY, maxY, ref tMin, ref tMax)) return false;
        if (!Slab(oz, dz, minZ, maxZ, ref tMin, ref tMax)) return false;

        if (tMax < 0.0 || tMin > tMax)
        {
            return false;
        }

        tHit = tMin >= 0.0 ? tMin : tMax;
        if (tHit < 0.0)
        {
            return false;
        }

        hx = ox + dx * tHit;
        hy = oy + dy * tHit;
        hz = oz + dz * tHit;
        return true;
    }

    private static bool Slab(double o, double d, double min, double max, ref double tMin, ref double tMax)
    {
        const double eps = 1e-15;
        if (Math.Abs(d) < eps)
        {
            return o >= min && o <= max;
        }

        var inv = 1.0 / d;
        var t1 = (min - o) * inv;
        var t2 = (max - o) * inv;
        if (t1 > t2)
        {
            (t1, t2) = (t2, t1);
        }

        tMin = Math.Max(tMin, t1);
        tMax = Math.Min(tMax, t2);
        return tMin <= tMax;
    }
}

/// <summary>Hit information for a single voxel cell probe.</summary>
public sealed record VoxelProbeResult(
    ViewportVoxelGrid Grid,
    int ActiveSlot,
    int LinearIndex,
    int I,
    int J,
    int K,
    double X,
    double Y,
    double Z,
    double? ScalarValue,
    int RegionId);

/// <summary>Event arguments for <see cref="WorkbenchViewport.VoxelProbePicked"/>.</summary>
public sealed class VoxelProbePickedEventArgs(VoxelProbeResult result) : EventArgs
{
    /// <summary>Voxel probe hit information.</summary>
    public VoxelProbeResult Result { get; } = result;

    /// <summary>When true, suppresses the default automatic annotation creation. Default is false.</summary>
    public bool Handled { get; set; }
}
