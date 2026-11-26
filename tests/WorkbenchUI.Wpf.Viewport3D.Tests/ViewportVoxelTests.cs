using WorkbenchUI.Wpf.Viewport3D;

namespace WorkbenchUI.Wpf.Viewport3D.Tests;

public class ViewportVoxelTests
{
    [Fact]
    public void EncodeDecode_RoundTrips()
    {
        const int nx = 10, ny = 8, nz = 6;
        for (var k = 0; k < nz; k++)
        for (var j = 0; j < ny; j++)
        for (var i = 0; i < nx; i++)
        {
            var linear = ViewportVoxelIndexing.Encode(i, j, k, nx, ny);
            Assert.True(ViewportVoxelIndexing.TryDecode(linear, nx, ny, nz, out var ii, out var jj, out var kk));
            Assert.Equal(i, ii);
            Assert.Equal(j, jj);
            Assert.Equal(k, kk);
        }
    }

    [Fact]
    public void Extract_SingleCell_EmitsSixFaces()
    {
        var grid = new ViewportVoxelGrid
        {
            ResolutionX = 4,
            ResolutionY = 4,
            ResolutionZ = 4,
            CellSizeX = 1,
            CellSizeY = 1,
            CellSizeZ = 1,
            ActiveCellIndices = [ViewportVoxelIndexing.Encode(1, 1, 1, 4, 4)],
            ScalarValues = [3.5],
        };

        var surface = ViewportVoxelSurface.Extract(grid);

        // 6 faces × 2 triangles
        Assert.Equal(12, surface.TriangleToLinearIndex.Length);
        Assert.Equal(6 * 4 * 3, surface.Positions.Length);
        Assert.NotNull(surface.ScalarValues);
        Assert.All(surface.ScalarValues!, s => Assert.Equal(3.5, s));
    }

    [Fact]
    public void Extract_TwoAdjacentCells_ShareFaceHidden()
    {
        var nx = 4;
        var a = ViewportVoxelIndexing.Encode(1, 1, 1, nx, 4);
        var b = ViewportVoxelIndexing.Encode(2, 1, 1, nx, 4); // +X neighbor
        var grid = new ViewportVoxelGrid
        {
            ResolutionX = nx,
            ResolutionY = 4,
            ResolutionZ = 4,
            ActiveCellIndices = [a, b],
        };

        var surface = ViewportVoxelSurface.Extract(grid);

        // 2 cells × 6 faces - 2 shared = 10 faces × 2 tris
        Assert.Equal(20, surface.TriangleToLinearIndex.Length);
    }

    [Fact]
    public void Extract_RegionFilter_HidesOtherRegions()
    {
        var grid = new ViewportVoxelGrid
        {
            ResolutionX = 4,
            ResolutionY = 4,
            ResolutionZ = 4,
            ActiveCellIndices =
            [
                ViewportVoxelIndexing.Encode(0, 0, 0, 4, 4),
                ViewportVoxelIndexing.Encode(2, 2, 2, 4, 4),
            ],
            RegionIds = [1, 2],
        };

        var surface = ViewportVoxelSurface.Extract(grid, new HashSet<int> { 1 });
        Assert.True(surface.TriangleToLinearIndex.Length > 0);
        Assert.All(surface.TriangleToLinearIndex, linear => Assert.Equal(grid.ActiveCellIndices[0], linear));
    }

    [Fact]
    public void TryMapTriangle_ReturnsCell()
    {
        var grid = new ViewportVoxelGrid
        {
            ResolutionX = 3,
            ResolutionY = 3,
            ResolutionZ = 3,
            ActiveCellIndices = [ViewportVoxelIndexing.Encode(1, 1, 1, 3, 3)],
        };
        var surface = ViewportVoxelSurface.Extract(grid);

        Assert.True(ViewportVoxelPicking.TryMapTriangle(
            grid, surface, 0, out var slot, out var linear, out var i, out var j, out var k));
        Assert.Equal(0, slot);
        Assert.Equal(grid.ActiveCellIndices[0], linear);
        Assert.Equal(1, i);
        Assert.Equal(1, j);
        Assert.Equal(1, k);
    }

