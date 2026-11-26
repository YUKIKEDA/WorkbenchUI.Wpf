using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Viewport3D;

namespace WorkbenchUI.Wpf.Gallery.Pages;

/// <summary>
/// Voxel demo switching between a medium-scale fill animation with velocity glyphs
/// and a sparse 1000³ logical domain stress test with PlaybackBar scalar field swapping.
/// </summary>
public partial class ViewportVoxelsPage : UserControl
{
    private const int MediumN = 32;
    private const int StressN = 1000;
    private const int FrameCount = 48;
    /// <summary>Target arrow length fraction (~8% of model diagonal) so voxel glyphs remain visible.</summary>
    private const double GlyphTargetFraction = 0.08;

    private readonly ObservableCollection<ViewportVoxelGrid> _voxels = [];
    private readonly SectionPlane _plane = new()
    {
        OriginX = 16, OriginY = 16, OriginZ = 16,
        NormalX = 1, NormalY = 0, NormalZ = 0,
    };

    private readonly ColorScale _scale = new()
    {
        ColorMap = ColorMap.Jet,
        Minimum = 0,
        Maximum = 1,
    };

    private ViewportVoxelGrid? _grid;
    private double[][]? _fillFrames;
    private double[]? _baseScalars;
    private int _logicalResolution = MediumN;

    public ViewportVoxelsPage()
    {
        InitializeComponent();

        Viewport.VoxelSource = _voxels;
        Viewport.ColorScale = _scale;
        Viewport.GlyphColorScale = _scale;
        Legend.Scale = _scale;
        Viewport.VoxelProbePicked += OnVoxelProbePicked;
        Playback.FrameCount = FrameCount;

        Viewport.GeometryBuildCompleted += (_, _) =>
        {
            // Collection swaps trigger auto-fit, and an explicit Fit ensures an exterior view of the hollow shell
            Viewport.FitToView();
            ApplyGlyphScale();
            UpdateStatsAfterBuild();
        };

        LoadMediumInternal();

        Loaded += (_, _) =>
        {
            RendererInfo.Text = Viewport.IsSoftwareRendering
                ? "Rendering path: WARP (software) + WriteableBitmap fallback"
                : "Rendering path: Hardware D3D11 + D3DImage (shared surface)";
        };
    }

    private void OnVoxelProbePicked(object? sender, VoxelProbePickedEventArgs e)
    {
        var r = e.Result;
        ProbeInfo.Text = FormattableString.Invariant(
            $"Cell [{r.I},{r.J},{r.K}]  Linear={r.LinearIndex}  Region={r.RegionId}  Scalar={r.ScalarValue:G4}");
    }

    private void OnContourChanged(object sender, RoutedEventArgs e)
    {
        if (Viewport is null) return;
        Viewport.ShowContours = ContourToggle.IsChecked == true;
    }

    private void OnClipChanged(object sender, RoutedEventArgs e)
    {
        if (Viewport is null) return;
        Viewport.SectionPlane = ClipToggle.IsChecked == true ? _plane : null;
    }

    private void OnProbeModeChanged(object sender, RoutedEventArgs e)
    {
        if (Viewport is null) return;
        Viewport.PickMode = CellProbeToggle.IsChecked == true
            ? ViewportPickMode.Cell
            : ViewportPickMode.None;
    }

    private void OnGlyphChanged(object sender, RoutedEventArgs e)
    {
        if (Viewport is null) return;
        Viewport.ShowGlyphs = GlyphToggle.IsChecked == true;
        if (Viewport.ShowGlyphs)
        {
            ApplyGlyphScale();
        }
    }

    private void ApplyGlyphScale()
    {
        if (Viewport is null || GlyphToggle.IsChecked != true)
        {
            return;
        }

        Viewport.ShowGlyphs = true;
        var suggested = Viewport.GetSuggestedGlyphScale(GlyphTargetFraction);
        // Clamp minimum scale to a few cell widths so arrows don't collapse to dots
        var cell = _grid?.CellSizeX ?? 1.0;
        Viewport.GlyphScale = Math.Max(suggested, cell * 3.0);
        Viewport.GlyphStride = _logicalResolution >= 256 ? 4 : 2;
    }

    private void OnFitClick(object sender, RoutedEventArgs e) => Viewport?.FitToView();

    private void OnClearAnnotations(object sender, RoutedEventArgs e) => Viewport?.Annotations.Clear();

    private void OnLoadMedium(object sender, RoutedEventArgs e) => LoadMediumInternal();

