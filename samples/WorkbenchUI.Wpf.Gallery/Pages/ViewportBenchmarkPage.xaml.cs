using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Viewport3D;

namespace WorkbenchUI.Wpf.Gallery.Pages;

/// <summary>
/// Large-mesh performance benchmark demo. Generates synthetic wave meshes from 100K to 200M
/// triangles and reports build time, render time, LOD/culling stats, and measured FPS via
/// <see cref="WorkbenchViewport.GetStatistics"/>. Face picking also verifies chunk base offsets,
/// and orbit animation verifies interactive LOD behavior.
/// </summary>
public partial class ViewportBenchmarkPage : UserControl
{
    private readonly ObservableCollection<ViewportMesh> _meshes = [];
    private readonly ColorScale _colorScale;
    private readonly DispatcherTimer _statsTimer;

    // Orbit animation + measured FPS (counts actual CompositionTarget.Rendering frames)
    private bool _isRotating;
    private DateTime _fpsWindowStart;
    private int _fpsFrameCount;

    public ViewportBenchmarkPage()
    {
        InitializeComponent();

        _colorScale = new ColorScale
        {
            ColorMap = ColorMap.Jet,
            Minimum = -1.0,
            Maximum = 1.0,
        };

        Viewport.MeshSource = _meshes;
        Viewport.ColorScale = _colorScale;
        Viewport.Selection.Changed += OnSelectionChanged;

        // Async build progress reporting (the previous scene stays visible and interactive while building)
        Viewport.GeometryBuildProgressChanged += OnBuildProgressChanged;
        Viewport.GeometryBuildCompleted += (_, _) =>
        {
            OnBuildCompleted(this, EventArgs.Empty);
            UpdateRendererInfo();
        };

        // Geometry builds run lazily on the next render frame, so poll stats on a timer.
        // Because CompositionTarget.Rendering fires every frame during orbit animation and would
        // starve Background priority, use Normal priority so stats update even while LOD is active.
        _statsTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(500),
        };
        _statsTimer.Tick += (_, _) => UpdateStats();