    [Fact]
    public void TryPickCell_HitsFromOutside()
    {
        var grid = new ViewportVoxelGrid
        {
            OriginX = 0,
            OriginY = 0,
            OriginZ = 0,
            CellSizeX = 1,
            CellSizeY = 1,
            CellSizeZ = 1,
            ResolutionX = 4,
            ResolutionY = 4,
            ResolutionZ = 4,
            ActiveCellIndices = [ViewportVoxelIndexing.Encode(1, 1, 1, 4, 4)],
            ScalarValues = [42.0],
            RegionIds = [7],
        };

        // Ray toward cell center (1.5, 1.5, 1.5) from (-2, 1.5, 1.5)
        Assert.True(ViewportVoxelPicking.TryPickCell(
            grid, -2, 1.5, 1.5, 1, 0, 0,
            out var slot, out _, out var i, out var j, out var k,
            out _, out _, out _, out var dist));

        Assert.Equal(0, slot);
        Assert.Equal(1, i);
        Assert.Equal(1, j);
        Assert.Equal(1, k);
        Assert.True(dist > 0);

        var probe = ViewportVoxelPicking.CreateProbeResult(grid, slot, grid.ActiveCellIndices[0], i, j, k, 1, 1.5, 1.5);
        Assert.Equal(42.0, probe.ScalarValue);
        Assert.Equal(7, probe.RegionId);
    }

    [Fact]
    public void Slice_CollectsNearPlane()
    {
        var grid = new ViewportVoxelGrid
        {
            ResolutionX = 5,
            ResolutionY = 5,
            ResolutionZ = 5,
            CellSizeX = 1,
            CellSizeY = 1,
            CellSizeZ = 1,
            ActiveCellIndices =
            [
                ViewportVoxelIndexing.Encode(0, 0, 2, 5, 5),
                ViewportVoxelIndexing.Encode(1, 1, 2, 5, 5),
                ViewportVoxelIndexing.Encode(2, 2, 4, 5, 5),
            ],
        };

        // Plane z = 2.5 (through cell centers at k=2)
        var plane = new SectionPlane(0, 0, 2.5, 0, 0, 1);
        var slots = ViewportVoxelSlice.CollectActiveSlotsNearPlane(grid, plane);
        Assert.Equal(2, slots.Length);
        Assert.Contains(0, slots);
        Assert.Contains(1, slots);
    }

    [Fact]
    public void ComputeBounds_MatchesActiveExtent()
    {
        var grid = new ViewportVoxelGrid
        {
            OriginX = 10,
            OriginY = 20,
            OriginZ = 30,
            CellSizeX = 2,
            CellSizeY = 2,
            CellSizeZ = 2,
            ResolutionX = 8,
            ResolutionY = 8,
            ResolutionZ = 8,
            ActiveCellIndices =
            [
                ViewportVoxelIndexing.Encode(0, 0, 0, 8, 8),
                ViewportVoxelIndexing.Encode(2, 1, 0, 8, 8),
            ],
        };

        var bounds = grid.ComputeBounds();
        Assert.Equal(10, bounds.MinX);
        Assert.Equal(20, bounds.MinY);
        Assert.Equal(30, bounds.MinZ);
        Assert.Equal(10 + 3 * 2, bounds.MaxX); // i=0..2 → 3 cells wide from origin of i=0
        Assert.Equal(20 + 2 * 2, bounds.MaxY);
        Assert.Equal(30 + 2, bounds.MaxZ);
    }

    [Fact]
    public void FormatDefaultVoxelProbeLabel_IncludesIndices()
    {
        var grid = new ViewportVoxelGrid();
        var result = new VoxelProbeResult(grid, 0, 0, 1, 2, 3, 0, 0, 0, 1.25, 4);
        var label = WorkbenchViewport.FormatDefaultVoxelProbeLabel(result);
        Assert.Contains("C[1,2,3]", label);
        Assert.Contains("R4", label);
        Assert.Contains("1.25", label);
    }

    [Fact]
    public void Coarsen_ReducesActiveCountAndAveragesScalar()
    {
        // Coalesce 2x2x2 blocks into 1 brick with brickSize=2
        var indices = new List<int>();
        var scalars = new List<double>();
        for (var k = 0; k < 2; k++)
        for (var j = 0; j < 2; j++)
        for (var i = 0; i < 2; i++)
        {
            indices.Add(ViewportVoxelIndexing.Encode(i, j, k, 8, 8));
            scalars.Add(i + j + k);
        }

        var grid = new ViewportVoxelGrid
        {
            ResolutionX = 8,
            ResolutionY = 8,
            ResolutionZ = 8,
            ActiveCellIndices = indices.ToArray(),
            ScalarValues = scalars.ToArray(),
        };

        var c = ViewportVoxelLod.Coarsen(grid, brickSize: 2);
        Assert.Single(c.ActiveCellIndices);
        Assert.Equal(scalars.Average(), c.ScalarValues![0], 1e-9);
    }

