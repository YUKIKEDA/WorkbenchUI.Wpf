namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Internal brick-based level-of-detail (LOD) coarsening for sparse voxel grids.
/// </summary>
internal static class ViewportVoxelLod
{
    /// <summary>Default brick edge size in cells.</summary>
    public const int DefaultBrickSize = 4;

    /// <summary>Active cell threshold above which brick LOD coarsening is applied.</summary>
    public const int DefaultActiveThreshold = 100_000;

    /// <summary>Brick coarsening result with indices in the original grid's linear space.</summary>
    internal sealed record Coarsened(
        int[] ActiveCellIndices,
        int[]? RegionIds,
        double[]? ScalarValues,
        double[]? VectorValues,
        int BrickSize);

    /// <summary>
    /// Aggregates active cells into bricks. Each brick is represented by its minimum linear cell index,
    /// scalar/vector values are averaged within the brick, and the region ID comes from the representative cell.
    /// </summary>
    public static Coarsened Coarsen(ViewportVoxelGrid grid, int brickSize = DefaultBrickSize)
    {
        ArgumentNullException.ThrowIfNull(grid);
        brickSize = Math.Max(brickSize, 1);
        if (brickSize == 1 || grid.ActiveCount == 0)
        {
            return new Coarsened(
                grid.ActiveCellIndices,
                grid.RegionIds,
                grid.ScalarValues,
                grid.VectorValues,
                brickSize);
        }

        var nx = grid.ResolutionX;
        var ny = grid.ResolutionY;
        var nz = grid.ResolutionZ;
        var active = grid.ActiveCellIndices;
        var regions = grid.RegionIds;
        var scalars = grid.ScalarValues;
        var vectors = grid.VectorValues;

        // brickKey → (repLinear, slotSum for avg, count, region, vx,vy,vz)
        var bricks = new Dictionary<long, Accumul>();
        for (var slot = 0; slot < active.Length; slot++)
        {
            if (!ViewportVoxelIndexing.TryDecode(active[slot], nx, ny, nz, out var i, out var j, out var k))
            {
                continue;
            }

            var bi = i / brickSize;
            var bj = j / brickSize;
            var bk = k / brickSize;
            var key = ((long)bi << 42) | ((long)bj << 21) | (uint)bk;

            var region = regions is not null && slot < regions.Length ? regions[slot] : 0;
            var scalar = scalars is not null && slot < scalars.Length ? scalars[slot] : 0.0;
            double vx = 0, vy = 0, vz = 0;
            if (vectors is not null && slot * 3 + 2 < vectors.Length)
            {
                vx = vectors[slot * 3];
                vy = vectors[slot * 3 + 1];
                vz = vectors[slot * 3 + 2];
            }

            if (!bricks.TryGetValue(key, out var acc))
            {
                bricks[key] = new Accumul(active[slot], scalar, 1, region, vx, vy, vz);
            }
            else
            {
                // Representative cell is the one with the smaller linear index
                var rep = Math.Min(acc.RepLinear, active[slot]);
                bricks[key] = new Accumul(
                    rep,
                    acc.ScalarSum + scalar,
                    acc.Count + 1,
                    acc.RepLinear <= active[slot] ? acc.Region : region,
                    acc.Vx + vx, acc.Vy + vy, acc.Vz + vz);
            }
        }

        var indices = new int[bricks.Count];
        int[]? outRegions = regions is null ? null : new int[bricks.Count];
        double[]? outScalars = scalars is null ? null : new double[bricks.Count];
        double[]? outVectors = vectors is null ? null : new double[bricks.Count * 3];

        var n = 0;
        foreach (var acc in bricks.Values)
        {
            indices[n] = acc.RepLinear;
            if (outRegions is not null)
            {
                outRegions[n] = acc.Region;
            }

            if (outScalars is not null)
            {
                outScalars[n] = acc.ScalarSum / acc.Count;
            }

            if (outVectors is not null)
            {
                outVectors[n * 3] = acc.Vx / acc.Count;
                outVectors[n * 3 + 1] = acc.Vy / acc.Count;
                outVectors[n * 3 + 2] = acc.Vz / acc.Count;
            }

            n++;
        }

        return new Coarsened(indices, outRegions, outScalars, outVectors, brickSize);
    }