    private void OnLoadStress(object sender, RoutedEventArgs e)
    {
        Playback.IsPlaying = false;
        var sw = Stopwatch.StartNew();
        // Logical 1000³ grid: sparsely sample all 6 faces with step=3 (~670K active cells -> triggers brick LOD)
        var grid = CreateFaceShell(StressN, sampleStep: 3);
        AttachVectorsRadial(grid);
        _baseScalars = (double[])grid.ScalarValues!.Clone();
        _fillFrames = BuildFillFrames(grid, _baseScalars);
        sw.Stop();
        ApplyGrid(grid, StressN);
        Playback.CurrentFrame = 0;
        OnPlaybackFrameChanged(Playback, new RoutedPropertyChangedEventArgs<int>(-1, 0));
        StatsText.Text = FormattableString.Invariant(
            $"Logical {StressN}³ / Generated {sw.Elapsed.TotalMilliseconds:F0} ms / Active {grid.ActiveCount:N0} (above threshold -> building brick LOD...)");
        ProbeInfo.Text =
            "Stress test: logical 1000³ sparse shell + Playback fill animation (field swap without rebuilding exterior faces). Fits view after build.";
    }

    private void OnPlaybackFrameChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
    {
        if (_grid is null || _fillFrames is null || e.NewValue < 0 || e.NewValue >= _fillFrames.Length)
        {
            return;
        }

        _grid.ScalarValues = _fillFrames[e.NewValue];
        _scale.Maximum = 1.0;
        _scale.Minimum = 0.0;
    }

    private void LoadMediumInternal()
    {
        var grid = CreateSparseShell(MediumN, wall: 1, includeRunner: true);
        AttachVectorsRadial(grid);
        _baseScalars = (double[])grid.ScalarValues!.Clone();
        _fillFrames = BuildFillFrames(grid, _baseScalars);
        ApplyGrid(grid, MediumN);
        Playback.CurrentFrame = 0;
        OnPlaybackFrameChanged(Playback, new RoutedPropertyChangedEventArgs<int>(-1, 0));
        // Show glyphs by default in medium demo for easy inspection
        if (GlyphToggle is not null)
        {
            GlyphToggle.IsChecked = true;
        }

        StatsText.Text = FormattableString.Invariant(
            $"Logical {MediumN}³ / Active {grid.ActiveCount:N0} / Frames {FrameCount}");
    }

    private void ApplyGrid(ViewportVoxelGrid grid, int logicalResolution)
    {
        _grid = grid;
        _logicalResolution = logicalResolution;
        _voxels.Clear();
        _voxels.Add(grid);
        _plane.OriginX = grid.ResolutionX * grid.CellSizeX * 0.5;
        _plane.OriginY = grid.ResolutionY * grid.CellSizeY * 0.5;
        _plane.OriginZ = grid.ResolutionZ * grid.CellSizeZ * 0.5;
    }

    private void UpdateStatsAfterBuild()
    {
        if (_grid is null || StatsText is null)
        {
            return;
        }

        var stats = Viewport.GetStatistics();
        var brick = _grid.ActiveCount > 100_000 ? " / brick LOD" : "";
        StatsText.Text = FormattableString.Invariant(
            $"Grid {_logicalResolution}³ / Active {_grid.ActiveCount:N0} / Triangles {stats.TriangleCount:N0} / Build {stats.LastGeometryBuildTime.TotalMilliseconds:F0} ms{brick}");
    }

    /// <summary>Hollow shell (with optional runner). Initial scalars use normalized Y (for medium demo).</summary>
    private static ViewportVoxelGrid CreateSparseShell(int n, int wall, bool includeRunner)
    {
        var active = new List<int>();
        var regions = new List<int>();
        var scalars = new List<double>();
        var lo = wall;
        var hi = n - 1 - wall;

        for (var k = 0; k < n; k++)
        for (var j = 0; j < n; j++)
        for (var i = 0; i < n; i++)
        {
            var inBox = i >= lo && i <= hi && j >= lo && j <= hi && k >= lo && k <= hi;
            var inner = i > lo && i < hi && j > lo && j < hi && k > lo && k < hi;
            var shell = inBox && !inner;
            var runner = includeRunner
                         && i <= lo
                         && Math.Abs(j - n / 2) <= wall
                         && Math.Abs(k - n / 2) <= wall;

            if (!shell && !runner)
            {
                continue;
            }

            active.Add(ViewportVoxelIndexing.Encode(i, j, k, n, n));
            regions.Add(runner && !shell ? 2 : 1);
            scalars.Add(j / (double)Math.Max(n - 1, 1));
        }

        return FinishGrid(includeRunner ? "CastingLike" : $"Sparse{n}", n, active, regions, scalars);
    }

