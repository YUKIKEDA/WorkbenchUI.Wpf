using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Numerics;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Theming;
using WorkbenchUI.Wpf.Viewport3D.Rendering;

namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Direct3D 11 3D viewport control.
/// <para>
/// - Rendering: draws <see cref="ViewportMesh"/> parts bound via <see cref="MeshSource"/> with
///   two-sided headlight shading in solid color or nodal-scalar contours via <see cref="ColorScale"/>.
///   <see cref="ViewportVoxelGrid"/> items in <see cref="VoxelSource"/> are surface-extracted and rendered through the same pipeline.
/// - Camera: middle-button drag = orbit, Shift + middle-button drag = pan, wheel = zoom at cursor,
///   middle double-click = Fit to View.
/// - Picking: when <see cref="PickMode"/> is set, left-click = replace selection, Ctrl + click = toggle,
///   left-drag = rubber-band selection. Results populate <see cref="Selection"/> and render built-in highlights.
///   <see cref="ViewportPickMode.Cell"/> performs voxel cell probing (<see cref="VoxelProbePicked"/>).
/// - Viewpoints: supports <see cref="SetStandardView"/> and a clickable top-right ViewCube with smooth animation.
/// - Rendering is on-demand (on state changes or user input) and follows <see cref="ThemeManager.ThemeChanged"/>.
/// - Uses D3DImage on hardware GPUs (avoiding airspace issues) with automatic WriteableBitmap fallback on WARP / non-D3D9 environments.
/// </para>
/// </summary>
[TemplatePart(Name = PartImage, Type = typeof(Image))]
[TemplatePart(Name = PartTriadCanvas, Type = typeof(Canvas))]
[TemplatePart(Name = PartAnnotationCanvas, Type = typeof(Canvas))]
[TemplatePart(Name = PartViewCubeCanvas, Type = typeof(Canvas))]
public class WorkbenchViewport : Control
{
    private const string PartImage = "PART_Image";
    private const string PartTriadCanvas = "PART_TriadCanvas";
    private const string PartAnnotationCanvas = "PART_AnnotationCanvas";
    private const string PartViewCubeCanvas = "PART_ViewCubeCanvas";
    private const double TriadArmLength = 36.0;
    private const double TriadMargin = 56.0;
    private const double RubberBandThresholdDip = 4.0;
    private const double ViewAnimationDurationMs = 150.0;
    private const double AnnotationLeaderLengthDip = 18.0;

    /// <summary>Identifies the <see cref="MeshSource"/> dependency property.</summary>
    public static readonly DependencyProperty MeshSourceProperty =
        DependencyProperty.Register(
            nameof(MeshSource), typeof(IEnumerable), typeof(WorkbenchViewport),
            new PropertyMetadata(null, OnMeshSourceChanged));

    /// <summary>Identifies the <see cref="VoxelSource"/> dependency property.</summary>
    public static readonly DependencyProperty VoxelSourceProperty =
        DependencyProperty.Register(
            nameof(VoxelSource), typeof(IEnumerable), typeof(WorkbenchViewport),
            new PropertyMetadata(null, OnVoxelSourceChanged));

    /// <summary>Identifies the <see cref="ColorScale"/> dependency property.</summary>
    public static readonly DependencyProperty ColorScaleProperty =
        DependencyProperty.Register(
            nameof(ColorScale), typeof(ColorScale), typeof(WorkbenchViewport),
            new PropertyMetadata(null, OnColorScaleChanged));

    /// <summary>Identifies the <see cref="ShowContours"/> dependency property.</summary>
    public static readonly DependencyProperty ShowContoursProperty =
        DependencyProperty.Register(
            nameof(ShowContours), typeof(bool), typeof(WorkbenchViewport),
            new PropertyMetadata(true, OnVisualOptionChanged));

    /// <summary>Identifies the <see cref="ShowEdges"/> dependency property.</summary>
    public static readonly DependencyProperty ShowEdgesProperty =
        DependencyProperty.Register(
            nameof(ShowEdges), typeof(bool), typeof(WorkbenchViewport),
            new PropertyMetadata(true, OnVisualOptionChanged));

    /// <summary>Identifies the <see cref="Projection"/> dependency property.</summary>
    public static readonly DependencyProperty ProjectionProperty =
        DependencyProperty.Register(
            nameof(Projection), typeof(ViewportProjection), typeof(WorkbenchViewport),
            new PropertyMetadata(ViewportProjection.Perspective, OnProjectionChanged));

    /// <summary>Identifies the <see cref="UpAxis"/> dependency property.</summary>
    public static readonly DependencyProperty UpAxisProperty =
        DependencyProperty.Register(
            nameof(UpAxis), typeof(ViewportUpAxis), typeof(WorkbenchViewport),
            new PropertyMetadata(ViewportUpAxis.ZUp, OnUpAxisChanged));

    /// <summary>Identifies the <see cref="ShowAxisTriad"/> dependency property.</summary>
    public static readonly DependencyProperty ShowAxisTriadProperty =
        DependencyProperty.Register(
            nameof(ShowAxisTriad), typeof(bool), typeof(WorkbenchViewport),
            new PropertyMetadata(true, OnVisualOptionChanged));

    /// <summary>Identifies the <see cref="ShowViewCube"/> dependency property.</summary>
    public static readonly DependencyProperty ShowViewCubeProperty =
        DependencyProperty.Register(
            nameof(ShowViewCube), typeof(bool), typeof(WorkbenchViewport),
            new PropertyMetadata(true, OnVisualOptionChanged));

    /// <summary>Identifies the <see cref="PickMode"/> dependency property.</summary>
    public static readonly DependencyProperty PickModeProperty =
        DependencyProperty.Register(
            nameof(PickMode), typeof(ViewportPickMode), typeof(WorkbenchViewport),
            new PropertyMetadata(ViewportPickMode.None, OnPickModeChanged));

    /// <summary>Identifies the <see cref="DeformationScale"/> dependency property.</summary>
    public static readonly DependencyProperty DeformationScaleProperty =
        DependencyProperty.Register(
            nameof(DeformationScale), typeof(double), typeof(WorkbenchViewport),
            new PropertyMetadata(1.0, OnVisualOptionChanged),
            v => double.IsFinite((double)v));

    /// <summary>Identifies the <see cref="IsDeformationAnimated"/> dependency property.</summary>
    public static readonly DependencyProperty IsDeformationAnimatedProperty =
        DependencyProperty.Register(
            nameof(IsDeformationAnimated), typeof(bool), typeof(WorkbenchViewport),
            new PropertyMetadata(false, OnIsDeformationAnimatedChanged));

    /// <summary>Identifies the <see cref="DeformationAnimationPeriod"/> dependency property.</summary>
    public static readonly DependencyProperty DeformationAnimationPeriodProperty =
        DependencyProperty.Register(
            nameof(DeformationAnimationPeriod), typeof(TimeSpan), typeof(WorkbenchViewport),
            new PropertyMetadata(TimeSpan.FromSeconds(1.0)),
            v => (TimeSpan)v > TimeSpan.Zero);

    /// <summary>Identifies the <see cref="ShowUndeformedWireframe"/> dependency property.</summary>
    public static readonly DependencyProperty ShowUndeformedWireframeProperty =
        DependencyProperty.Register(
            nameof(ShowUndeformedWireframe), typeof(bool), typeof(WorkbenchViewport),
            new PropertyMetadata(false, OnVisualOptionChanged));

    /// <summary>Identifies the <see cref="SectionPlane"/> dependency property.</summary>
    public static readonly DependencyProperty SectionPlaneProperty =
        DependencyProperty.Register(
            nameof(SectionPlane), typeof(SectionPlane), typeof(WorkbenchViewport),
            new PropertyMetadata(null, OnSectionPlaneChanged));

    /// <summary>Identifies the <see cref="ShowSectionPlaneIndicator"/> dependency property.</summary>
    public static readonly DependencyProperty ShowSectionPlaneIndicatorProperty =
        DependencyProperty.Register(
            nameof(ShowSectionPlaneIndicator), typeof(bool), typeof(WorkbenchViewport),
            new PropertyMetadata(true, OnVisualOptionChanged));

    /// <summary>Identifies the <see cref="ShowGlyphs"/> dependency property.</summary>
    public static readonly DependencyProperty ShowGlyphsProperty =
        DependencyProperty.Register(
            nameof(ShowGlyphs), typeof(bool), typeof(WorkbenchViewport),
            new PropertyMetadata(true, OnVisualOptionChanged));

    /// <summary>Identifies the <see cref="GlyphScale"/> dependency property.</summary>
    public static readonly DependencyProperty GlyphScaleProperty =
        DependencyProperty.Register(
            nameof(GlyphScale), typeof(double), typeof(WorkbenchViewport),
            new PropertyMetadata(1.0, OnVisualOptionChanged),
            v => double.IsFinite((double)v) && (double)v >= 0.0);

    /// <summary>Identifies the <see cref="GlyphStride"/> dependency property.</summary>
    public static readonly DependencyProperty GlyphStrideProperty =
        DependencyProperty.Register(
            nameof(GlyphStride), typeof(int), typeof(WorkbenchViewport),
            new PropertyMetadata(1, OnGlyphDataOptionChanged),
            v => (int)v >= 1);

    /// <summary>Identifies the <see cref="GlyphColorScale"/> dependency property.</summary>
    public static readonly DependencyProperty GlyphColorScaleProperty =
        DependencyProperty.Register(
            nameof(GlyphColorScale), typeof(ColorScale), typeof(WorkbenchViewport),
            new PropertyMetadata(null, OnGlyphColorScaleChanged));

    /// <summary>Identifies the <see cref="EdgeExtractionLimit"/> dependency property.</summary>
    public static readonly DependencyProperty EdgeExtractionLimitProperty =
        DependencyProperty.Register(
            nameof(EdgeExtractionLimit), typeof(int), typeof(WorkbenchViewport),
            new PropertyMetadata(5_000_000, OnGeometryOptionChanged),
            v => (int)v >= 0);

    /// <summary>Identifies the <see cref="InteractiveLodThreshold"/> dependency property.</summary>
    public static readonly DependencyProperty InteractiveLodThresholdProperty =
        DependencyProperty.Register(
            nameof(InteractiveLodThreshold), typeof(int), typeof(WorkbenchViewport),
            new PropertyMetadata(5_000_000, OnGeometryOptionChanged),
            v => (int)v >= 0);

    /// <summary>Identifies the <see cref="IsHoverHighlightEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsHoverHighlightEnabledProperty =
        DependencyProperty.Register(
            nameof(IsHoverHighlightEnabled), typeof(bool), typeof(WorkbenchViewport),
            new PropertyMetadata(true, OnIsHoverHighlightEnabledChanged));

    /// <summary>Identifies the <see cref="RubberBandSelectionMode"/> dependency property.</summary>
    public static readonly DependencyProperty RubberBandSelectionModeProperty =
        DependencyProperty.Register(
            nameof(RubberBandSelectionMode), typeof(ViewportRubberBandSelectionMode), typeof(WorkbenchViewport),
            new PropertyMetadata(ViewportRubberBandSelectionMode.VisibleOnly));