        Loaded += (_, _) =>
        {
            if (_meshes.Count == 0)
            {
                RebuildMesh();
            }

            _statsTimer.Start();
            UpdateRendererInfo();
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, UpdateRendererInfo);
        };
        Unloaded += (_, _) =>
        {
            _statsTimer.Stop();
            SetRotating(false);
        };
    }

    private void UpdateRendererInfo() => RendererInfo.Text = Viewport.IsSoftwareRendering
        ? "Rendering path: WARP (software) + WriteableBitmap fallback"
        : "Rendering path: Hardware D3D11 + D3DImage (shared surface)";

    // ================= Synthetic wave mesh =================

    /// <summary>
    /// Generates an n×n grid wave surface (superposed sine waves) near the target triangle count.
    /// Triangle count = (n − 1)² × 2. Coordinate and scalar generation is parallelized per row.
    /// </summary>
    private static ViewportMesh CreateWaveMesh(int targetTriangles)
    {
        var side = Math.Max((int)Math.Round(Math.Sqrt(targetTriangles / 2.0)), 8);
        var n = side + 1;

        var positions = new double[n * n * 3];
        var scalars = new double[n * n];
        const double extent = 100.0;
        var step = extent / side;

        Parallel.For(0, n, i =>
        {
            var x = -extent / 2.0 + i * step;
            for (var j = 0; j < n; j++)
            {
                var y = -extent / 2.0 + j * step;
                // Superposed sine waves of different frequencies for visual detail and normal variation
                var z = 4.0 * Math.Sin(x * 0.12) * Math.Cos(y * 0.12)
                        + 1.5 * Math.Sin(x * 0.55 + y * 0.35);
                var v = i * n + j;
                positions[v * 3] = x;
                positions[v * 3 + 1] = y;
                positions[v * 3 + 2] = z;
                scalars[v] = z / 5.5; // Approximately [-1, 1]
            }
        });

        var triangles = new int[side * side * 6];
        Parallel.For(0, side, i =>
        {
            for (var j = 0; j < side; j++)
            {
                var i00 = i * n + j;
                var i01 = i00 + 1;
                var i10 = i00 + n;
                var i11 = i10 + 1;
                var dst = (i * side + j) * 6;
                triangles[dst] = i00;
                triangles[dst + 1] = i10;
                triangles[dst + 2] = i11;
                triangles[dst + 3] = i00;
                triangles[dst + 4] = i11;
                triangles[dst + 5] = i01;
            }
        });

        return new ViewportMesh
        {
            Name = "Synthetic Wave",
            Positions = positions,
            TriangleIndices = triangles,
            ScalarValues = scalars,
            ShowEdges = false, // Disabled by default since edge rendering dominates at large triangle counts
        };
    }

    private void RebuildMesh()
    {
        if (SizeCombo.SelectedItem is not ComboBoxItem { Tag: string tag }
            || !int.TryParse(tag, NumberStyles.Integer, CultureInfo.InvariantCulture, out var target))
        {
            return;
        }

        StatsText.Text = $"Generating... (target {target:N0} triangles)";
        Viewport.Selection.Clear();

        // CPU-side parallel mesh generation; GPU build runs on the next render frame
        var mesh = CreateWaveMesh(target);
        _meshes.Clear();
        _meshes.Add(mesh);
        Viewport.FitToView();
    }

    // ================= Async build progress =================

    private void OnBuildProgressChanged(object? sender, ViewportBuildProgressEventArgs e)
    {
        BuildProgress.Value = e.Progress;
        BuildText.Text = string.Create(
            CultureInfo.InvariantCulture, $"Building: {e.Stage} {e.Progress:P0}");
    }

    private void OnBuildCompleted(object? sender, EventArgs e)
    {
        BuildProgress.Value = 1.0;
        var stats = Viewport.GetStatistics();
        BuildText.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"Build complete: {stats.LastGeometryBuildTime.TotalMilliseconds:N0} ms (async build while keeping previous scene)");
        UpdateStats();
    }

    // ================= Statistics / FPS =================

    private void UpdateStats()
    {
        var stats = Viewport.GetStatistics();
        if (stats.TriangleCount == 0)
        {
            return;
        }

        StatsText.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"Triangles {stats.TriangleCount:N0} / Nodes {stats.VertexCount:N0} / Chunks {stats.ChunkCount} / "
            + $"Edge-skipped {stats.EdgeSkippedMeshCount} / "
            + $"Build {stats.LastGeometryBuildTime.TotalMilliseconds:N0} ms / "
            + $"Render {stats.LastRenderTime.TotalMilliseconds:N1} ms / "
            + $"LOD triangles {stats.LodTriangleCount:N0} / Drawn chunks {stats.LastDrawnChunkCount}"
            + $"{(stats.IsLodActive ? " / LOD active" : string.Empty)}");
    }

    private void OnCompositionRendering(object? sender, EventArgs e)
    {
        // Orbit slightly every frame to drive continuous rendering and measure actual FPS
        Viewport.Camera.Orbit(0.6, 0.0);

        _fpsFrameCount++;
        var elapsed = DateTime.UtcNow - _fpsWindowStart;
        if (elapsed >= TimeSpan.FromSeconds(1.0))
        {
            FpsText.Text = string.Create(
                CultureInfo.InvariantCulture,
                $"FPS: {_fpsFrameCount / elapsed.TotalSeconds:N1} (measured during orbit animation)");
            _fpsWindowStart = DateTime.UtcNow;
            _fpsFrameCount = 0;
        }
    }

    private void SetRotating(bool rotate)
    {
        if (_isRotating == rotate)
        {
            return;
        }

        _isRotating = rotate;
        if (rotate)
        {
            _fpsWindowStart = DateTime.UtcNow;
            _fpsFrameCount = 0;
            CompositionTarget.Rendering += OnCompositionRendering;
        }
        else
        {
            CompositionTarget.Rendering -= OnCompositionRendering;
            FpsText.Text = "FPS: -";
        }
    }

    // ================= UI handlers =================
    // Note: Events may fire during XAML parsing (before field initialization), so guard against null.

    private void OnSizeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Viewport is not null && IsLoaded)
        {
            RebuildMesh();
        }
    }

    private void OnRotateToggleChanged(object sender, RoutedEventArgs e)
    {
        if (Viewport is not null)
        {
            SetRotating(RotateToggle.IsChecked == true);
        }
    }

    /// <summary>Toggles interactive LOD (switches threshold, triggering a geometry rebuild).</summary>
    private void OnLodToggleChanged(object sender, RoutedEventArgs e)
    {
        if (Viewport is not null)
        {
            Viewport.InteractiveLodThreshold = LodToggle.IsChecked == true ? 5_000_000 : int.MaxValue;
        }
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        var faces = _meshes.Count > 0 ? Viewport.Selection.GetSelectedFaces(_meshes[0]) : [];
        PickText.Text = faces.Count == 0
            ? "Pick: Click to select a face (verifies chunk base-vertex offsets preserve global triangle IDs)"
            : string.Create(
                CultureInfo.InvariantCulture,
                $"Pick: {faces.Count} face(s) selected, triangle IDs [{string.Join(", ", faces.Take(5))}{(faces.Count > 5 ? ", ..." : string.Empty)}]");
    }

    private void OnFitClick(object sender, RoutedEventArgs e) => Viewport.FitToView();

    /// <summary>Top view (used by UIA tests for deterministic cross-chunk picking checks).</summary>
    private void OnTopViewClick(object sender, RoutedEventArgs e) =>
        Viewport.SetStandardView(ViewportStandardView.Top, animate: false);
}