    [Fact]
    public void CreateDisplayGrid_AboveThreshold_UsesLargerCells()
    {
        var active = Enumerable.Range(0, ViewportVoxelLod.DefaultActiveThreshold + 10).ToArray();
        var grid = new ViewportVoxelGrid
        {
            ResolutionX = 200,
            ResolutionY = 200,
            ResolutionZ = 200,
            CellSizeX = 1,
            CellSizeY = 1,
            CellSizeZ = 1,
            ActiveCellIndices = active,
        };

        var display = ViewportVoxelLod.CreateDisplayGrid(grid, brickSize: 4);
        Assert.Equal(4.0, display.CellSizeX);
        Assert.True(display.ActiveCount < grid.ActiveCount);
        Assert.True(display.ResolutionX < grid.ResolutionX);
    }

    [Fact]
    public void EffectiveGlyphStride_MultipliesWhenLodActive()
    {
        Assert.Equal(2, ViewportVoxelLod.EffectiveGlyphStride(2, 4, lodActive: false));
        Assert.Equal(8, ViewportVoxelLod.EffectiveGlyphStride(2, 4, lodActive: true));
    }

    [Fact]
    public void BuildVoxelInstances_EmitsPerStride()
    {
        var grid = new ViewportVoxelGrid
        {
            ResolutionX = 4,
            ResolutionY = 4,
            ResolutionZ = 4,
            ActiveCellIndices =
            [
                ViewportVoxelIndexing.Encode(0, 0, 0, 4, 4),
                ViewportVoxelIndexing.Encode(1, 0, 0, 4, 4),
                ViewportVoxelIndexing.Encode(2, 0, 0, 4, 4),
                ViewportVoxelIndexing.Encode(3, 0, 0, 4, 4),
            ],
            VectorValues = [1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0],
        };

        var data = ViewportGlyph.BuildVoxelInstances(
            grid, stride: 2, 0, 0, 0, null, new System.Numerics.Vector4(1, 0, 0, 1), out var count);
        Assert.Equal(2, count);
        Assert.Equal(2 * ViewportGlyph.FloatsPerInstance, data.Length);
    }

    [Fact]
    public void FaceShell_AfterBrickLod_HasExteriorBoundsNearDomain()
    {
        // Smaller version of the stress test: outer faces lie near the domain boundary
        const int n = 40;
        var active = new List<int>();
        var seen = new HashSet<int>();
        void Add(int i, int j, int k)
        {
            var linear = ViewportVoxelIndexing.Encode(i, j, k, n, n);
            if (seen.Add(linear))
            {
                active.Add(linear);
            }
        }

        for (var j = 0; j < n; j += 2)
        for (var k = 0; k < n; k += 2)
        {
            Add(0, j, k);
            Add(n - 1, j, k);
        }

        var grid = new ViewportVoxelGrid
        {
            ResolutionX = n,
            ResolutionY = n,
            ResolutionZ = n,
            ActiveCellIndices = active.ToArray(),
            ScalarValues = active.Select(_ => 0.5).ToArray(),
        };

        // Lower threshold to force the brick path
        var display = ViewportVoxelLod.CreateDisplayGrid(grid, activeThreshold: 10, brickSize: 4);
        var surface = ViewportVoxelSurface.Extract(display);
        Assert.True(surface.TriangleIndices.Length > 0);
        var bounds = ViewportGeometry.ComputeBounds(surface.Positions);
        Assert.False(bounds.IsEmpty);
        // Covers the full domain even after bricking (extends near outer boundaries)
        Assert.True(bounds.MaxX - bounds.MinX >= n * 0.7);
    }