    private static readonly DependencyPropertyKey IsGeometryBuildingPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsGeometryBuilding), typeof(bool), typeof(WorkbenchViewport),
            new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsGeometryBuilding"/> read-only dependency property.</summary>
    public static readonly DependencyProperty IsGeometryBuildingProperty =
        IsGeometryBuildingPropertyKey.DependencyProperty;

    private readonly List<(ViewportMesh Source, GpuMesh Gpu)> _gpuMeshes = [];
    private readonly HashSet<ViewportMesh> _displacementDirtyMeshes = [];
    private readonly HashSet<ViewportMesh> _scalarDirtyMeshes = [];
    private readonly List<RenderItem> _renderItems = [];
    private readonly List<GpuMesh> _visibleGpus = [];
    private readonly List<ViewportMesh> _visibleSources = [];
    private readonly Dictionary<ViewportMesh, GpuSelectionMesh> _selectionGpu = [];
    private readonly Dictionary<ViewportMesh, VoxelMeshBinding> _voxelBindings = [];

    private sealed record VoxelMeshBinding(
        ViewportVoxelGrid Source,
        ViewportVoxelGrid Display,
        ViewportVoxelSurface.Result Surface,
        bool UsedBrickLod);

    private Image? _image;
    private Canvas? _triadCanvas;
    private Canvas? _viewCubeCanvas;
    private ViewCubeOverlay? _viewCube;
    private ViewportRenderer? _renderer;
    private D3DImage? _d3dImage;
    private WriteableBitmap? _softwareBitmap;
    private nint _lastBackBuffer;

    private bool _renderQueued;
    private bool _geometryDirty = true;
    private bool _colorMapDirty = true;
    private bool _selectionDirty = true;
    private bool _glyphDirty = true;
    private bool _hasAutoFitted;
    private bool _renderBroken;
    private int _consecutiveFailures;

    private Bounds3D _localBounds = Bounds3D.Empty;
    private double _originX;
    private double _originY;
    private double _originZ;

    // Mouse interaction state
    private Point _lastMousePosition;
    private bool _isOrbiting;
    private bool _isPanning;
    private bool _isPicking;
    private bool _isRubberBanding;
    private Point _pickStart;

    // Viewpoint interpolation animation
    private bool _isAnimatingView;
    private DateTime _animationStart;
    private double _animStartYaw;
    private double _animStartPitch;
    private double _animDeltaYaw;
    private double _animDeltaPitch;

    // Modal oscillation animation
    private bool _isDeformationTickAttached;
    private DateTime _deformationAnimationStart;
    private float _lastEffectiveDeformationScale = 1.0f;

    // Timing values for statistics API
    private TimeSpan _lastGeometryBuildTime;
    private TimeSpan _lastRenderTime;

    // Async geometry build: generation tracking while displaying previous scene
    private readonly GeometryBuildCoordinator _buildCoordinator = new();
    private bool _fitToViewPending;

    // Hover pre-highlight: full-frame ID buffer captured while idle and its state stamp
    private uint[]? _hoverIdBuffer;
    private bool _hoverIdValid;
    private Matrix4x4 _hoverIdViewProj;
    private float _hoverIdDeformScale;
    private Vector4 _hoverIdClip;
    private int _hoverIdWidth;
    private int _hoverIdHeight;
    private int _hoverIdSceneVersion;
    private int _sceneVersion; // Incremented whenever geometry, displacement, or visibility changes

    // Current hover target and GPU overlay
    private ViewportMesh? _hoverMesh;
    private int _hoverTriangle = -1;
    private int _hoverNode = -1;
    private bool _hoverIsPart;
    private GpuSelectionMesh? _hoverGpu;

    // Interactive LOD: renders LOD chunks during camera interaction or animation, then restores full resolution
    private static readonly TimeSpan LodRestoreDelay = TimeSpan.FromMilliseconds(300);
    private bool _lodRequested;
    private bool _hasAnyLod;
    private DispatcherTimer? _lodRestoreTimer;

    // WPF overlay elements for axis triad and rubber-band rectangle
    private readonly Line[] _triadLines = new Line[3];
    private readonly TextBlock[] _triadLabels = new TextBlock[3];
    private Rectangle? _rubberBand;

    // Annotation overlays
    private readonly List<AnnotationVisual> _annotationVisuals = [];
    private readonly List<ViewportAnnotation> _hookedAnnotations = [];
    private Canvas? _annotationCanvas;

    static WorkbenchViewport()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(WorkbenchViewport), new FrameworkPropertyMetadata(typeof(WorkbenchViewport)));
    }

    /// <summary>Initializes a new instance of the <see cref="WorkbenchViewport"/> class.</summary>
    public WorkbenchViewport()
    {
        Camera = new ViewportCamera();
        Camera.Changed += (_, _) =>
        {
            // Camera interaction (orbit/pan/zoom/view animation) activates interactive LOD
            NotifyInteractiveChange();
            InvalidateViewport();
        };

        Selection = new ViewportSelection();
        Selection.Changed += OnSelectionModelChanged;

        Annotations = [];
        Annotations.CollectionChanged += OnAnnotationsChanged;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>Raised when the selection changes (forwarded from <see cref="Selection"/>).</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>
    /// Progress notification for asynchronous geometry builds, raised on the UI thread.
    /// The previous scene remains interactive while building.
    /// </summary>
    public event EventHandler<ViewportBuildProgressEventArgs>? GeometryBuildProgressChanged;

    /// <summary>
    /// Raised when an asynchronous geometry build finishes and swaps in the new scene.
    /// Suppressed for superseded generations if MeshSource changes again mid-build.
    /// </summary>
    public event EventHandler? GeometryBuildCompleted;

    /// <summary>Raised when the hover pre-highlight target changes.</summary>
    public event EventHandler? HoverChanged;

    /// <summary>Current hover target (null if none), matching the granularity of <see cref="PickMode"/>.</summary>
    public ViewportHoverInfo? HoverInfo { get; private set; }

    /// <summary>
    /// Raised when a <see cref="ViewportPickMode.Probe"/> click hits a mesh.
    /// Unless <see cref="ProbePickedEventArgs.Handled"/> is set to true, an annotation is added automatically
    /// using <see cref="ProbeLabelFormatter"/> or the built-in format. Not raised on background clicks.
    /// </summary>
    public event EventHandler<ProbePickedEventArgs>? ProbePicked;

    /// <summary>
    /// Raised when a <see cref="ViewportPickMode.Cell"/> click hits a voxel cell.
    /// Unless <see cref="VoxelProbePickedEventArgs.Handled"/> is set to true, an annotation is added automatically.
    /// </summary>
    public event EventHandler<VoxelProbePickedEventArgs>? VoxelProbePicked;

    /// <summary>
    /// Custom formatter for probe annotation labels (e.g., to attach physical units).
    /// When null, uses the built-in format ("N{node}: {value:G4}" or node index + coordinates).
    /// </summary>
    public Func<ProbeResult, string>? ProbeLabelFormatter { get; set; }

    /// <summary>Custom formatter for voxel cell probe annotation labels; uses the built-in format when null.</summary>
    public Func<VoxelProbeResult, string>? VoxelProbeLabelFormatter { get; set; }

    /// <summary>
    /// Collection of annotations displayed on the viewport. Populated automatically by probing or managed
    /// directly by the application. Node-bound annotations follow deformation, section cuts, and mesh visibility.
    /// </summary>
    public ObservableCollection<ViewportAnnotation> Annotations { get; }

    /// <summary>Collection of <see cref="ViewportMesh"/> objects to render (typically an ObservableCollection).</summary>
    public IEnumerable? MeshSource
    {
        get => (IEnumerable?)GetValue(MeshSourceProperty);
        set => SetValue(MeshSourceProperty, value);
    }

    /// <summary>
    /// Collection of sparse <see cref="ViewportVoxelGrid"/> objects to render (typically an ObservableCollection).
    /// Outer surfaces are triangulated and rendered through the same pipeline as <see cref="MeshSource"/>.
    /// </summary>
    public IEnumerable? VoxelSource
    {
        get => (IEnumerable?)GetValue(VoxelSourceProperty);
        set => SetValue(VoxelSourceProperty, value);
    }

    /// <summary>Color scale used for contour shading; may be shared directly with a ColorMapLegend.</summary>
    public ColorScale? ColorScale
    {
        get => (ColorScale?)GetValue(ColorScaleProperty);
        set => SetValue(ColorScaleProperty, value);
    }

    /// <summary>Enables or disables contour shading (parts render in solid color when disabled).</summary>
    public bool ShowContours
    {
        get => (bool)GetValue(ShowContoursProperty);
        set => SetValue(ShowContoursProperty, value);
    }

    /// <summary>Global gate for wireframe edge overlays (combined with per-part <see cref="ViewportMesh.ShowEdges"/>).</summary>
    public bool ShowEdges
    {
        get => (bool)GetValue(ShowEdgesProperty);
        set => SetValue(ShowEdgesProperty, value);
    }

    /// <summary>Camera projection mode (perspective or orthographic).</summary>
    public ViewportProjection Projection
    {
        get => (ViewportProjection)GetValue(ProjectionProperty);
        set => SetValue(ProjectionProperty, value);
    }

    /// <summary>Upward axis for turntable rotation (Z-up or Y-up).</summary>
    public ViewportUpAxis UpAxis
    {
        get => (ViewportUpAxis)GetValue(UpAxisProperty);
        set => SetValue(UpAxisProperty, value);
    }

    /// <summary>Gets or sets whether the bottom-left XYZ axis triad overlay is displayed.</summary>
    public bool ShowAxisTriad
    {
        get => (bool)GetValue(ShowAxisTriadProperty);
        set => SetValue(ShowAxisTriadProperty, value);
    }

    /// <summary>Gets or sets whether the clickable top-right ViewCube overlay is displayed.</summary>
    public bool ShowViewCube
    {
        get => (bool)GetValue(ShowViewCubeProperty);
        set => SetValue(ShowViewCubeProperty, value);
    }

    /// <summary>
    /// Left-button picking granularity. When <see cref="ViewportPickMode.None"/> (default), left clicks are ignored.
    /// </summary>
    public ViewportPickMode PickMode
    {
        get => (ViewportPickMode)GetValue(PickModeProperty);
        set => SetValue(PickModeProperty, value);
    }

    /// <summary>
    /// Deformation scale multiplier applied to each mesh's <see cref="ViewportMesh.Displacements"/> in the vertex shader
    /// (0 disables deformation). Use <see cref="GetSuggestedDeformationScale"/> for an auto-scale estimate.
    /// </summary>
    public double DeformationScale
    {
        get => (double)GetValue(DeformationScaleProperty);
        set => SetValue(DeformationScaleProperty, value);
    }

    /// <summary>
    /// Enables sinusoidal modal oscillation animation, scaling deformation by DeformationScale * sin(2*pi*t/T).
    /// Note that this triggers per-frame rendering as an exception to on-demand redraws.
    /// </summary>
    public bool IsDeformationAnimated
    {
        get => (bool)GetValue(IsDeformationAnimatedProperty);
        set => SetValue(IsDeformationAnimatedProperty, value);
    }

    /// <summary>Period of the sinusoidal deformation animation (default 1 second).</summary>
    public TimeSpan DeformationAnimationPeriod
    {
        get => (TimeSpan)GetValue(DeformationAnimationPeriodProperty);
        set => SetValue(DeformationAnimationPeriodProperty, value);
    }

    /// <summary>Overlays a semi-transparent wireframe of the undeformed geometry.</summary>
    public bool ShowUndeformedWireframe
    {
        get => (bool)GetValue(ShowUndeformedWireframeProperty);
        set => SetValue(ShowUndeformedWireframeProperty, value);
    }

    /// <summary>
    /// Clipping plane for section cuts (null disables clipping). The positive normal side remains visible.
    /// Meshes with <see cref="ViewportMesh.IsClippable"/> = false remain unclipped (useful for slice overlays or fixtures).
    /// Property changes on the plane update the viewport automatically.
    /// </summary>
    public SectionPlane? SectionPlane
    {
        get => (SectionPlane?)GetValue(SectionPlaneProperty);
        set => SetValue(SectionPlaneProperty, value);
    }

    /// <summary>
    /// Shows a semi-transparent quad and outline indicator for <see cref="SectionPlane"/> (nothing drawn when null). Default is true.
    /// </summary>
    public bool ShowSectionPlaneIndicator
    {
        get => (bool)GetValue(ShowSectionPlaneIndicatorProperty);
        set => SetValue(ShowSectionPlaneIndicatorProperty, value);
    }

    /// <summary>Camera controller; applications can manipulate or save/restore viewpoints directly.</summary>
    public ViewportCamera Camera { get; }

    /// <summary>
    /// Selection state model updated by picking and reflected automatically when mutated programmatically.
    /// </summary>
    public ViewportSelection Selection { get; }

    /// <summary>Gets whether the renderer is using the WARP software rasterizer.</summary>
    public bool IsSoftwareRendering => _renderer?.IsSoftwareRendering ?? false;

    /// <summary>
    /// Fits the camera to enclose all meshes. If an asynchronous geometry build is pending or in progress,
    /// fits automatically once the new bounds are ready.
    /// </summary>
    public void FitToView()
    {
        if (_geometryDirty || IsGeometryBuilding)
        {
            _fitToViewPending = true;
            InvalidateViewport();
            return;
        }

        if (!_localBounds.IsEmpty)
        {
            Camera.FitToBounds(_localBounds);
        }
    }

    /// <summary>
    /// Enables or disables vector glyph (arrow) rendering for meshes/grids with vector fields. Default is true.
    /// </summary>
    public bool ShowGlyphs
    {
        get => (bool)GetValue(ShowGlyphsProperty);
        set => SetValue(ShowGlyphsProperty, value);
    }

    /// <summary>
    /// Glyph length multiplier (arrow length = |v| * GlyphScale, thickness scaled proportionally).
    /// Use <see cref="GetSuggestedGlyphScale"/> for an auto-scale estimate; 0 hides glyphs.
    /// </summary>
    public double GlyphScale
    {
        get => (double)GetValue(GlyphScaleProperty);
        set => SetValue(GlyphScaleProperty, value);
    }

    /// <summary>
    /// Subsampling stride for vector glyphs (places one arrow every n nodes; default 1 = every node).
    /// Prevents clutter on dense meshes.
    /// </summary>
    public int GlyphStride
    {
        get => (int)GetValue(GlyphStrideProperty);
        set => SetValue(GlyphStrideProperty, value);
    }

    /// <summary>
    /// Color scale mapping vector magnitude |v| to glyph color, independent of the contour <see cref="ColorScale"/>.
    /// Falls back to the theme accent color when null.
    /// </summary>
    public ColorScale? GlyphColorScale
    {
        get => (ColorScale?)GetValue(GlyphColorScaleProperty);
        set => SetValue(GlyphColorScaleProperty, value);
    }

    /// <summary>
    /// Maximum triangle count for which wireframe edges are extracted (default 5M).
    /// Meshes exceeding this limit skip edge extraction to prevent excessive memory usage;
    /// check <see cref="GetStatistics"/> for skipped counts. Set to <see cref="int.MaxValue"/> to disable the limit.
    /// </summary>
    public int EdgeExtractionLimit
    {
        get => (int)GetValue(EdgeExtractionLimitProperty);
        set => SetValue(EdgeExtractionLimitProperty, value);
    }

    /// <summary>
    /// Triangle-count threshold above which an interactive ~1/20 LOD mesh is built via grid vertex clustering
    /// (default 5M). During camera interaction or deformation animation, LOD chunks render without edges, glyphs,
    /// or undeformed wireframes while preserving contours, deformation, and section cuts. Picking and probing always
    /// use full-resolution meshes. Set to <see cref="int.MaxValue"/> to disable LOD.
    /// </summary>
    public int InteractiveLodThreshold
    {
        get => (int)GetValue(InteractiveLodThresholdProperty);
        set => SetValue(InteractiveLodThresholdProperty, value);
    }

    /// <summary>
    /// Enables hover pre-highlighting (default true) when <see cref="PickMode"/> is not None.
    /// Caches the ID buffer once while the scene is idle for near-zero pointer-move cost; disabled during camera motion.
    /// </summary>
    public bool IsHoverHighlightEnabled
    {
        get => (bool)GetValue(IsHoverHighlightEnabledProperty);
        set => SetValue(IsHoverHighlightEnabledProperty, value);
    }

    /// <summary>
    /// Scope of rubber-band rectangle selection (default <see cref="ViewportRubberBandSelectionMode.VisibleOnly"/>).
    /// <see cref="ViewportRubberBandSelectionMode.Through"/> selects occluded elements via parallel CPU screen projection.
    /// </summary>
    public ViewportRubberBandSelectionMode RubberBandSelectionMode
    {
        get => (ViewportRubberBandSelectionMode)GetValue(RubberBandSelectionModeProperty);
        set => SetValue(RubberBandSelectionModeProperty, value);
    }

    /// <summary>
    /// Gets whether geometry is currently being built on a background thread (read-only).
    /// </summary>
    public bool IsGeometryBuilding => (bool)GetValue(IsGeometryBuildingProperty);

    /// <summary>
    /// Returns a snapshot of current scene and render statistics (returns zeroed values before the first build).
    /// </summary>
    public ViewportStatistics GetStatistics()
    {
        long triangles = 0;
        long vertices = 0;
        long lodTriangles = 0;
        var chunks = 0;
        var edgeSkipped = 0;
        foreach (var (_, gpu) in _gpuMeshes)
        {
            triangles += gpu.TriangleCount;
            vertices += gpu.VertexCount;
            lodTriangles += gpu.LodTriangleCount;
            chunks += gpu.Chunks.Count;
            if (gpu.EdgesSkipped)
            {
                edgeSkipped++;
            }
        }

        return new ViewportStatistics(
            triangles, vertices, _gpuMeshes.Count, chunks, edgeSkipped,
            lodTriangles,
            _renderer?.LastFrameUsedLod ?? false,
            _renderer?.LastDrawnChunkCount ?? 0,
            _lastGeometryBuildTime, _lastRenderTime);
    }

    // ================= Interactive LOD =================

    /// <summary>
    /// Notifies the viewport that camera interaction or animation occurred, switching to LOD rendering
    /// if available and scheduling a return to full resolution after <see cref="LodRestoreDelay"/> of inactivity.
    /// </summary>
    private void NotifyInteractiveChange()
    {
        if (!_hasAnyLod)
        {
            return;
        }

        _lodRequested = true;

        if (_lodRestoreTimer is null)
        {
            _lodRestoreTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = LodRestoreDelay,
            };
            _lodRestoreTimer.Tick += (_, _) =>
            {
                _lodRestoreTimer!.Stop();
                _lodRequested = false;
                InvalidateViewport(); // Redraw at full resolution
            };
        }

        // Reset the restore timer as long as interaction continues
        _lodRestoreTimer.Stop();
        _lodRestoreTimer.Start();
    }

    /// <summary>
    /// Returns a suggested deformation scale so the maximum displacement spans 5% (or <paramref name="targetFraction"/>)
    /// of the model bounding-box diagonal (returns 1.0 if no displacements are present).
    /// </summary>
    public double GetSuggestedDeformationScale(double targetFraction = ViewportDeformation.DefaultTargetFraction)
    {
        var meshes = MeshSource?.OfType<ViewportMesh>() ?? [];
        var bounds = Bounds3D.Empty;
        var maxDisplacement = 0.0;
        foreach (var mesh in meshes)
        {
            bounds = bounds.Union(ViewportGeometry.ComputeBounds(mesh.Positions));
            maxDisplacement = Math.Max(
                maxDisplacement, ViewportDeformation.GetMaxDisplacementMagnitude(mesh.Displacements));
        }

        var diagonal = bounds.IsEmpty
            ? 0.0
            : Math.Sqrt(
                (bounds.MaxX - bounds.MinX) * (bounds.MaxX - bounds.MinX)
                + (bounds.MaxY - bounds.MinY) * (bounds.MaxY - bounds.MinY)
                + (bounds.MaxZ - bounds.MinZ) * (bounds.MaxZ - bounds.MinZ));

        return ViewportDeformation.ComputeSuggestedScale(maxDisplacement, diagonal, targetFraction);
    }

    /// <summary>
    /// Returns a suggested glyph scale so the longest vector arrow spans 5% (or <paramref name="targetFraction"/>)
    /// of the model bounding-box diagonal (returns 1.0 if no vector fields are present).
    /// </summary>
    public double GetSuggestedGlyphScale(double targetFraction = ViewportDeformation.DefaultTargetFraction)
    {
        var bounds = Bounds3D.Empty;
        var maxMagnitude = 0.0;

        foreach (var mesh in MeshSource?.OfType<ViewportMesh>() ?? [])
        {
            bounds = bounds.Union(ViewportGeometry.ComputeBounds(mesh.Positions));
            maxMagnitude = Math.Max(
                maxMagnitude, ViewportDeformation.GetMaxDisplacementMagnitude(mesh.VectorValues));
        }

        // Also include voxel grids for voxel-only scenes
        foreach (var voxel in VoxelSource?.OfType<ViewportVoxelGrid>() ?? [])
        {
            if (!voxel.IsVisible)
            {
                continue;
            }

            bounds = bounds.Union(voxel.ComputeBounds());
            maxMagnitude = Math.Max(
                maxMagnitude, ViewportDeformation.GetMaxDisplacementMagnitude(voxel.VectorValues));
        }

        var diagonal = bounds.IsEmpty
            ? 0.0
            : Math.Sqrt(
                (bounds.MaxX - bounds.MinX) * (bounds.MaxX - bounds.MinX)
                + (bounds.MaxY - bounds.MinY) * (bounds.MaxY - bounds.MinY)
                + (bounds.MaxZ - bounds.MinZ) * (bounds.MaxZ - bounds.MinZ));

        return ViewportDeformation.ComputeSuggestedScale(maxMagnitude, diagonal, targetFraction);
    }

    /// <summary>
    /// Switches to a standard viewpoint, using a smooth 150 ms orientation transition by default
    /// without altering target or distance.
    /// </summary>
    public void SetStandardView(ViewportStandardView view, bool animate = true)
    {
        var (yaw, pitch) = ViewportCamera.GetStandardViewAngles(view, UpAxis);
        AnimateOrientationTo(yaw, pitch, animate);
    }

    private void AnimateOrientationTo(double targetYaw, double targetPitch, bool animate)
    {
        StopViewAnimation();

        if (!animate || !IsLoaded)
        {
            Camera.SetOrientation(targetYaw, targetPitch);
            return;
        }

        _animStartYaw = Camera.Yaw;
        _animStartPitch = Camera.Pitch;
        _animDeltaYaw = NormalizeSignedAngle(targetYaw - _animStartYaw); // Shortest path
        _animDeltaPitch = targetPitch - _animStartPitch;
        _animationStart = DateTime.UtcNow;
        _isAnimatingView = true;
        CompositionTarget.Rendering += OnViewAnimationTick;
    }

    private void OnViewAnimationTick(object? sender, EventArgs e)
    {
        var t = (DateTime.UtcNow - _animationStart).TotalMilliseconds / ViewAnimationDurationMs;
        if (t >= 1.0)
        {
            Camera.SetOrientation(_animStartYaw + _animDeltaYaw, _animStartPitch + _animDeltaPitch);
            StopViewAnimation();
            return;
        }

        var eased = 1.0 - Math.Pow(1.0 - t, 3.0); // ease-out cubic
        Camera.SetOrientation(_animStartYaw + _animDeltaYaw * eased, _animStartPitch + _animDeltaPitch * eased);
    }

    private void StopViewAnimation()
    {
        if (_isAnimatingView)
        {
            _isAnimatingView = false;
            CompositionTarget.Rendering -= OnViewAnimationTick;
        }
    }

    // ================= Modal oscillation animation =================

    private static void OnIsDeformationAnimatedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((WorkbenchViewport)d).UpdateDeformationAnimation();

    private void UpdateDeformationAnimation()
    {
        var shouldRun = IsDeformationAnimated && IsLoaded;
        if (shouldRun && !_isDeformationTickAttached)
        {
            _deformationAnimationStart = DateTime.UtcNow;
            _isDeformationTickAttached = true;
            CompositionTarget.Rendering += OnDeformationAnimationTick;
        }
        else if (!shouldRun && _isDeformationTickAttached)
        {
            _isDeformationTickAttached = false;
            CompositionTarget.Rendering -= OnDeformationAnimationTick;
        }

        // When stopped, restore static deformation display (factor = 1)
        InvalidateViewport();
    }

    private void OnDeformationAnimationTick(object? sender, EventArgs e)
    {
        // Oscillation renders every frame, so switch large meshes to LOD during playback
        NotifyInteractiveChange();
        InvalidateViewport();
    }

    /// <summary>Current effective glyph scale (0 when ShowGlyphs is false to skip rendering).</summary>
    private float GetEffectiveGlyphScale() => ShowGlyphs ? (float)GlyphScale : 0.0f;

    /// <summary>Current effective deformation scale including the sinusoidal animation factor.</summary>
    private float GetEffectiveDeformationScale()
    {
        var scale = DeformationScale;
        if (_isDeformationTickAttached)
        {
            scale *= ViewportDeformation.GetAnimationFactor(
                (DateTime.UtcNow - _deformationAnimationStart).TotalSeconds,
                DeformationAnimationPeriod.TotalSeconds);
        }

        return (float)scale;
    }

    /// <summary>Normalizes an angular difference into [-pi, pi] for shortest-path yaw interpolation.</summary>
    private static double NormalizeSignedAngle(double angle)
    {
        var twoPi = 2.0 * Math.PI;
        angle %= twoPi;
        return angle switch
        {
            > Math.PI => angle - twoPi,
            < -Math.PI => angle + twoPi,
            _ => angle,
        };
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new WorkbenchViewportAutomationPeer(this);

    private sealed class WorkbenchViewportAutomationPeer(WorkbenchViewport owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(WorkbenchViewport);

        protected override AutomationControlType GetAutomationControlTypeCore() =>
            AutomationControlType.Pane;
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _image = GetTemplateChild(PartImage) as Image;
        _triadCanvas = GetTemplateChild(PartTriadCanvas) as Canvas;
        _annotationCanvas = GetTemplateChild(PartAnnotationCanvas) as Canvas;
        _viewCubeCanvas = GetTemplateChild(PartViewCubeCanvas) as Canvas;
        SetupTriadOverlay();
        RebuildAnnotationVisuals();
        SetupViewCube();
        InvalidateViewport();
    }

    private void SetupViewCube()
    {
        if (_viewCubeCanvas is null)
        {
            _viewCube = null;
            return;
        }

        _viewCubeCanvas.Children.Clear();
        _viewCube = new ViewCubeOverlay(_viewCubeCanvas);
        _viewCube.OrientationRequested += (yaw, pitch) => AnimateOrientationTo(yaw, pitch, animate: true);
    }

    // ================= Render scheduling =================

    /// <summary>Schedules an asynchronous viewport redraw (coalesced on the Dispatcher at Render priority).</summary>
    public void InvalidateViewport()
    {
        if (_renderQueued)
        {
            return;
        }

        _renderQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
        {
            _renderQueued = false;
            RenderFrame();
        });
    }

    /// <inheritdoc />
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        InvalidateViewport();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ThemeManager.ThemeChanged += OnThemeChanged;
        _renderBroken = false;
        UpdateDeformationAnimation();
        InvalidateViewport();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ThemeManager.ThemeChanged -= OnThemeChanged;
        StopViewAnimation();
        UpdateDeformationAnimation();
        CancelPicking();
        _lodRestoreTimer?.Stop();
        _lodRequested = false;
        ReleaseRenderer();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        // Rebuild glyphs on theme change since fallback color is baked into instance buffers
        _glyphDirty = true;
        InvalidateViewport();
    }

    private void RenderFrame()
    {
        if (!IsLoaded || _image is null || _renderBroken)
        {
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        var pixelWidth = (int)Math.Ceiling(ActualWidth * dpi.DpiScaleX);
        var pixelHeight = (int)Math.Ceiling(ActualHeight * dpi.DpiScaleY);
        if (pixelWidth <= 0 || pixelHeight <= 0)
        {
            return;
        }

        try
        {
            _renderer ??= new ViewportRenderer();
            var sizeChanged = pixelWidth != _renderer.Width || pixelHeight != _renderer.Height;
            _renderer.Resize(pixelWidth, pixelHeight);

            EnsureGeometryAsync();
            EnsureDisplacements();
            EnsureScalars();
            EnsureGlyphs();
            EnsureColorMap();
            EnsureSelectionBuffers();
            BuildRenderLists();

            var aspect = (double)pixelWidth / pixelHeight;
            var viewProj = Camera.GetViewMatrix() * Camera.GetProjectionMatrix(aspect);
            var contour = BuildContourSettings();

            var background = GetTokenColor("Wb.Color.Surface.Window", Color.FromRgb(0x1E, 0x1E, 0x1E));
            var edgeColor = GetTokenColor("Wb.Color.Text.Muted", Color.FromRgb(0xC5, 0xC5, 0xC5));
            var accent = GetTokenColor("Wb.Color.Accent.Default", Color.FromRgb(0x00, 0x7A, 0xCC));
            var highlightColor = ToVector4(accent, 0.55);
            var nodeColor = ToVector4(accent, 1.0);
            var hoverColor = ToVector4(accent, 0.30); // Hover highlight is subtler than selection
            var pointSize = (float)(7.0 * dpi.DpiScaleX);

            // Preserve effective deformation scale so PickPixel matches the most recent frame
            _lastEffectiveDeformationScale = GetEffectiveDeformationScale();

            // Section cut: compute shader clip coefficients and indicator geometry
            var clipPlane = GetCurrentClipPlane();
            float[]? sectionIndicator = null;
            if (ShowSectionPlaneIndicator && clipPlane != ViewportSection.DisabledClip)
            {
                sectionIndicator = ViewportSection.BuildIndicatorVertices(
                    clipPlane, (float)_localBounds.Radius);
            }

            var sectionFill = ToVector4(accent, 0.10);
            var sectionLine = ToVector4(accent, 0.70);

            // Measure CPU-side frame render time (including command submission, Flush, and software readback)
            var renderTimer = System.Diagnostics.Stopwatch.StartNew();
            if (_renderer.CanUseD3DImage)
            {
                PresentViaD3DImage(sizeChanged, viewProj, contour, background, edgeColor,
                    highlightColor, nodeColor, hoverColor, pointSize,
                    clipPlane, sectionIndicator, sectionFill, sectionLine);
            }
            else
            {
                PresentViaWriteableBitmap(sizeChanged, viewProj, contour, background, edgeColor,
                    highlightColor, nodeColor, hoverColor, pointSize,
                    clipPlane, sectionIndicator, sectionFill, sectionLine);
            }

            _lastRenderTime = renderTimer.Elapsed;

            UpdateTriadOverlay();
            UpdateAnnotationOverlay(in viewProj, pixelWidth, pixelHeight, dpi.DpiScaleX, clipPlane);
            _viewCube?.Update(Camera, ActualWidth, ActualHeight, ShowViewCube);
            _consecutiveFailures = 0;
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            // Device lost or similar; dispose the broken renderer and retry on the next render request,
            // halting after repeated failures to avoid an infinite loop.
            ReleaseRenderer();
            if (++_consecutiveFailures >= 2)
            {
                _renderBroken = true;
            }
            else
            {
                InvalidateViewport();
            }
        }
    }

    private void PresentViaD3DImage(
        bool sizeChanged, Matrix4x4 viewProj, ContourSettings contour, Color background, Color edgeColor,
        Vector4 highlightColor, Vector4 nodeColor, Vector4 hoverColor, float pointSize,
        Vector4 clipPlane, float[]? sectionIndicator, Vector4 sectionFill, Vector4 sectionLine)
    {
        if (_d3dImage is null)
        {
            _d3dImage = new D3DImage();
            _d3dImage.IsFrontBufferAvailableChanged += (_, _) =>
            {
                // Rebind back buffer when front buffer recovers (WPF may temporarily release it during theme switches)
                _lastBackBuffer = 0;
                InvalidateViewport();
            };
            _softwareBitmap = null;
            _lastBackBuffer = 0;
        }

        if (!ReferenceEquals(_image!.Source, _d3dImage))
        {
            _image.Source = _d3dImage;
        }

        _d3dImage.Lock();
        try
        {
            var surface = _renderer!.BackBufferSurface;
            if (sizeChanged || surface != _lastBackBuffer)
            {
                _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface, enableSoftwareFallback: true);
                _lastBackBuffer = surface;
            }

            _renderer.Render(
                _renderItems, in viewProj, Camera.GetEyeDirection(),
                ToVector4(background, 1.0), in contour, ShowContours, ToVector4(edgeColor, 1.0),
                highlightColor, nodeColor, pointSize, hoverColor,
                _lastEffectiveDeformationScale, GetEffectiveGlyphScale(),
                ShowUndeformedWireframe, ToVector4(edgeColor, 0.35),
                clipPlane, sectionIndicator, sectionFill, sectionLine,
                useLod: _lodRequested);

            _d3dImage.AddDirtyRect(new Int32Rect(0, 0, _renderer.Width, _renderer.Height));
        }
        finally
        {
            _d3dImage.Unlock();
        }
    }

    private void PresentViaWriteableBitmap(
        bool sizeChanged, Matrix4x4 viewProj, ContourSettings contour, Color background, Color edgeColor,
        Vector4 highlightColor, Vector4 nodeColor, Vector4 hoverColor, float pointSize,
        Vector4 clipPlane, float[]? sectionIndicator, Vector4 sectionFill, Vector4 sectionLine)
    {
        _renderer!.Render(
            _renderItems, in viewProj, Camera.GetEyeDirection(),
            ToVector4(background, 1.0), in contour, ShowContours, ToVector4(edgeColor, 1.0),
            highlightColor, nodeColor, pointSize, hoverColor,
            _lastEffectiveDeformationScale, GetEffectiveGlyphScale(),
            ShowUndeformedWireframe, ToVector4(edgeColor, 0.35),
            clipPlane, sectionIndicator, sectionFill, sectionLine,
            useLod: _lodRequested);

        if (_softwareBitmap is null || sizeChanged)
        {
            _softwareBitmap = new WriteableBitmap(
                _renderer.Width, _renderer.Height, 96, 96, PixelFormats.Bgra32, null);
            _d3dImage = null;
        }

        if (!ReferenceEquals(_image!.Source, _softwareBitmap))
        {
            _image.Source = _softwareBitmap;
        }

        _softwareBitmap.Lock();
        try
        {
            _renderer.ReadPixels(_softwareBitmap.BackBuffer, _softwareBitmap.BackBufferStride);
            _softwareBitmap.AddDirtyRect(new Int32Rect(0, 0, _renderer.Width, _renderer.Height));
        }
        finally
        {
            _softwareBitmap.Unlock();
        }
    }

    // ================= Geometry / ColorMap synchronization =================

    /// <summary>Result of a single asynchronous geometry build (assembled on a background thread, applied on UI).</summary>
    private sealed record GeometryBuildResult(
        List<(ViewportMesh Source, GpuMesh Gpu)> Meshes,
        List<ViewportMesh> Sources,
        Bounds3D LocalBounds,
        double OriginX,
        double OriginY,
        double OriginZ,
        TimeSpan BuildTime);

    /// <summary>
    /// Starts an asynchronous geometry build if geometry is dirty.
    /// Keeps the previous scene visible, interactive, and pickable during construction, swapping atomically on completion.
    /// If geometry changes again mid-build, generation tracking cancels and discards the superseded build.
    /// </summary>
    private async void EnsureGeometryAsync()
    {
        // Retry on the next RenderFrame if the renderer is not yet initialized
        if (_renderer is null || !_geometryDirty)
        {
            return;
        }

        _geometryDirty = false;
        var (generation, token) = _buildCoordinator.Begin();
        var renderer = _renderer;
        var meshes = MeshSource?.OfType<ViewportMesh>().ToList() ?? [];
        var voxels = VoxelSource?.OfType<ViewportVoxelGrid>().ToList() ?? [];
        var edgeLimit = EdgeExtractionLimit;
        var lodThreshold = InteractiveLodThreshold;

        SetValue(IsGeometryBuildingPropertyKey, true);
        GeometryBuildProgressChanged?.Invoke(this, new ViewportBuildProgressEventArgs("準備", 0.0));

        // Hold an AddRef'd device lease so background CreateBuffer calls stay safe if the renderer is disposed mid-build;
        // RentDevice adds a reference balanced by the using statement.
        using var lease = renderer.AcquireDeviceLease();
        using var device = lease.RentDevice();

        GeometryBuildResult? result = null;
        Dictionary<ViewportMesh, VoxelMeshBinding>? voxelBindings = null;
        try
        {
            // Pass ComPtr by value (ownership stays with device/lease)
            var deviceForBuild = device;
            (result, voxelBindings) = await Task.Run(
                () =>
                {
                    var bindings = new Dictionary<ViewportMesh, VoxelMeshBinding>();
                    var combined = new List<ViewportMesh>(meshes.Count + voxels.Count);
                    combined.AddRange(meshes);
                    foreach (var voxel in voxels)
                    {
                        token.ThrowIfCancellationRequested();
                        if (!voxel.IsVisible || voxel.ActiveCount == 0)
                        {
                            continue;
                        }

                        var usedBrick = voxel.ActiveCount > ViewportVoxelLod.DefaultActiveThreshold;
                        var display = ViewportVoxelLod.CreateDisplayGrid(voxel);
                        var surface = ViewportVoxelSurface.Extract(display);
                        if (surface.TriangleIndices.Length == 0)
                        {
                            continue;
                        }

                        var mesh = ViewportVoxelSurface.ToMesh(display, surface);
                        bindings[mesh] = new VoxelMeshBinding(voxel, display, surface, usedBrick);
                        combined.Add(mesh);
                    }

                    var geo = BuildGeometryCore(
                        deviceForBuild, combined, edgeLimit, lodThreshold, token,
                        (stage, progress) => ReportBuildProgress(generation, stage, progress));
                    return (geo, bindings);
                },
                CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer generation or unloaded; cleanup is handled below
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            // Device-level build failure; recreate the renderer as in RenderFrame
            if (_buildCoordinator.IsCurrent(generation))
            {
                SetValue(IsGeometryBuildingPropertyKey, false);
                // Dispose partial build resources before releasing the device
                DisposeBuildResult(result);
                ReleaseRenderer();
                if (++_consecutiveFailures >= 2)
                {
                    _renderBroken = true;
                }
                else
                {
                    InvalidateViewport();
                }
            }
            else
            {
                DisposeBuildResult(result);
            }

            return;
        }

        // Discard results from superseded or unloaded generations
        if (!_buildCoordinator.IsCurrent(generation) || !ReferenceEquals(renderer, _renderer))
        {
            DisposeBuildResult(result);
            return;
        }

        SetValue(IsGeometryBuildingPropertyKey, false);
        if (result is null)
        {
            return;
        }

        ApplyBuildResult(result, voxelBindings);
        GeometryBuildCompleted?.Invoke(this, EventArgs.Empty);
        InvalidateViewport();
    }

    /// <summary>
    /// Core background build routine. Uses only read-only <see cref="ViewportMesh"/> array references
    /// and the AddRef'd device without touching WPF objects. Checks cancellation at chunk boundaries.
    /// </summary>
    private static GeometryBuildResult BuildGeometryCore(
        Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Device> device,
        List<ViewportMesh> meshes, int edgeExtractionLimit, int interactiveLodThreshold,
        CancellationToken token, Action<string, double> reportProgress)
    {
        var buildTimer = System.Diagnostics.Stopwatch.StartNew();
        var built = new List<(ViewportMesh Source, GpuMesh Gpu)>(meshes.Count);
        try
        {
            var bounds = Bounds3D.Empty;
            var meshBounds = new List<Bounds3D>(meshes.Count);
            foreach (var mesh in meshes)
            {
                token.ThrowIfCancellationRequested();
                var b = ViewportGeometry.ComputeBounds(mesh.Positions);
                meshBounds.Add(b);
                bounds = bounds.Union(b);
            }

            if (bounds.IsEmpty)
            {
                return new GeometryBuildResult(built, meshes, Bounds3D.Empty, 0.0, 0.0, 0.0, buildTimer.Elapsed);
            }

            // Recenter around scene center to prevent float precision jitter on large coordinates
            var (ox, oy, oz) = (bounds.CenterX, bounds.CenterY, bounds.CenterZ);
            var localBounds = new Bounds3D(
                bounds.MinX - ox, bounds.MinY - oy, bounds.MinZ - oz,
                bounds.MaxX - ox, bounds.MaxY - oy, bounds.MaxZ - oz);

            for (var i = 0; i < meshes.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                var stage = meshes.Count == 1
                    ? "ジオメトリ構築"
                    : FormattableString.Invariant($"ジオメトリ構築 ({i + 1}/{meshes.Count})");
                var meshIndex = i;
                var gpu = GpuMesh.Create(
                    device, meshes[i], ox, oy, oz, edgeExtractionLimit, interactiveLodThreshold,
                    meshBounds[i],
                    cancellationToken: token,
                    progress: p => reportProgress(stage, (meshIndex + p) / meshes.Count));
                if (gpu is not null)
                {
                    built.Add((meshes[i], gpu));
                }
            }

            return new GeometryBuildResult(built, meshes, localBounds, ox, oy, oz, buildTimer.Elapsed);
        }
        catch
        {
            foreach (var (_, gpu) in built)
            {
                gpu.Dispose();
            }

            throw;
        }
    }

    /// <summary>Forwards progress from the background thread to the UI thread, discarding stale generations.</summary>
    private void ReportBuildProgress(int generation, string stage, double progress) =>
        Dispatcher.BeginInvoke(() =>
        {
            if (_buildCoordinator.IsCurrent(generation))
            {
                GeometryBuildProgressChanged?.Invoke(this, new ViewportBuildProgressEventArgs(stage, progress));
            }
        });

    /// <summary>Swaps the completed build result into the active scene on the UI thread.</summary>
    private void ApplyBuildResult(
        GeometryBuildResult result, Dictionary<ViewportMesh, VoxelMeshBinding>? voxelBindings = null)
    {
        foreach (var (_, gpu) in _gpuMeshes)
        {
            gpu.Dispose();
        }

        _gpuMeshes.Clear();
        _gpuMeshes.AddRange(result.Meshes);

        _voxelBindings.Clear();
        if (voxelBindings is not null)
        {
            foreach (var (mesh, binding) in voxelBindings)
            {
                _voxelBindings[mesh] = binding;
            }
        }

        // Prune selection/hover for removed parts and rebuild selection/glyph buffers.
        // Any displacements dirtied during the build remain in _displacementDirtyMeshes for the next EnsureDisplacements.
        Selection.PruneTo(result.Sources);
        _selectionDirty = true;
        _glyphDirty = true;
        ClearHover();
        _sceneVersion++;

        (_originX, _originY, _originZ) = (result.OriginX, result.OriginY, result.OriginZ);
        _localBounds = result.LocalBounds;
        _lastGeometryBuildTime = result.BuildTime;

        _hasAnyLod = _gpuMeshes.Any(pair => pair.Gpu.HasLod);
        if (!_hasAnyLod)
        {
            _lodRequested = false;
            _lodRestoreTimer?.Stop();
        }

        if (!_localBounds.IsEmpty && (!_hasAutoFitted || _fitToViewPending))
        {
            _hasAutoFitted = true;
            _fitToViewPending = false;
            Camera.FitToBounds(_localBounds);
        }
    }

    private static void DisposeBuildResult(GeometryBuildResult? result)
    {
        if (result is null)
        {
            return;
        }

        foreach (var (_, gpu) in result.Meshes)
        {
            gpu.Dispose();
        }

        result.Meshes.Clear();
    }

    /// <summary>
    /// Updates displacement buffers only for meshes whose Displacements changed,
    /// avoiding full geometry rebuilds during transient playback.
    /// </summary>
    private void EnsureDisplacements()
    {
        if (_displacementDirtyMeshes.Count == 0 || _renderer is null)
        {
            return;
        }

        foreach (var (source, gpu) in _gpuMeshes)
        {
            if (_displacementDirtyMeshes.Contains(source))
            {
                _renderer.UpdateMeshDisplacements(gpu, source.Displacements);
            }
        }

        _displacementDirtyMeshes.Clear();
    }

    /// <summary>
    /// Updates only vertex scalar attributes for meshes whose ScalarValues changed.
    /// For voxel grids, re-averages brick scalars if needed and expands to outer-surface vertices
    /// without re-extracting faces or bricks.
    /// </summary>
    private void EnsureScalars()
    {
        if (_scalarDirtyMeshes.Count == 0 || _renderer is null)
        {
            return;
        }

        foreach (var (source, gpu) in _gpuMeshes)
        {
            if (!_scalarDirtyMeshes.Contains(source))
            {
                continue;
            }

            if (!gpu.SupportsScalarUpdate)
            {
                // Built without scalars (immutable VB); trigger full rebuild on first scalar assignment
                _geometryDirty = true;
                continue;
            }

            double[]? vertexScalars;
            if (_voxelBindings.TryGetValue(source, out var binding))
            {
                var cellScalars = binding.UsedBrickLod
                    ? ViewportVoxelLod.ReaverageScalarsOntoDisplay(
                        binding.Source, binding.Display, ViewportVoxelLod.DefaultBrickSize)
                    : binding.Source.ScalarValues;
                if (!ReferenceEquals(binding.Source, binding.Display))
                {
                    binding.Display.SyncScalarValuesWithoutNotify(cellScalars);
                }

                vertexScalars = ViewportVoxelSurface.BuildVertexScalars(binding.Surface, cellScalars);
                source.SyncScalarValuesWithoutNotify(vertexScalars);
            }
            else
            {
                vertexScalars = source.ScalarValues;
            }

            _renderer.UpdateMeshScalars(gpu, vertexScalars);
        }

        _scalarDirtyMeshes.Clear();
    }

    /// <summary>
    /// Synchronizes glyph instance buffers with mesh and voxel vector fields
    /// (rebuilt on VectorValues, Displacements, GlyphStride, GlyphColorScale, or theme changes).
    /// </summary>
    private void EnsureGlyphs()
    {
        if (!_glyphDirty || _renderer is null)
        {
            return;
        }

        _glyphDirty = false;
        var accent = GetTokenColor("Wb.Color.Accent.Default", Color.FromRgb(0x00, 0x7A, 0xCC));
        var fallback = ToVector4(accent, 1.0);
        foreach (var (source, gpu) in _gpuMeshes)
        {
            float[] data;
            int count;
            if (_voxelBindings.TryGetValue(source, out var binding)
                && binding.Source.VectorValues is not null)
            {
                // Voxel grid: cell-center glyphs; scale effective stride when brick LOD is active
                var stride = ViewportVoxelLod.EffectiveGlyphStride(
                    GlyphStride, ViewportVoxelLod.DefaultBrickSize, binding.UsedBrickLod);
                data = ViewportGlyph.BuildVoxelInstances(
                    binding.Source, stride, _originX, _originY, _originZ,
                    GlyphColorScale, fallback, out count);
            }
            else
            {
                data = ViewportGlyph.BuildInstances(
                    source, GlyphStride, _originX, _originY, _originZ,
                    GlyphColorScale, fallback, out count);
            }

            _renderer.UpdateMeshGlyphs(gpu, data, count);
        }
    }

    /// <summary>Synchronizes selection highlight GPU buffers with <see cref="Selection"/>.</summary>
    private void EnsureSelectionBuffers()
    {
        if (!_selectionDirty || _renderer is null)
        {
            return;
        }

        _selectionDirty = false;
        foreach (var gpu in _selectionGpu.Values)
        {
            gpu.Dispose();
        }

        _selectionGpu.Clear();

        foreach (var (source, _) in _gpuMeshes)
        {
            var faces = Selection.GetSelectedFaces(source);
            var nodes = Selection.GetSelectedNodes(source);
            if (faces.Count == 0 && nodes.Count == 0)
            {
                continue;
            }

            var gpu = _renderer.CreateSelectionMesh(source, faces, nodes, _originX, _originY, _originZ);
            if (gpu is not null)
            {
                _selectionGpu[source] = gpu;
            }
        }
    }

    /// <summary>Builds the ordered visible render-item and pick-lookup lists.</summary>
    private void BuildRenderLists()
    {
        _renderItems.Clear();
        _visibleGpus.Clear();
        _visibleSources.Clear();

        foreach (var (source, gpu) in _gpuMeshes)
        {
            if (!source.IsVisible)
            {
                continue;
            }

            gpu.Color = ToVector4(source.Color, source.Opacity);
            gpu.ShowEdges = ShowEdges && source.ShowEdges;
            gpu.IsClippable = source.IsClippable;

            _selectionGpu.TryGetValue(source, out var selection);
            var isHovered = ReferenceEquals(source, _hoverMesh);
            _renderItems.Add(new RenderItem(
                gpu, selection, Selection.IsPartSelected(source),
                Hover: isHovered ? _hoverGpu : null,
                IsPartHovered: isHovered && _hoverIsPart));
            _visibleGpus.Add(gpu);
            _visibleSources.Add(source);
        }
    }

    private void EnsureColorMap()
    {
        if (!_colorMapDirty || _renderer is null)
        {
            return;
        }

        _colorMapDirty = false;
        var scale = ColorScale;
        if (scale is null)
        {
            return;
        }

        var rgba = new byte[256 * 4];
        for (var i = 0; i < 256; i++)
        {
            var color = scale.Sample((i + 0.5) / 256.0);
            rgba[i * 4] = color.R;
            rgba[i * 4 + 1] = color.G;
            rgba[i * 4 + 2] = color.B;
            rgba[i * 4 + 3] = color.A;
        }

        _renderer.SetColorMap(rgba);
    }

    private ContourSettings BuildContourSettings()
    {
        var scale = ColorScale;
        if (scale is null)
        {
            return new ContourSettings(0.0f, 1.0f, false, Vector4.Zero, Vector4.Zero, Vector4.One);
        }

        var useLog = scale.IsLogarithmic && scale.Minimum > 0 && scale.Maximum > 0;
        var min = useLog ? Math.Log10(scale.Minimum) : scale.Minimum;
        var max = useLog ? Math.Log10(scale.Maximum) : scale.Maximum;
        var range = max - min;
        var invRange = Math.Abs(range) < 1e-300 ? 0.0 : 1.0 / range;

        var below = scale.BelowRangeColor ?? scale.Sample(0.0);
        var above = scale.AboveRangeColor ?? scale.Sample(1.0);

        return new ContourSettings(
            (float)min, (float)invRange, useLog,
            ToVector4(scale.NaNColor, scale.NaNColor.A / 255.0),
            ToVector4(below, 1.0),
            ToVector4(above, 1.0));
    }

    // ================= MeshSource / ColorScale change tracking =================

    private static void OnMeshSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewport = (WorkbenchViewport)d;

        if (e.OldValue is INotifyCollectionChanged oldIncc)
        {
            oldIncc.CollectionChanged -= viewport.OnMeshCollectionChanged;
        }

        viewport.UnhookAllMeshes(e.OldValue as IEnumerable);

        if (e.NewValue is INotifyCollectionChanged newIncc)
        {
            newIncc.CollectionChanged += viewport.OnMeshCollectionChanged;
        }

        viewport.HookAllMeshes(e.NewValue as IEnumerable);
        viewport._geometryDirty = true;
        viewport._hasAutoFitted = false;
        viewport.InvalidateViewport();
    }

    private static void OnVoxelSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewport = (WorkbenchViewport)d;

        if (e.OldValue is INotifyCollectionChanged oldIncc)
        {
            oldIncc.CollectionChanged -= viewport.OnVoxelCollectionChanged;
        }

        viewport.UnhookAllVoxels(e.OldValue as IEnumerable);

        if (e.NewValue is INotifyCollectionChanged newIncc)
        {
            newIncc.CollectionChanged += viewport.OnVoxelCollectionChanged;
        }

        viewport.HookAllVoxels(e.NewValue as IEnumerable);
        viewport._geometryDirty = true;
        viewport._hasAutoFitted = false;
        viewport.InvalidateViewport();
    }

    private void OnVoxelCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems.OfType<ViewportVoxelGrid>())
            {
                item.PropertyChanged -= OnVoxelPropertyChanged;
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            UnhookAllVoxels(VoxelSource);
            HookAllVoxels(VoxelSource);
        }
        else if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<ViewportVoxelGrid>())
            {
                item.PropertyChanged += OnVoxelPropertyChanged;
            }
        }

        _geometryDirty = true;
        _hasAutoFitted = false;
        InvalidateViewport();
    }

    private void HookAllVoxels(IEnumerable? voxels)
    {
        if (voxels is null)
        {
            return;
        }

        foreach (var voxel in voxels.OfType<ViewportVoxelGrid>())
        {
            voxel.PropertyChanged += OnVoxelPropertyChanged;
        }
    }

    private void UnhookAllVoxels(IEnumerable? voxels)
    {
        if (voxels is null)
        {
            return;
        }

        foreach (var voxel in voxels.OfType<ViewportVoxelGrid>())
        {
            voxel.PropertyChanged -= OnVoxelPropertyChanged;
        }
    }

    private void OnVoxelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _sceneVersion++;

        if (e.PropertyName is nameof(ViewportVoxelGrid.VectorValues))
        {
            // Vector field change only updates glyphs without re-extracting surfaces
            _glyphDirty = true;
        }
        else if (e.PropertyName is nameof(ViewportVoxelGrid.ScalarValues)
                 && sender is ViewportVoxelGrid voxel)
        {
            // Scalar field change uses fast path: brick re-averaging + vertex scalar update only
            var any = false;
            foreach (var (mesh, binding) in _voxelBindings)
            {
                if (!ReferenceEquals(binding.Source, voxel))
                {
                    continue;
                }

                _scalarDirtyMeshes.Add(mesh);
                any = true;
            }

            if (!any)
            {
                // Not yet built or bound; fall back to full rebuild
                _geometryDirty = true;
            }
        }
        else
        {
            _geometryDirty = true;
        }

        InvalidateViewport();
    }

    private void OnMeshCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems.OfType<ViewportMesh>())
            {
                item.PropertyChanged -= OnMeshPropertyChanged;
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            // Reset does not supply OldItems; unhook and re-hook the full collection
            UnhookAllMeshes(MeshSource);
            HookAllMeshes(MeshSource);
        }
        else if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<ViewportMesh>())
            {
                item.PropertyChanged += OnMeshPropertyChanged;
            }
        }

        _geometryDirty = true;
        _hasAutoFitted = false;
        InvalidateViewport();
    }

    private void HookAllMeshes(IEnumerable? meshes)
    {
        if (meshes is null)
        {
            return;
        }

        foreach (var mesh in meshes.OfType<ViewportMesh>())
        {
            mesh.PropertyChanged += OnMeshPropertyChanged;
        }
    }

    private void UnhookAllMeshes(IEnumerable? meshes)
    {
        if (meshes is null)
        {
            return;
        }

        foreach (var mesh in meshes.OfType<ViewportMesh>())
        {
            mesh.PropertyChanged -= OnMeshPropertyChanged;
        }
    }

    private void OnMeshPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Visibility or displacement changes invalidate the hover ID cache
        _sceneVersion++;

        if (e.PropertyName is nameof(ViewportMesh.Positions)
            or nameof(ViewportMesh.TriangleIndices))
        {
            _geometryDirty = true;
        }
        else if (e.PropertyName == nameof(ViewportMesh.ScalarValues) && sender is ViewportMesh scalarMesh)
        {
            // Contour field swap uses the lightweight scalar update path
            _scalarDirtyMeshes.Add(scalarMesh);
        }
        else if (e.PropertyName == nameof(ViewportMesh.Displacements) && sender is ViewportMesh mesh)
        {
            // Lightweight displacement swap: update displacement buffer, selection quads, and glyph instances
            _displacementDirtyMeshes.Add(mesh);
            _selectionDirty = true;
            _glyphDirty = true;
        }
        else if (e.PropertyName == nameof(ViewportMesh.VectorValues))
        {
            // Field switch (e.g., displacement vs. reaction force) only rebuilds glyph instance buffers
            _glyphDirty = true;
        }

        InvalidateViewport();
    }

    private static void OnColorScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewport = (WorkbenchViewport)d;

        if (e.OldValue is ColorScale oldScale)
        {
            oldScale.PropertyChanged -= viewport.OnColorScalePropertyChanged;
        }

        if (e.NewValue is ColorScale newScale)
        {
            newScale.PropertyChanged += viewport.OnColorScalePropertyChanged;
        }

        viewport._colorMapDirty = true;
        viewport.InvalidateViewport();
    }

    private void OnColorScalePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _colorMapDirty = true;
        InvalidateViewport();
    }

    private static void OnVisualOptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((WorkbenchViewport)d).InvalidateViewport();

    /// <summary>Handles changes to options requiring a full geometry rebuild (e.g., EdgeExtractionLimit).</summary>
    private static void OnGeometryOptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewport = (WorkbenchViewport)d;
        viewport._geometryDirty = true;
        viewport.InvalidateViewport();
    }

    /// <summary>Handles changes to glyph options requiring instance buffer rebuilds (e.g., GlyphStride).</summary>
    private static void OnGlyphDataOptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewport = (WorkbenchViewport)d;
        viewport._glyphDirty = true;
        viewport.InvalidateViewport();
    }

    private static void OnGlyphColorScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewport = (WorkbenchViewport)d;

        if (e.OldValue is ColorScale oldScale)
        {
            oldScale.PropertyChanged -= viewport.OnGlyphColorScalePropertyChanged;
        }

        if (e.NewValue is ColorScale newScale)
        {
            newScale.PropertyChanged += viewport.OnGlyphColorScalePropertyChanged;
        }

        viewport._glyphDirty = true;
        viewport.InvalidateViewport();
    }

    private void OnGlyphColorScalePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _glyphDirty = true;
        InvalidateViewport();
    }

    private static void OnSectionPlaneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewport = (WorkbenchViewport)d;

        if (e.OldValue is SectionPlane oldPlane)
        {
            oldPlane.PropertyChanged -= viewport.OnSectionPlanePropertyChanged;
        }

        if (e.NewValue is SectionPlane newPlane)
        {
            newPlane.PropertyChanged += viewport.OnSectionPlanePropertyChanged;
        }

        viewport.InvalidateViewport();
    }

    private void OnSectionPlanePropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        InvalidateViewport();

    /// <summary>
    /// Computes shader section-clip coefficients; returns <see cref="ViewportSection.DisabledClip"/> when disabled.
    /// </summary>
    private Vector4 GetCurrentClipPlane() =>
        ViewportSection.ComputeClipCoefficients(SectionPlane, _originX, _originY, _originZ)
        ?? ViewportSection.DisabledClip;

    private static void OnPickModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewport = (WorkbenchViewport)d;
        // Show crosshair cursor while a pick mode is active
        viewport.Cursor = (ViewportPickMode)e.NewValue != ViewportPickMode.None ? Cursors.Cross : null;
        viewport.CancelPicking();
        viewport.ClearHover(); // Granularity changed; resolve again on next pointer move
    }

    private void OnSelectionModelChanged(object? sender, EventArgs e)
    {
        _selectionDirty = true;
        InvalidateViewport();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void OnProjectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((WorkbenchViewport)d).Camera.Projection = (ViewportProjection)e.NewValue;

    private static void OnUpAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((WorkbenchViewport)d).Camera.UpAxis = (ViewportUpAxis)e.NewValue;

    // ================= Mouse interaction =================

    /// <inheritdoc />
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.ChangedButton == MouseButton.Left)
        {
            if (PickMode == ViewportPickMode.None || _isOrbiting || _isPanning)
            {
                return;
            }

            _isPicking = true;
            _isRubberBanding = false;
            _pickStart = e.GetPosition(this);
            Focus();
            CaptureMouse();
            e.Handled = true;
            return;
        }

        if (e.ChangedButton != MouseButton.Middle)
        {
            return;
        }

        CancelPicking();

        if (e.ClickCount == 2)
        {
            FitToView();
            e.Handled = true;
            return;
        }

        _lastMousePosition = e.GetPosition(this);
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            _isPanning = true;
        }
        else
        {
            _isOrbiting = true;
        }

        Focus();
        CaptureMouse();
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_isPicking)
        {
            var position = e.GetPosition(this);
            // Probe and Cell modes are click-only (no rubber-band selection)
            if (!_isRubberBanding
                && PickMode != ViewportPickMode.Probe
                && PickMode != ViewportPickMode.Cell
                && (Math.Abs(position.X - _pickStart.X) > RubberBandThresholdDip
                    || Math.Abs(position.Y - _pickStart.Y) > RubberBandThresholdDip))
            {
                _isRubberBanding = true;
            }

            if (_isRubberBanding)
            {
                UpdateRubberBand(_pickStart, position);
            }

            e.Handled = true;
            return;
        }

        if (!_isOrbiting && !_isPanning)
        {
            // Update hover pre-highlight when not dragging
            UpdateHover(e.GetPosition(this));
            return;
        }

        var current = e.GetPosition(this);
        var dx = current.X - _lastMousePosition.X;
        var dy = current.Y - _lastMousePosition.Y;
        _lastMousePosition = current;

        var dpi = VisualTreeHelper.GetDpi(this);

        if (_isPanning)
        {
            Camera.Pan(dx * dpi.DpiScaleX, dy * dpi.DpiScaleY, ActualHeight * dpi.DpiScaleY);
        }
        else
        {
            // ~0.5 degrees per pixel (standard CAD sensitivity)
            const double sensitivity = 0.5 * Math.PI / 180.0;
            // Make the model follow cursor motion: dragging right decreases yaw
            Camera.Orbit(-dx * sensitivity, -dy * sensitivity);
        }

        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.ChangedButton == MouseButton.Left && _isPicking)
        {
            var position = e.GetPosition(this);
            var wasRubberBanding = _isRubberBanding;
            var rubberBandStart = _pickStart;
            CancelPicking();

            var additive = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            if (PickMode == ViewportPickMode.Probe)
            {
                ProbeAtPoint(position);
            }
            else if (PickMode == ViewportPickMode.Cell)
            {
                VoxelProbeAtPoint(position);
            }
            else if (wasRubberBanding)
            {
                SelectInRectangle(rubberBandStart, position, additive);
            }
            else
            {
                SelectAtPoint(position, additive);
            }

            e.Handled = true;
            return;
        }

        if (e.ChangedButton == MouseButton.Middle && (_isOrbiting || _isPanning))
        {
            _isOrbiting = false;
            _isPanning = false;
            ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        ClearHover();
    }

    /// <summary>Cancels any active picking or rubber-band operation and restores state.</summary>
    private void CancelPicking()
    {
        if (_isPicking)
        {
            _isPicking = false;
            _isRubberBanding = false;
            ReleaseMouseCapture();
        }

        if (_rubberBand is not null)
        {
            _rubberBand.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateRubberBand(Point start, Point end)
    {
        if (_rubberBand is null)
        {
            return;
        }

        var x = Math.Min(start.X, end.X);
        var y = Math.Min(start.Y, end.Y);
        Canvas.SetLeft(_rubberBand, x);
        Canvas.SetTop(_rubberBand, y);
        _rubberBand.Width = Math.Abs(end.X - start.X);
        _rubberBand.Height = Math.Abs(end.Y - start.Y);
        _rubberBand.Visibility = Visibility.Visible;
    }

    // ================= Hover pre-highlight =================

    private static void OnIsHoverHighlightEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is false)
        {
            ((WorkbenchViewport)d).ClearHover();
        }
    }

    /// <summary>
    /// Updates the hover target on pointer move using the cached idle ID buffer (near-zero cost per move).
    /// Re-captures the ID pass only when camera, deformation, clip plane, or scene version changes.
    /// Disabled during camera interaction (LOD) and view animations.
    /// </summary>
    private void UpdateHover(Point positionDip)
    {
        if (!IsHoverHighlightEnabled || PickMode == ViewportPickMode.None
            || _lodRequested || _isAnimatingView)
        {
            ClearHover();
            return;
        }

        if (!TryGetPickContext(out var viewProj, out var dpiScale) || !EnsureHoverIdCache(in viewProj))
        {
            ClearHover();
            return;
        }

        var px = (int)(positionDip.X * dpiScale);
        var py = (int)(positionDip.Y * dpiScale);
        var hit = ViewportHover.ReadId(_hoverIdBuffer!, _hoverIdWidth, _hoverIdHeight, px, py);
        if (hit is not { } h || h.MeshIndex >= _visibleSources.Count)
        {
            ClearHover();
            return;
        }

        var mesh = _visibleSources[h.MeshIndex];
        switch (PickMode)
        {
            case ViewportPickMode.Part:
                SetHover(mesh, -1, -1, isPart: true);
                break;

            case ViewportPickMode.Face:
            case ViewportPickMode.Cell:
                SetHover(mesh, h.TriangleIndex, -1, isPart: false);
                break;

            case ViewportPickMode.Node:
            case ViewportPickMode.Probe:
                // Probe mode previews the nearest node where an annotation will anchor
                var node = ViewportPicking.FindNearestNodeOnTriangle(
                    mesh, h.TriangleIndex, _originX, _originY, _originZ,
                    in viewProj, _renderer!.Width, _renderer.Height, new Vector2(px, py),
                    _lastEffectiveDeformationScale);
                if (node is { } n)
                {
                    SetHover(mesh, -1, n, isPart: false);
                }
                else
                {
                    ClearHover();
                }

                break;
        }
    }

    /// <summary>
    /// Refreshes the hover ID buffer if the render-state stamp (viewProj, deformation, clip, size, scene version) differs.
    /// </summary>
    private bool EnsureHoverIdCache(in Matrix4x4 viewProj)
    {
        var deformScale = _lastEffectiveDeformationScale;
        var clip = GetCurrentClipPlane();
        if (_hoverIdValid
            && _hoverIdViewProj == viewProj
            && _hoverIdDeformScale == deformScale
            && _hoverIdClip == clip
            && _hoverIdWidth == _renderer!.Width
            && _hoverIdHeight == _renderer.Height
            && _hoverIdSceneVersion == _sceneVersion)
        {
            return true;
        }

        if (!_renderer!.CaptureIdBuffer(_visibleGpus, in viewProj, deformScale, clip, ref _hoverIdBuffer))
        {
            _hoverIdValid = false;
            return false;
        }

        _hoverIdValid = true;
        _hoverIdViewProj = viewProj;
        _hoverIdDeformScale = deformScale;
        _hoverIdClip = clip;
        _hoverIdWidth = _renderer.Width;
        _hoverIdHeight = _renderer.Height;
        _hoverIdSceneVersion = _sceneVersion;
        return true;
    }

    /// <summary>
    /// Reads a pixel from the hover ID cache when its state stamp matches the current view;
    /// returns false if stale so click picking falls back to GPU PickPixel.
    /// </summary>
    private bool TryReadCachedId(int px, int py, in Matrix4x4 viewProj, out (int MeshIndex, int TriangleIndex)? hit)
    {
        hit = null;
        if (!_hoverIdValid || _hoverIdBuffer is null
            || _hoverIdViewProj != viewProj
            || _hoverIdDeformScale != _lastEffectiveDeformationScale
            || _hoverIdClip != GetCurrentClipPlane()
            || _hoverIdWidth != _renderer!.Width
            || _hoverIdHeight != _renderer.Height
            || _hoverIdSceneVersion != _sceneVersion)
        {
            return false;
        }

        hit = ViewportHover.ReadId(_hoverIdBuffer, _hoverIdWidth, _hoverIdHeight, px, py);
        return true;
    }

    private void SetHover(ViewportMesh mesh, int triangle, int node, bool isPart)
    {
        if (ReferenceEquals(mesh, _hoverMesh)
            && triangle == _hoverTriangle && node == _hoverNode && isPart == _hoverIsPart)
        {
            return;
        }

        _hoverGpu?.Dispose();
        _hoverGpu = null;
        _hoverMesh = mesh;
        _hoverTriangle = triangle;
        _hoverNode = node;
        _hoverIsPart = isPart;

        if (!isPart && _renderer is not null)
        {
            _hoverGpu = _renderer.CreateSelectionMesh(
                mesh,
                triangle >= 0 ? [triangle] : [],
                node >= 0 ? [node] : [],
                _originX, _originY, _originZ);
        }

        HoverInfo = new ViewportHoverInfo(mesh, triangle, node, isPart);
        HoverChanged?.Invoke(this, EventArgs.Empty);
        InvalidateViewport();
    }

    private void ClearHover()
    {
        if (_hoverMesh is null && HoverInfo is null)
        {
            return;
        }

        _hoverGpu?.Dispose();
        _hoverGpu = null;
        _hoverMesh = null;
        _hoverTriangle = -1;
        _hoverNode = -1;
        _hoverIsPart = false;
        HoverInfo = null;
        HoverChanged?.Invoke(this, EventArgs.Empty);
        InvalidateViewport();
    }

    // ================= Picking execution =================

    /// <summary>
    /// Performs click selection. When <paramref name="additive"/> is false, replaces the selection
    /// (clearing on background click); when true (Ctrl), toggles the hit element.
    /// </summary>
    private void SelectAtPoint(Point positionDip, bool additive)
    {
        if (!TryGetPickContext(out var viewProj, out var dpiScale))
        {
            return;
        }

        var px = (int)(positionDip.X * dpiScale);
        var py = (int)(positionDip.Y * dpiScale);

        // Reuse valid hover ID cache to skip a redundant GPU ID pass
        if (!TryReadCachedId(px, py, in viewProj, out var hit))
        {
            hit = _renderer!.PickPixel(
                _visibleGpus, in viewProj, px, py, _lastEffectiveDeformationScale, GetCurrentClipPlane());
        }

        Selection.BeginUpdate();
        try
        {
            if (!additive)
            {
                Selection.Clear();
            }

            if (hit is not { } h || h.MeshIndex >= _visibleSources.Count)
            {
                return;
            }

            var mesh = _visibleSources[h.MeshIndex];
            switch (PickMode)
            {
                case ViewportPickMode.Part:
                    if (additive)
                    {
                        Selection.TogglePart(mesh);
                    }
                    else
                    {
                        Selection.AddPart(mesh);
                    }

                    break;

                case ViewportPickMode.Face:
                    if (additive)
                    {
                        Selection.ToggleFace(mesh, h.TriangleIndex);
                    }
                    else
                    {
                        Selection.AddFace(mesh, h.TriangleIndex);
                    }

                    break;

                case ViewportPickMode.Node:
                    var cursor = new Vector2(px, py);
                    var node = ViewportPicking.FindNearestNodeOnTriangle(
                        mesh, h.TriangleIndex, _originX, _originY, _originZ,
                        in viewProj, _renderer!.Width, _renderer.Height, cursor,
                        _lastEffectiveDeformationScale);
                    if (node is { } n)
                    {
                        if (additive)
                        {
                            Selection.ToggleNode(mesh, n);
                        }
                        else
                        {
                            Selection.AddNode(mesh, n);
                        }
                    }

                    break;
            }
        }
        finally
        {
            Selection.EndUpdate();
        }
    }

    /// <summary>
    /// Performs rubber-band rectangle selection. Targets visible elements by default, or includes occluded
    /// elements when <see cref="RubberBandSelectionMode"/> is <see cref="ViewportRubberBandSelectionMode.Through"/>.
    /// </summary>
    private void SelectInRectangle(Point startDip, Point endDip, bool additive)
    {
        if (!TryGetPickContext(out var viewProj, out var dpiScale))
        {
            return;
        }

        var x0 = (int)(Math.Min(startDip.X, endDip.X) * dpiScale);
        var y0 = (int)(Math.Min(startDip.Y, endDip.Y) * dpiScale);
        var x1 = (int)Math.Ceiling(Math.Max(startDip.X, endDip.X) * dpiScale);
        var y1 = (int)Math.Ceiling(Math.Max(startDip.Y, endDip.Y) * dpiScale);

        if (RubberBandSelectionMode == ViewportRubberBandSelectionMode.Through)
        {
            SelectInRectangleThrough(in viewProj, new Vector2(x0, y0), new Vector2(x1, y1), additive);
            return;
        }

        var region = _renderer!.PickRegion(
            _visibleGpus, in viewProj, x0, y0, x1 - x0, y1 - y0, _lastEffectiveDeformationScale,
            GetCurrentClipPlane());

        Selection.BeginUpdate();
        try
        {
            if (!additive)
            {
                Selection.Clear();
            }

            foreach (var (meshIndex, triangles) in region)
            {
                if (meshIndex >= _visibleSources.Count)
                {
                    continue;
                }

                var mesh = _visibleSources[meshIndex];
                switch (PickMode)
                {
                    case ViewportPickMode.Part:
                        Selection.AddPart(mesh);
                        break;

                    case ViewportPickMode.Face:
                        Selection.AddFaces(mesh, triangles);
                        break;

                    case ViewportPickMode.Node:
                        var nodes = ViewportPicking.FindNodesInRectangle(
                            mesh, triangles, _originX, _originY, _originZ,
                            in viewProj, _renderer.Width, _renderer.Height,
                            new Vector2(x0, y0), new Vector2(x1, y1),
                            _lastEffectiveDeformationScale);
                        Selection.AddNodes(mesh, nodes);
                        break;
                }
            }
        }
        finally
        {
            Selection.EndUpdate();
        }
    }

    // ================= Through rectangle selection =================

    /// <summary>Triangles per task in parallel CPU through-selection.</summary>
    private const int ThroughSelectBlockSize = 65_536;

    /// <summary>
    /// Performs through-selection without GPU ID readback: coarsely culls chunks by projected AABB,
    /// then tests deformed coordinates in screen space in parallel on the CPU (excluding section-clipped elements).
    /// </summary>
    private void SelectInRectangleThrough(
        in Matrix4x4 viewProj, Vector2 rectMin, Vector2 rectMax, bool additive)
    {
        var width = (double)_renderer!.Width;
        var height = (double)_renderer.Height;
        var deformScale = (double)_lastEffectiveDeformationScale;
        var clipCoefficients = GetCurrentClipPlane();
        var viewProjLocal = viewProj; // Copy for lambda capture

        Selection.BeginUpdate();
        try
        {
            if (!additive)
            {
                Selection.Clear();
            }

            for (var i = 0; i < _visibleSources.Count; i++)
            {
                var mesh = _visibleSources[i];
                var gpu = _visibleGpus[i];
                var clip = mesh.IsClippable ? clipCoefficients : ViewportSection.DisabledClip;
                var expand = gpu.MaxDisplacementMagnitude * MathF.Abs((float)deformScale);

                // Coarse chunk AABB screen-bounds filter
                var candidates = new List<GpuMeshChunk>();
                foreach (var chunk in gpu.Chunks)
                {
                    if (ViewportRectSelect.ScreenBoundsMayOverlapRect(
                        chunk.BoundsMin, chunk.BoundsMax, expand,
                        in viewProjLocal, width, height, rectMin, rectMax))
                    {
                        candidates.Add(chunk);
                    }
                }

                if (candidates.Count == 0)
                {
                    continue;
                }

                switch (PickMode)
                {
                    case ViewportPickMode.Part:
                        if (AnyTriangleInRectThrough(
                            mesh, candidates, deformScale, in viewProjLocal, width, height,
                            rectMin, rectMax, clip))
                        {
                            Selection.AddPart(mesh);
                        }

                        break;

                    case ViewportPickMode.Face:
                        var faces = CollectTrianglesThrough(
                            mesh, candidates, deformScale, in viewProjLocal, width, height,
                            rectMin, rectMax, clip);
                        if (faces.Count > 0)
                        {
                            Selection.AddFaces(mesh, faces);
                        }

                        break;

                    case ViewportPickMode.Node:
                        var nodes = CollectNodesThrough(
                            mesh, candidates, deformScale, in viewProjLocal, width, height,
                            rectMin, rectMax, clip);
                        if (nodes.Count > 0)
                        {
                            Selection.AddNodes(mesh, nodes);
                        }

                        break;
                }
            }
        }
        finally
        {
            Selection.EndUpdate();
        }
    }

    /// <summary>Splits chunk global triangle ranges into blocks for parallel processing.</summary>
    private static List<(int Start, int Count)> BuildTriangleBlocks(IReadOnlyList<GpuMeshChunk> chunks)
    {
        var blocks = new List<(int Start, int Count)>();
        foreach (var chunk in chunks)
        {
            var start = (int)chunk.TriangleBase;
            var count = (int)chunk.TriangleIndexCount / 3;
            for (var offset = 0; offset < count; offset += ThroughSelectBlockSize)
            {
                blocks.Add((start + offset, Math.Min(ThroughSelectBlockSize, count - offset)));
            }
        }

        return blocks;
    }

    private bool AnyTriangleInRectThrough(
        ViewportMesh mesh, IReadOnlyList<GpuMeshChunk> chunks, double deformScale,
        in Matrix4x4 viewProj, double width, double height,
        Vector2 rectMin, Vector2 rectMax, Vector4 clip)
    {
        var viewProjLocal = viewProj;
        var found = 0;
        Parallel.ForEach(BuildTriangleBlocks(chunks), (block, state) =>
        {
            for (var t = block.Start; t < block.Start + block.Count; t++)
            {
                if (state.IsStopped)
                {
                    return;
                }

                if (ViewportRectSelect.IsTriangleInRect(
                    mesh.Positions, mesh.TriangleIndices, mesh.Displacements, deformScale, t,
                    _originX, _originY, _originZ, in viewProjLocal, width, height,
                    rectMin, rectMax, clip))
                {
                    Interlocked.Exchange(ref found, 1);
                    state.Stop();
                    return;
                }
            }
        });

        return found == 1;
    }

    private List<int> CollectTrianglesThrough(
        ViewportMesh mesh, IReadOnlyList<GpuMeshChunk> chunks, double deformScale,
        in Matrix4x4 viewProj, double width, double height,
        Vector2 rectMin, Vector2 rectMax, Vector4 clip)
    {
        var viewProjLocal = viewProj;
        var results = new List<int>();
        var gate = new object();
        Parallel.ForEach(
            BuildTriangleBlocks(chunks),
            () => new List<int>(),
            (block, _, local) =>
            {
                ViewportRectSelect.CollectTrianglesInRect(
                    mesh.Positions, mesh.TriangleIndices, mesh.Displacements, deformScale,
                    _originX, _originY, _originZ, in viewProjLocal, width, height,
                    rectMin, rectMax, clip, block.Start, block.Count, local);
                return local;
            },
            local =>
            {
                lock (gate)
                {
                    results.AddRange(local);
                }
            });

        return results;
    }

    private HashSet<int> CollectNodesThrough(
        ViewportMesh mesh, IReadOnlyList<GpuMeshChunk> chunks, double deformScale,
        in Matrix4x4 viewProj, double width, double height,
        Vector2 rectMin, Vector2 rectMax, Vector4 clip)
    {
        var viewProjLocal = viewProj;
        var results = new HashSet<int>();
        var gate = new object();

        // Parallelize per chunk (HashSet deduplicates shared boundary vertices)
        Parallel.ForEach(
            chunks,
            () => new List<int>(),
            (chunk, _, local) =>
            {
                foreach (var node in chunk.GlobalVertices)
                {
                    if (ViewportRectSelect.IsNodeInRect(
                        mesh.Positions, mesh.Displacements, deformScale, node,
                        _originX, _originY, _originZ, in viewProjLocal, width, height,
                        rectMin, rectMax, clip))
                    {
                        local.Add(node);
                    }
                }

                return local;
            },
            local =>
            {
                lock (gate)
                {
                    foreach (var node in local)
                    {
                        results.Add(node);
                    }
                }
            });

        return results;
    }

    // ================= Probing and annotations =================

    /// <summary>
    /// Executes a surface probe: identifies the hit triangle via GPU ID picking, computes the hit point
    /// and interpolated scalar via CPU ray intersection, raises <see cref="ProbePicked"/>, and adds an
    /// annotation unless handled. Background clicks do nothing.
    /// </summary>
    private void ProbeAtPoint(Point positionDip)
    {
        if (!TryGetPickContext(out var viewProj, out var dpiScale))
        {
            return;
        }

        var px = (int)(positionDip.X * dpiScale);
        var py = (int)(positionDip.Y * dpiScale);

        // Reuse valid hover ID cache to avoid a redundant GPU ID pass
        if (!TryReadCachedId(px, py, in viewProj, out var hit))
        {
            hit = _renderer!.PickPixel(
                _visibleGpus, in viewProj, px, py, _lastEffectiveDeformationScale, GetCurrentClipPlane());
        }

        if (hit is not { } h || h.MeshIndex >= _visibleSources.Count)
        {
            return;
        }

        var result = ViewportProbing.Probe(
            _visibleSources[h.MeshIndex], h.TriangleIndex, new Vector2(px, py), in viewProj,
            _renderer!.Width, _renderer.Height, _originX, _originY, _originZ,
            _lastEffectiveDeformationScale);
        if (result is null)
        {
            return;
        }

        var args = new ProbePickedEventArgs(result);
        ProbePicked?.Invoke(this, args);
        if (args.Handled)
        {
            return;
        }

        Annotations.Add(new ViewportAnnotation
        {
            Mesh = result.Mesh,
            NodeIndex = result.NodeIndex,
            X = result.X,
            Y = result.Y,
            Z = result.Z,
            Text = ProbeLabelFormatter?.Invoke(result) ?? FormatDefaultProbeLabel(result),
            Tag = result,
        });
    }

    /// <summary>
    /// Executes a voxel cell probe: picks the outer surface via GPU and maps back to the cell,
    /// falling back to CPU ray-vs-AABB intersection when needed.
    /// </summary>
    private void VoxelProbeAtPoint(Point positionDip)
    {
        if (!TryGetPickContext(out var viewProj, out var dpiScale))
        {
            return;
        }

        var px = (int)(positionDip.X * dpiScale);
        var py = (int)(positionDip.Y * dpiScale);

        VoxelProbeResult? probe = null;
        ViewportMesh? hitMesh = null;
        var hitTriangle = -1;

        if (!TryReadCachedId(px, py, in viewProj, out var hit))
        {
            hit = _renderer!.PickPixel(
                _visibleGpus, in viewProj, px, py, _lastEffectiveDeformationScale, GetCurrentClipPlane());
        }

        if (hit is { } h && h.MeshIndex < _visibleSources.Count)
        {
            hitMesh = _visibleSources[h.MeshIndex];
            hitTriangle = h.TriangleIndex;
            if (_voxelBindings.TryGetValue(hitMesh, out var binding)
                && ViewportVoxelPicking.TryMapTriangle(
                    binding.Display, binding.Surface, h.TriangleIndex,
                    out var slot, out var linear, out var i, out var j, out var k))
            {
                // Obtain hit coordinates from triangle probe
                var meshProbe = ViewportProbing.Probe(
                    hitMesh, h.TriangleIndex, new Vector2(px, py), in viewProj,
                    _renderer!.Width, _renderer.Height, _originX, _originY, _originZ,
                    _lastEffectiveDeformationScale);
                var hx = meshProbe?.X ?? 0;
                var hy = meshProbe?.Y ?? 0;
                var hz = meshProbe?.Z ?? 0;
                if (meshProbe is null)
                {
                    binding.Display.GetCellCenter(i, j, k, out hx, out hy, out hz);
                }

                probe = ViewportVoxelPicking.CreateProbeResult(
                    binding.Display, slot, linear, i, j, k, hx, hy, hz);
            }
        }

        if (probe is null)
        {
            // Fallback: raycast against each visible voxel grid (local -> model coordinates)
            var ray = ViewportProbing.ComputePickRay(
                new Vector2(px, py), in viewProj, _renderer!.Width, _renderer.Height);
            if (ray is { } r)
            {
                var rox = r.Origin.X + _originX;
                var roy = r.Origin.Y + _originY;
                var roz = r.Origin.Z + _originZ;
                var rdx = r.Direction.X;
                var rdy = r.Direction.Y;
                var rdz = r.Direction.Z;
                var bestDist = double.PositiveInfinity;
                foreach (var voxel in VoxelSource?.OfType<ViewportVoxelGrid>() ?? [])
                {
                    if (!voxel.IsVisible)
                    {
                        continue;
                    }

                    if (ViewportVoxelPicking.TryPickCell(
                            voxel, rox, roy, roz, rdx, rdy, rdz,
                            out var slot, out var linear, out var i, out var j, out var k,
                            out var hx, out var hy, out var hz, out var dist)
                        && dist < bestDist)
                    {
                        bestDist = dist;
                        probe = ViewportVoxelPicking.CreateProbeResult(
                            voxel, slot, linear, i, j, k, hx, hy, hz);
                    }
                }
            }
        }

        if (probe is null)
        {
            return;
        }

        var args = new VoxelProbePickedEventArgs(probe);
        VoxelProbePicked?.Invoke(this, args);
        if (args.Handled)
        {
            return;
        }

        // Anchor the annotation to a node on the outer mesh when available, otherwise use a fixed world point.
        if (hitMesh is not null && hitTriangle >= 0)
        {
            var node = hitTriangle * 3; // Approximate anchor with first vertex of the hit triangle
            if (hitMesh.TriangleIndices.Length > node)
            {
                node = hitMesh.TriangleIndices[node];
            }

            Annotations.Add(new ViewportAnnotation
            {
                Mesh = hitMesh,
                NodeIndex = node,
                X = probe.X,
                Y = probe.Y,
                Z = probe.Z,
                Text = VoxelProbeLabelFormatter?.Invoke(probe) ?? FormatDefaultVoxelProbeLabel(probe),
                Tag = probe,
            });
        }
        else
        {
            Annotations.Add(new ViewportAnnotation
            {
                X = probe.X,
                Y = probe.Y,
                Z = probe.Z,
                Text = VoxelProbeLabelFormatter?.Invoke(probe) ?? FormatDefaultVoxelProbeLabel(probe),
                Tag = probe,
            });
        }
    }

    /// <summary>Built-in culture-invariant voxel probe label format.</summary>
    internal static string FormatDefaultVoxelProbeLabel(VoxelProbeResult result) => result.ScalarValue is { } value
        ? FormattableString.Invariant($"C[{result.I},{result.J},{result.K}] R{result.RegionId}: {value:G4}")
        : FormattableString.Invariant($"C[{result.I},{result.J},{result.K}] R{result.RegionId}");

    /// <summary>Built-in culture-invariant probe label format.</summary>
    internal static string FormatDefaultProbeLabel(ProbeResult result) => result.ScalarValue is { } value
        ? FormattableString.Invariant($"N{result.NodeIndex}: {value:G4}")
        : FormattableString.Invariant(
            $"N{result.NodeIndex} ({result.X:G4}, {result.Y:G4}, {result.Z:G4})");

    private void OnAnnotationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Track hooked items in a separate list because Reset does not expose removed items
        foreach (var annotation in _hookedAnnotations)
        {
            annotation.PropertyChanged -= OnAnnotationItemChanged;
        }

        _hookedAnnotations.Clear();
        foreach (var annotation in Annotations)
        {
            annotation.PropertyChanged += OnAnnotationItemChanged;
            _hookedAnnotations.Add(annotation);
        }

        RebuildAnnotationVisuals();
        InvalidateViewport();
    }

    private void OnAnnotationItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewportAnnotation.Text) && sender is ViewportAnnotation annotation)
        {
            _annotationVisuals.FirstOrDefault(v => v.Annotation == annotation)?.Label
                .SetCurrentValue(TextBlock.TextProperty, annotation.Text);
        }

        InvalidateViewport();
    }

    /// <summary>Visual elements for a single annotation (chip + leader line + anchor dot).</summary>
    private sealed record AnnotationVisual(
        ViewportAnnotation Annotation, Border Chip, TextBlock Label, Line Leader, Ellipse Dot);

    private void RebuildAnnotationVisuals()
    {
        if (_annotationCanvas is null)
        {
            return;
        }

        _annotationCanvas.Children.Clear();
        _annotationVisuals.Clear();

        foreach (var annotation in Annotations)
        {
            var label = new TextBlock
            {
                Text = annotation.Text,
                FontSize = 11,
                TextAlignment = TextAlignment.Left,
            };
            label.SetResourceReference(TextBlock.ForegroundProperty, "Wb.Brush.Text.Primary");

            var chip = new Border
            {
                Child = label,
                CornerRadius = new CornerRadius(3),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 2, 6, 2),
                Visibility = Visibility.Collapsed,
            };
            chip.SetResourceReference(Border.BackgroundProperty, "Wb.Brush.Surface.Elevated");
            chip.SetResourceReference(Border.BorderBrushProperty, "Wb.Brush.Accent.Default");

            var leader = new Line { StrokeThickness = 1.0, Visibility = Visibility.Collapsed };
            leader.SetResourceReference(Shape.StrokeProperty, "Wb.Brush.Accent.Default");

            var dot = new Ellipse { Width = 5.0, Height = 5.0, Visibility = Visibility.Collapsed };
            dot.SetResourceReference(Shape.FillProperty, "Wb.Brush.Accent.Default");

            _annotationCanvas.Children.Add(leader);
            _annotationCanvas.Children.Add(dot);
            _annotationCanvas.Children.Add(chip);
            _annotationVisuals.Add(new AnnotationVisual(annotation, chip, label, leader, dot));
        }
    }

    /// <summary>
    /// Updates annotation overlays each frame. Node-bound annotations follow deformed node positions
    /// and automatically hide when the mesh is hidden, clipped by a section plane, or off-screen.
    /// </summary>
    private void UpdateAnnotationOverlay(
        in Matrix4x4 viewProj, double pixelWidth, double pixelHeight, double dpiScale,
        Vector4 clipPlane)
    {
        if (_annotationCanvas is null || _annotationVisuals.Count == 0)
        {
            return;
        }

        foreach (var visual in _annotationVisuals)
        {
            var anchor = GetAnnotationAnchor(visual.Annotation, clipPlane);
            var pixel = anchor is { } a
                ? ViewportPicking.ProjectToPixel(a, in viewProj, pixelWidth, pixelHeight)
                : null;
            var visible = pixel is { } p
                && p.X >= 0.0f && p.X <= pixelWidth && p.Y >= 0.0f && p.Y <= pixelHeight;

            var visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            visual.Chip.Visibility = visibility;
            visual.Leader.Visibility = visibility;
            visual.Dot.Visibility = visibility;
            if (!visible)
            {
                continue;
            }

            var ax = pixel!.Value.X / dpiScale;
            var ay = pixel.Value.Y / dpiScale;

            visual.Dot.SetCurrentValue(Canvas.LeftProperty, ax - visual.Dot.Width / 2.0);
            visual.Dot.SetCurrentValue(Canvas.TopProperty, ay - visual.Dot.Height / 2.0);

            visual.Leader.X1 = ax;
            visual.Leader.Y1 = ay;
            visual.Leader.X2 = ax + AnnotationLeaderLengthDip;
            visual.Leader.Y2 = ay - AnnotationLeaderLengthDip;

            // Position the bottom-left corner of the chip at the end of the leader line
            visual.Chip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            visual.Chip.SetCurrentValue(Canvas.LeftProperty, ax + AnnotationLeaderLengthDip);
            visual.Chip.SetCurrentValue(
                Canvas.TopProperty,
                ay - AnnotationLeaderLengthDip - visual.Chip.DesiredSize.Height);
        }
    }

    /// <summary>
    /// Computes the recentered and deformed local-space anchor position for an annotation.
    /// Returns null when a node-bound annotation should auto-hide (mesh not visible, node out of range,
    /// or clipped by the section plane). Free 3D point anchors always return their position.
    /// </summary>
    private Vector3? GetAnnotationAnchor(ViewportAnnotation annotation, Vector4 clipPlane)
    {
        if (annotation.Mesh is not { } mesh)
        {
            return new Vector3(
                (float)(annotation.X - _originX),
                (float)(annotation.Y - _originY),
                (float)(annotation.Z - _originZ));
        }

        if (!_visibleSources.Contains(mesh))
        {
            return null;
        }

        var node = annotation.NodeIndex;
        var positions = mesh.Positions;
        if (node < 0 || node * 3 + 2 >= positions.Length)
        {
            return null;
        }

        var local = ViewportPicking.GetLocalPosition(
            positions, mesh.Displacements, _lastEffectiveDeformationScale, node,
            _originX, _originY, _originZ);
        if (mesh.IsClippable && ViewportSection.IsClipped(local, clipPlane))
        {
            return null;
        }

        return local;
    }

    /// <summary>
    /// Prepares renderer, visible lists, and view-projection matrix for picking using the exact matrices
    /// used during rendering so hit results align with the displayed frame.
    /// </summary>
    private bool TryGetPickContext(out Matrix4x4 viewProj, out double dpiScale)
    {
        viewProj = default;
        dpiScale = 1.0;

        if (_renderer is null || _renderBroken || _renderer.Width <= 0 || _renderer.Height <= 0)
        {
            return false;
        }

        // Pick against the previous scene while an async geometry build is in progress
        BuildRenderLists();
        if (_visibleGpus.Count == 0)
        {
            return false;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        dpiScale = dpi.DpiScaleX;
        var aspect = (double)_renderer.Width / _renderer.Height;
        viewProj = Camera.GetViewMatrix() * Camera.GetProjectionMatrix(aspect);
        return true;
    }

    /// <inheritdoc />
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        // Zoom directly with the mouse wheel in the 3D viewport
        var factor = Math.Pow(1.0 / 1.2, e.Delta / 120.0);
        var position = e.GetPosition(this);
        var dpi = VisualTreeHelper.GetDpi(this);
        Camera.ZoomAt(
            factor,
            position.X * dpi.DpiScaleX, position.Y * dpi.DpiScaleY,
            ActualWidth * dpi.DpiScaleX, ActualHeight * dpi.DpiScaleY);
        e.Handled = true;
    }

    // ================= Axis triad (WPF overlay) =================

    private static readonly Color[] TriadColors =
    [
        Color.FromRgb(0xE5, 0x51, 0x51), // X
        Color.FromRgb(0x51, 0xC0, 0x51), // Y
        Color.FromRgb(0x42, 0x8B, 0xF0), // Z
    ];

    private static readonly string[] TriadLabelTexts = ["X", "Y", "Z"];

    private void SetupTriadOverlay()
    {
        if (_triadCanvas is null)
        {
            return;
        }

        _triadCanvas.Children.Clear();
        for (var i = 0; i < 3; i++)
        {
            var brush = new SolidColorBrush(TriadColors[i]);
            _triadLines[i] = new Line
            {
                Stroke = brush,
                StrokeThickness = 2.0,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
            };
            _triadLabels[i] = new TextBlock
            {
                Text = TriadLabelTexts[i],
                Foreground = brush,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
            };
            _triadCanvas.Children.Add(_triadLines[i]);
            _triadCanvas.Children.Add(_triadLabels[i]);
        }

        // Rubber-band selection rectangle following theme accent tokens
        _rubberBand = new Rectangle
        {
            StrokeThickness = 1.0,
            StrokeDashArray = [3.0, 2.0],
            Visibility = Visibility.Collapsed,
            Opacity = 0.9,
        };
        _rubberBand.SetResourceReference(Shape.StrokeProperty, "Wb.Brush.Accent.Default");
        _rubberBand.SetResourceReference(Shape.FillProperty, "Wb.Brush.State.Selected");
        _triadCanvas.Children.Add(_rubberBand);
    }

    private void UpdateTriadOverlay()
    {
        if (_triadCanvas is null || _triadLines[0] is null)
        {
            return;
        }

        // Toggle only triad elements since the canvas is shared with the rubber-band rectangle
        var visible = ShowAxisTriad && ActualWidth > TriadMargin * 2 && ActualHeight > TriadMargin * 2;
        for (var i = 0; i < 3; i++)
        {
            _triadLines[i].Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            _triadLabels[i].Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        if (!visible)
        {
            return;
        }

        var (right, up) = Camera.GetViewBasis();
        var anchorX = TriadMargin;
        var anchorY = ActualHeight - TriadMargin;

        Vector3[] axes = [Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ];
        for (var i = 0; i < 3; i++)
        {
            var sx = Vector3.Dot(axes[i], right) * TriadArmLength;
            var sy = -Vector3.Dot(axes[i], up) * TriadArmLength; // Screen Y points downward
            var endX = anchorX + sx;
            var endY = anchorY + sy;

            _triadLines[i].X1 = anchorX;
            _triadLines[i].Y1 = anchorY;
            _triadLines[i].X2 = endX;
            _triadLines[i].Y2 = endY;

            // Offset label slightly beyond the axis tip
            Canvas.SetLeft(_triadLabels[i], endX + sx * 0.18 - 4);
            Canvas.SetTop(_triadLabels[i], endY + sy * 0.18 - 8);
        }
    }

    // ================= Helpers =================

    private Color GetTokenColor(string key, Color fallback) =>
        TryFindResource(key) is Color color ? color : fallback;

    private static Vector4 ToVector4(Color color, double alpha) => new(
        color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, (float)Math.Clamp(alpha, 0.0, 1.0));

    private void ReleaseRenderer()
    {
        // Cancel any in-progress async build (stale completions are discarded on generation mismatch)
        _buildCoordinator.CancelAll();
        SetValue(IsGeometryBuildingPropertyKey, false);
        ClearHover();
        _hoverIdBuffer = null;
        _hoverIdValid = false;

        foreach (var (_, gpu) in _gpuMeshes)
        {
            gpu.Dispose();
        }

        foreach (var gpu in _selectionGpu.Values)
        {
            gpu.Dispose();
        }

        _gpuMeshes.Clear();
        _displacementDirtyMeshes.Clear();
        _scalarDirtyMeshes.Clear();
        _voxelBindings.Clear();
        _selectionGpu.Clear();
        _renderItems.Clear();
        _visibleGpus.Clear();
        _visibleSources.Clear();
        _geometryDirty = true;
        _colorMapDirty = true;
        _selectionDirty = true;
        _glyphDirty = true;
        _hasAnyLod = false;
        _lodRequested = false;
        _lodRestoreTimer?.Stop();
        _lastBackBuffer = 0;
        _d3dImage = null;
        _softwareBitmap = null;
        if (_image is not null)
        {
            _image.Source = null;
        }

        _renderer?.Dispose();
        _renderer = null;
    }
}