    /// <summary>
    /// Re-averages source scalar values onto the display grid's active slot order.
    /// Used when updating scalar fields during brick LOD display without re-running <see cref="Coarsen"/>.
    /// </summary>
    public static double[] ReaverageScalarsOntoDisplay(
        ViewportVoxelGrid source, ViewportVoxelGrid display, int brickSize = DefaultBrickSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(display);

        var sourceScalars = source.ScalarValues;
        if (sourceScalars is null)
        {
            return new double[display.ActiveCount];
        }

        if (ReferenceEquals(source, display) || brickSize <= 1)
        {
            if (sourceScalars.Length == display.ActiveCount)
            {
                return sourceScalars;
            }

            var copy = new double[display.ActiveCount];
            Array.Copy(sourceScalars, copy, Math.Min(sourceScalars.Length, copy.Length));
            return copy;
        }

        brickSize = Math.Max(brickSize, 1);
        var keyToSlot = new Dictionary<long, int>(display.ActiveCount);
        for (var s = 0; s < display.ActiveCount; s++)
        {
            if (!display.TryDecodeIndex(display.ActiveCellIndices[s], out var bi, out var bj, out var bk))
            {
                continue;
            }

            var key = ((long)bi << 42) | ((long)bj << 21) | (uint)bk;
            keyToSlot[key] = s;
        }

        var sums = new double[display.ActiveCount];
        var counts = new int[display.ActiveCount];
        var nx = source.ResolutionX;
        var ny = source.ResolutionY;
        var nz = source.ResolutionZ;
        var active = source.ActiveCellIndices;
        for (var slot = 0; slot < active.Length; slot++)
        {
            if (!ViewportVoxelIndexing.TryDecode(active[slot], nx, ny, nz, out var i, out var j, out var k))
            {
                continue;
            }

            var key = ((long)(i / brickSize) << 42)
                      | ((long)(j / brickSize) << 21)
                      | (uint)(k / brickSize);
            if (!keyToSlot.TryGetValue(key, out var displaySlot))
            {
                continue;
            }

            sums[displaySlot] += slot < sourceScalars.Length ? sourceScalars[slot] : 0.0;
            counts[displaySlot]++;
        }

        var result = new double[display.ActiveCount];
        for (var s = 0; s < result.Length; s++)
        {
            result[s] = counts[s] > 0 ? sums[s] / counts[s] : 0.0;
        }

        return result;
    }

    /// <summary>
    /// Returns a coarsened display grid for rendering/glyphs if active cell count exceeds the threshold,
    /// without mutating <paramref name="source"/>.
    /// </summary>
    public static ViewportVoxelGrid CreateDisplayGrid(
        ViewportVoxelGrid source, int activeThreshold = DefaultActiveThreshold, int brickSize = DefaultBrickSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.ActiveCount <= activeThreshold)
        {
            return source;
        }

        var c = Coarsen(source, brickSize);
        return new ViewportVoxelGrid
        {
            Name = source.Name,
            OriginX = source.OriginX,
            OriginY = source.OriginY,
            OriginZ = source.OriginZ,
            CellSizeX = source.CellSizeX * brickSize,
            CellSizeY = source.CellSizeY * brickSize,
            CellSizeZ = source.CellSizeZ * brickSize,
            // Scale cell size by brickSize and treat each brick as a single large cell to keep picking aligned;
            // resolutions use ceiling division.
            ResolutionX = (source.ResolutionX + brickSize - 1) / brickSize,
            ResolutionY = (source.ResolutionY + brickSize - 1) / brickSize,
            ResolutionZ = (source.ResolutionZ + brickSize - 1) / brickSize,
            ActiveCellIndices = RemapToBrickIndices(c.ActiveCellIndices, source, brickSize),
            RegionIds = c.RegionIds,
            ScalarValues = c.ScalarValues,
            VectorValues = c.VectorValues,
            Color = source.Color,
            IsVisible = source.IsVisible,
            IsClippable = source.IsClippable,
            ShowEdges = source.ShowEdges,
        };
    }

    /// <summary>Effective glyph stride, scaled by brick size when brick LOD is active.</summary>
    public static int EffectiveGlyphStride(int glyphStride, int brickSize, bool lodActive)
    {
        var stride = Math.Max(glyphStride, 1);
        return lodActive ? stride * Math.Max(brickSize, 1) : stride;
    }

    private static int[] RemapToBrickIndices(int[] fineLinears, ViewportVoxelGrid source, int brickSize)
    {
        var nx = source.ResolutionX;
        var ny = source.ResolutionY;
        var nz = source.ResolutionZ;
        var brickNx = (nx + brickSize - 1) / brickSize;
        var brickNy = (ny + brickSize - 1) / brickSize;
        var result = new int[fineLinears.Length];
        for (var n = 0; n < fineLinears.Length; n++)
        {
            if (!ViewportVoxelIndexing.TryDecode(fineLinears[n], nx, ny, nz, out var i, out var j, out var k))
            {
                result[n] = 0;
                continue;
            }

            result[n] = ViewportVoxelIndexing.Encode(i / brickSize, j / brickSize, k / brickSize, brickNx, brickNy);
        }

        return result;
    }

    private readonly record struct Accumul(
        int RepLinear, double ScalarSum, int Count, int Region, double Vx, double Vy, double Vz);
}