    [Fact]
    public void StressLike_1000FaceShell_BrickExtractAndGlyphs_NonEmpty()
    {
        const int n = 1000;
        const int step = 3;
        var active = new List<int>();
        var seen = new HashSet<int>();
        void Add(int i, int j, int k)
        {
            var linear = ViewportVoxelIndexing.Encode(i, j, k, n, n);
            if (seen.Add(linear))
            {
                active.Add(linear);
            }
        }

        for (var j = 0; j < n; j += step)
        for (var k = 0; k < n; k += step)
        {
            Add(0, j, k);
            Add(n - 1, j, k);
        }

        for (var i = 0; i < n; i += step)
        for (var k = 0; k < n; k += step)
        {
            Add(i, 0, k);
            Add(i, n - 1, k);
        }

        for (var i = 0; i < n; i += step)
        for (var j = 0; j < n; j += step)
        {
            Add(i, j, 0);
            Add(i, j, n - 1);
        }

        var vectors = new double[active.Count * 3];
        for (var s = 0; s < active.Count; s++)
        {
            vectors[s * 3] = 2;
        }

        var grid = new ViewportVoxelGrid
        {
            ResolutionX = n,
            ResolutionY = n,
            ResolutionZ = n,
            ActiveCellIndices = active.ToArray(),
            ScalarValues = active.Select(_ => 0.5).ToArray(),
            VectorValues = vectors,
        };

        Assert.True(grid.ActiveCount > ViewportVoxelLod.DefaultActiveThreshold);

        var display = ViewportVoxelLod.CreateDisplayGrid(grid);
        Assert.True(display.CellSizeX > grid.CellSizeX);
        var surface = ViewportVoxelSurface.Extract(display);
        Assert.True(surface.TriangleIndices.Length > 0);
        var bounds = ViewportGeometry.ComputeBounds(surface.Positions);
        Assert.True(bounds.MaxX - bounds.MinX >= n * 0.7);

        var glyphs = ViewportGlyph.BuildVoxelInstances(
            grid, stride: 4, 0, 0, 0, null, new System.Numerics.Vector4(1, 0, 0, 1), out var gcount);
        Assert.True(gcount > 1000);
        Assert.Equal(gcount * ViewportGlyph.FloatsPerInstance, glyphs.Length);
    }

    [Fact]
    public void ReaverageScalarsOntoDisplay_MatchesBrickAverages()
    {
        var source = new ViewportVoxelGrid
        {
            ResolutionX = 4,
            ResolutionY = 4,
            ResolutionZ = 4,
            ActiveCellIndices =
            [
                ViewportVoxelIndexing.Encode(0, 0, 0, 4, 4),
                ViewportVoxelIndexing.Encode(1, 0, 0, 4, 4),
                ViewportVoxelIndexing.Encode(0, 1, 0, 4, 4),
                ViewportVoxelIndexing.Encode(1, 1, 0, 4, 4),
            ],
            ScalarValues = [1, 3, 5, 7],
        };

        var display = ViewportVoxelLod.CreateDisplayGrid(source, activeThreshold: 1, brickSize: 2);
        Assert.Equal(1, display.ActiveCount);

        var averaged = ViewportVoxelLod.ReaverageScalarsOntoDisplay(source, display, brickSize: 2);
        Assert.Single(averaged);
        Assert.Equal(4.0, averaged[0], 1e-9);
    }

    [Fact]
    public void BuildVertexScalars_PropagatesCellValuesToFaceVertices()
    {
        var grid = new ViewportVoxelGrid
        {
            ResolutionX = 2,
            ResolutionY = 2,
            ResolutionZ = 2,
            ActiveCellIndices = [ViewportVoxelIndexing.Encode(0, 0, 0, 2, 2)],
            ScalarValues = [42.0],
        };

        var surface = ViewportVoxelSurface.Extract(grid);
        var vertices = ViewportVoxelSurface.BuildVertexScalars(surface, [99.0]);
        Assert.Equal(surface.Positions.Length / 3, vertices.Length);
        Assert.All(vertices, v => Assert.Equal(99.0, v));
    }

    [Fact]
    public void BuildVertexScalars_NullCells_YieldZeros()
    {
        var grid = new ViewportVoxelGrid
        {
            ResolutionX = 2,
            ResolutionY = 2,
            ResolutionZ = 2,
            ActiveCellIndices = [0],
            ScalarValues = [1.0],
        };
        var surface = ViewportVoxelSurface.Extract(grid);
        var vertices = ViewportVoxelSurface.BuildVertexScalars(surface, null);
        Assert.All(vertices, v => Assert.Equal(0.0, v));
    }
}
