namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>Pure helper functions for linear voxel cell indexing (x varies fastest: index = i + j*Nx + k*Nx*Ny).</summary>
public static class ViewportVoxelIndexing
{
    /// <summary>Encodes 3D cell coordinates (<paramref name="i"/>, <paramref name="j"/>, <paramref name="k"/>) into a linear cell index.</summary>
    public static int Encode(int i, int j, int k, int resolutionX, int resolutionY)
        => i + j * resolutionX + k * resolutionX * resolutionY;

    /// <summary>Attempts to decode a linear cell index into 3D cell coordinates (<paramref name="i"/>, <paramref name="j"/>, <paramref name="k"/>).</summary>
    public static bool TryDecode(
        int linear, int resolutionX, int resolutionY, int resolutionZ,
        out int i, out int j, out int k)
    {
        i = j = k = 0;
        if (resolutionX <= 0 || resolutionY <= 0 || resolutionZ <= 0 || linear < 0)
        {
            return false;
        }

        var slab = resolutionX * resolutionY;
        var max = slab * resolutionZ;
        if (linear >= max)
        {
            return false;
        }

        k = linear / slab;
        var rem = linear - k * slab;
        j = rem / resolutionX;
        i = rem - j * resolutionX;
        return true;
    }

    /// <summary>Computes the world-space axis-aligned bounding box enclosing all valid active cells.</summary>
    public static Bounds3D ComputeActiveBounds(
        int[] activeCellIndices,
        int resolutionX, int resolutionY, int resolutionZ,
        double originX, double originY, double originZ,
        double cellSizeX, double cellSizeY, double cellSizeZ)
    {
        ArgumentNullException.ThrowIfNull(activeCellIndices);
        if (activeCellIndices.Length == 0)
        {
            return Bounds3D.Empty;
        }

        double minX = double.PositiveInfinity, minY = double.PositiveInfinity, minZ = double.PositiveInfinity;
        double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity, maxZ = double.NegativeInfinity;

        for (var n = 0; n < activeCellIndices.Length; n++)
        {
            if (!TryDecode(activeCellIndices[n], resolutionX, resolutionY, resolutionZ, out var i, out var j, out var k))
            {
                continue;
            }

            var x0 = originX + i * cellSizeX;
            var y0 = originY + j * cellSizeY;
            var z0 = originZ + k * cellSizeZ;
            var x1 = x0 + cellSizeX;
            var y1 = y0 + cellSizeY;
            var z1 = z0 + cellSizeZ;

            minX = Math.Min(minX, x0);
            minY = Math.Min(minY, y0);
            minZ = Math.Min(minZ, z0);
            maxX = Math.Max(maxX, x1);
            maxY = Math.Max(maxY, y1);
            maxZ = Math.Max(maxZ, z1);
        }

        return minX > maxX
            ? Bounds3D.Empty
            : new Bounds3D(minX, minY, minZ, maxX, maxY, maxZ);
    }
}