    /// <summary>
    /// Populates only the outer 6 faces of a logical n³ domain at the given step (for stress testing)
    /// without running a full triple loop.
    /// </summary>
    private static ViewportVoxelGrid CreateFaceShell(int n, int sampleStep)
    {
        sampleStep = Math.Max(sampleStep, 1);
        var active = new List<int>();
        var regions = new List<int>();
        var scalars = new List<double>();
        var seen = new HashSet<int>();

        void Add(int i, int j, int k)
        {
            var linear = ViewportVoxelIndexing.Encode(i, j, k, n, n);
            if (!seen.Add(linear))
            {
                return;
            }

            active.Add(linear);
            regions.Add(1);
            scalars.Add(j / (double)Math.Max(n - 1, 1));
        }

        for (var j = 0; j < n; j += sampleStep)
        for (var k = 0; k < n; k += sampleStep)
        {
            Add(0, j, k);
            Add(n - 1, j, k);
        }

        for (var i = 0; i < n; i += sampleStep)
        for (var k = 0; k < n; k += sampleStep)
        {
            Add(i, 0, k);
            Add(i, n - 1, k);
        }

        for (var i = 0; i < n; i += sampleStep)
        for (var j = 0; j < n; j += sampleStep)
        {
            Add(i, j, 0);
            Add(i, j, n - 1);
        }

        return FinishGrid($"Sparse{n}", n, active, regions, scalars);
    }

    private static ViewportVoxelGrid FinishGrid(
        string name, int n, List<int> active, List<int> regions, List<double> scalars)
        => new()
        {
            Name = name,
            ResolutionX = n,
            ResolutionY = n,
            ResolutionZ = n,
            CellSizeX = 1,
            CellSizeY = 1,
            CellSizeZ = 1,
            ActiveCellIndices = active.ToArray(),
            RegionIds = regions.ToArray(),
            ScalarValues = scalars.ToArray(),
            ShowEdges = false,
        };

    /// <summary>Outward synthetic velocity vectors scaled relative to cell size.</summary>
    private static void AttachVectorsRadial(ViewportVoxelGrid grid)
    {
        var active = grid.ActiveCellIndices;
        var vectors = new double[active.Length * 3];
        var cx = grid.ResolutionX * 0.5;
        var cy = grid.ResolutionY * 0.5;
        var cz = grid.ResolutionZ * 0.5;
        // Keep |v| on the order of a few cells so suggested glyph scaling stays well-conditioned
        var speedScale = Math.Max(grid.CellSizeX, 1.0) * 2.0;
        for (var slot = 0; slot < active.Length; slot++)
        {
            if (!grid.TryDecodeIndex(active[slot], out var i, out var j, out var k))
            {
                continue;
            }

            var dx = i + 0.5 - cx;
            var dy = j + 0.5 - cy;
            var dz = k + 0.5 - cz;
            var len = Math.Sqrt(dx * dx + dy * dy + dz * dz) + 1e-9;
            var speed = speedScale * (0.35 + 0.65 * (1.0 - i / (double)Math.Max(grid.ResolutionX - 1, 1)));
            vectors[slot * 3] = dx / len * speed;
            vectors[slot * 3 + 1] = dy / len * speed;
            vectors[slot * 3 + 2] = dz / len * speed;
        }

        grid.VectorValues = vectors;
    }

    /// <summary>Synthetic fill-fraction frames propagating from the runner side.</summary>
    private static double[][] BuildFillFrames(ViewportVoxelGrid grid, double[] baseScalars)
    {
        var active = grid.ActiveCellIndices;
        var frames = new double[FrameCount][];
        var maxI = 0;
        var slotI = new int[active.Length];
        for (var s = 0; s < active.Length; s++)
        {
            grid.TryDecodeIndex(active[s], out var i, out _, out _);
            slotI[s] = i;
            maxI = Math.Max(maxI, i);
        }

        for (var f = 0; f < FrameCount; f++)
        {
            var front = maxI * (f + 1) / (double)FrameCount;
            var frame = new double[active.Length];
            for (var s = 0; s < active.Length; s++)
            {
                var fill = slotI[s] <= front ? 1.0 : Math.Max(0.0, 1.0 - (slotI[s] - front) * 0.25);
                frame[s] = fill * (0.35 + 0.65 * baseScalars[s]);
            }

            frames[f] = frame;
        }

        return frames;
    }
}
