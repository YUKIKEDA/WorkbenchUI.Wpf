using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AvalonDock.Layout;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Docking;
using WorkbenchUI.Wpf.Viewport3D;

namespace WorkbenchUI.Wpf.Gallery
{
    /// <summary>
    /// Integrated mini CAE shell demo.
    /// Hosts two live WorkbenchViewport documents (Static Kirsch / Transient Cantilever) in the center,
    /// serving as a full reference implementation and interoperability showcase with two-way ModelTree
    /// selection sync, PropertyGrid selection binding, Probe-to-LogConsole logging, shared
    /// ColorMapLegend + ColorScaleEditor, PlaybackBar, and StatusBar synchronization.
    /// </summary>
    public partial class DockingShellWindow : WorkbenchWindow
    {
        private static readonly string LayoutPath =
            Path.Combine(Path.GetTempPath(), "WorkbenchUI.Wpf.Gallery.docklayout.xml");

        /// <summary>Per-document state (viewport, color scale, and tree node mapping).</summary>
        private sealed record ShellDocument(
            string Title,
            string LegendTitle,
            WorkbenchViewport Viewport,
            ColorScale Scale,
            FrameworkElement Content,
            Dictionary<TreeNode, ViewportMesh> NodeToMesh,
            Dictionary<ViewportMesh, TreeNode> MeshToNode);

        private readonly LogBuffer _logBuffer = new(capacity: 2000);
        private readonly Dictionary<string, object> _contentMap;
        private readonly List<ShellDocument> _documents = [];
        private readonly ObservableCollection<ViewportMesh> _staticMeshes = [];
        private readonly ObservableCollection<ViewportMesh> _transientMeshes = [];
        private readonly ViewportMesh _transientPlate;
        private readonly double[][] _transientFrames;
        private string? _defaultLayout;
        private ShellDocument? _activeDocument;

        /// <summary>Re-entrancy guard for two-way selection synchronization between Tree and Viewport.</summary>
        private bool _syncingSelection;

        public DockingShellWindow()
        {
            InitializeComponent();

            // ContentId -> pane content mapping referenced by the layout restore resolver
            _contentMap = new Dictionary<string, object>
            {
                ["Shell.ModelTree"] = Tree,
                ["Shell.Properties"] = Props,
                ["Shell.Legend"] = LegendHost,
                ["Shell.Log"] = Console,
                ["Shell.Doc.Case1"] = StaticDoc,
                ["Shell.Doc.Case2"] = TransientDoc,
            };

            // ---- Static document (Kirsch plate with hole + boss) ----
            var staticScene = ShellScenes.CreateStaticScene();
            _staticMeshes.Add(staticScene.Plate);
            _staticMeshes.Add(staticScene.Boss);

            var staticScale = new ColorScale
            {
                ColorMap = ColorMap.Jet,
                Minimum = 0.0,
                Maximum = 3.0 * ShellScenes.NominalStress, // Stress concentration factor of 3 at hole edge
            };
            ViewportStatic.ColorScale = staticScale;
            ViewportStatic.MeshSource = _staticMeshes;

            // Formatted label with physical units (application responsibility). Invariant so UIA can parse values.
            ViewportStatic.ProbeLabelFormatter = r =>
                FormattableString.Invariant($"σ_vM = {r.ScalarValue:0.0} MPa (N{r.NodeIndex})");
            ViewportStatic.ProbePicked += OnProbePicked;

            // ---- Transient document (damped free vibration of cantilever beam) ----
            var transientScene = ShellScenes.CreateTransientScene();
            _transientPlate = transientScene.Plate;
            _transientFrames = transientScene.Frames;
            _transientMeshes.Add(_transientPlate);

            var transientScale = new ColorScale
            {
                ColorMap = ColorMap.Jet,
                Minimum = 0.0,
                Maximum = ShellScenes.TipAmplitude,
            };
            ViewportTransient.ColorScale = transientScale;
            ViewportTransient.MeshSource = _transientMeshes;
            Playback.FrameCount = ShellScenes.TransientFrameCount;

            // ---- Document contexts + ModelTree ----
            _documents.Add(CreateDocument(
                "Static: Plate with Hole", "Stress [MPa]", ViewportStatic, staticScale, StaticDoc,
                [staticScene.Plate, staticScene.Boss]));
            _documents.Add(CreateDocument(
                "Transient: Cantilever Beam", "|u| [mm]", ViewportTransient, transientScale, TransientDoc,
                [_transientPlate]));

            Tree.ItemsSource = BuildModelTree();
            Tree.SelectionChanged += OnTreeSelectionChanged;

            foreach (var document in _documents)
            {
                var doc = document;
                doc.Viewport.SelectionChanged += (_, _) => OnViewportSelectionChanged(doc);
                doc.Viewport.HoverChanged += (_, _) => OnViewportHoverChanged(doc);
                doc.Viewport.GeometryBuildCompleted += (_, _) => OnGeometryBuildCompleted(doc);
            }

            // ---- Legend + ColorScaleEditor (shared with the active document) ----
            SetActiveDocument(_documents[0]);
            Dock.ActiveContentChanged += OnActiveContentChanged;

            ShowViewportProperties(_documents[0]);

            Console.Source = _logBuffer;
            _logBuffer.Append(LogLevel.Info, "Initialized integrated CAE shell (2 live WorkbenchViewport documents).");
            _logBuffer.Append(LogLevel.Info, "Tree selection <-> 3D part picking are synchronized two-way; probe results are logged here.");

            // Capture the initial layout as default for reset
            Loaded += (_, _) => _defaultLayout ??= DockLayout.SaveToString(Dock);
        }

        // ================= ModelTree construction & synchronization =================

        private ShellDocument CreateDocument(
            string title, string legendTitle, WorkbenchViewport viewport, ColorScale scale,
            FrameworkElement content, IReadOnlyList<ViewportMesh> parts)
        {
            var nodeToMesh = new Dictionary<TreeNode, ViewportMesh>();
            var meshToNode = new Dictionary<ViewportMesh, TreeNode>();
            foreach (var mesh in parts)
            {
                var node = new TreeNode { Name = mesh.Name ?? "(Unnamed)", IsVisible = mesh.IsVisible };
                node.PropertyChanged += OnTreeNodePropertyChanged;
                nodeToMesh[node] = mesh;
                meshToNode[mesh] = node;
            }

            return new ShellDocument(title, legendTitle, viewport, scale, content, nodeToMesh, meshToNode);
        }

        private TreeNode[] BuildModelTree() =>
            [.. _documents.Select(doc =>
            {
                var root = new TreeNode { Name = doc.Title, IsExpanded = true };
                foreach (var node in doc.NodeToMesh.Keys)
                {
                    root.Children.Add(node);
                }

                return root;
            })];

        /// <summary>Propagates eye icon (visibility) and F2 rename changes to the mesh.</summary>
        private void OnTreeNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not TreeNode node || FindMesh(node) is not { } mesh)
            {
                return;
            }

            if (e.PropertyName == nameof(TreeNode.IsVisible))
            {
                mesh.IsVisible = node.IsVisible != false;
            }
            else if (e.PropertyName == nameof(TreeNode.Name))
            {
                mesh.Name = node.Name;
            }
        }

        private ViewportMesh? FindMesh(TreeNode node) =>
            _documents.Select(d => d.NodeToMesh.GetValueOrDefault(node)).FirstOrDefault(m => m is not null);

        /// <summary>Tree selection -> viewport part selection.</summary>
        private void OnTreeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingSelection)
            {
                return;
            }

            _syncingSelection = true;
            try
            {
                var selected = Tree.GetSelectedNodes().OfType<TreeNode>().ToHashSet();
                foreach (var doc in _documents)
                {
                    doc.Viewport.Selection.Clear();
                    foreach (var (node, mesh) in doc.NodeToMesh)
                    {
                        if (selected.Contains(node))
                        {
                            doc.Viewport.Selection.AddPart(mesh);
                        }
                    }
                }

                UpdatePropertyTarget();
                UpdateSelectionStatus();
            }
            finally
            {
                _syncingSelection = false;
            }
        }

        /// <summary>Viewport picking -> tree selection.</summary>
        private void OnViewportSelectionChanged(ShellDocument doc)
        {
            if (_syncingSelection)
            {
                UpdateSelectionStatus();
                return;
            }

            _syncingSelection = true;
            try
            {
                // Reflect only this document's part selection in the tree without touching other documents' nodes
                foreach (var (node, mesh) in doc.NodeToMesh)
                {
                    node.IsSelected = doc.Viewport.Selection.IsPartSelected(mesh);
                }

                UpdatePropertyTarget();
                UpdateSelectionStatus();
            }
            finally
            {
                _syncingSelection = false;
            }
        }

        private void UpdateSelectionStatus()
        {
            var parts = _documents.Sum(d => d.Viewport.Selection.PartCount);
            var faces = _documents.Sum(d => d.Viewport.Selection.FaceCount);
            var nodes = _documents.Sum(d => d.Viewport.Selection.NodeCount);
            SelectionStatus.Text = parts + faces + nodes == 0
                ? "Selection: None"
                : string.Create(CultureInfo.InvariantCulture,
                    $"Selection: {parts} part(s) / {faces} face(s) / {nodes} node(s)");
        }

        private void OnViewportHoverChanged(ShellDocument doc)
        {
            HoverStatus.Text = doc.Viewport.HoverInfo switch
            {
                null => "Hover: -",
                { IsPart: true } h => $"Hover: {h.Mesh.Name}",
                { NodeIndex: >= 0 } h => $"Hover: {h.Mesh.Name} Node {h.NodeIndex}",
                { } h => $"Hover: {h.Mesh.Name} Face {h.TriangleIndex}",
            };
        }

        // ================= PropertyGrid selection binding =================

        /// <summary>Shows properties of the selected part, or the active document's viewport settings if none.</summary>
        private void UpdatePropertyTarget()
        {
            foreach (var doc in _documents)
            {
                var mesh = doc.Viewport.Selection.SelectedParts.FirstOrDefault();
                if (mesh is not null && doc.MeshToNode.TryGetValue(mesh, out var node))
                {
                    ShowPartProperties(doc, mesh, node);
                    return;
                }
            }

            ShowViewportProperties(_activeDocument ?? _documents[0]);
        }

        /// <summary>Display properties of the selected part (edits apply to 3D immediately).</summary>
        private void ShowPartProperties(ShellDocument doc, ViewportMesh mesh, TreeNode node)
        {
            var name = new TextPropertyItem
            {
                Name = "Name", Category = "General", Value = mesh.Name,
                Description = "Part name (shared with the model tree)",
            };
            name.PropertyChanged += OnValueChanged(name, () =>
            {
                if (!string.IsNullOrWhiteSpace(name.Value))
                {
                    node.Name = name.Value; // Propagates to mesh via node
                }
            });

            var visible = new BoolPropertyItem
            {
                Name = "Visible", Category = "General", Value = mesh.IsVisible,
                Description = "Same visibility as the tree eye icon",
            };
            visible.PropertyChanged += OnValueChanged(visible, () => Tree.SetVisibility(node, visible.Value));

            var color = new ColorPropertyItem
            {
                Name = "Part Color", Category = "Display", Value = mesh.Color, IsAlphaEnabled = false,
                Description = "Solid color for parts without scalar contours",
            };
            color.PropertyChanged += OnValueChanged(color, () => mesh.Color = color.Value);

            var opacity = new NumericPropertyItem
            {
                Name = "Opacity", Category = "Display", Value = mesh.Opacity,
                Minimum = 0.1, Maximum = 1.0, Increment = 0.1, Format = "0.0",
                Description = "1.0 is fully opaque",
            };
            opacity.PropertyChanged += OnValueChanged(opacity, () =>
            {
                if (opacity.Value is { } v)
                {
                    mesh.Opacity = v;
                }
            });

            var edges = new BoolPropertyItem
            {
                Name = "Show Edges", Category = "Display", Value = mesh.ShowEdges,
            };
            edges.PropertyChanged += OnValueChanged(edges, () => mesh.ShowEdges = edges.Value);

            Props.ItemsSource = new PropertyItem[] { name, visible, color, opacity, edges };
        }

        /// <summary>No selection: shows viewport settings for the active document.</summary>
        private void ShowViewportProperties(ShellDocument doc)
        {
            var viewport = doc.Viewport;

            var deformScale = new NumericPropertyItem
            {
                Name = "Deformation Scale", Category = "Viewport", Value = viewport.DeformationScale,
                Minimum = 0.0, Maximum = 1000.0, Increment = 1.0, Format = "0.#",
                Description = "Applies displacement * scale in the GPU vertex shader",
            };
            deformScale.PropertyChanged += OnValueChanged(deformScale, () =>
            {
                if (deformScale.Value is { } v)
                {
                    viewport.DeformationScale = v;
                }
            });

            var ortho = new BoolPropertyItem
            {
                Name = "Orthographic", Category = "Viewport",
                Value = viewport.Projection == ViewportProjection.Orthographic,
            };
            ortho.PropertyChanged += OnValueChanged(ortho, () => viewport.Projection =
                ortho.Value ? ViewportProjection.Orthographic : ViewportProjection.Perspective);

            var contours = new BoolPropertyItem
            {
                Name = "Contours", Category = "Viewport", Value = viewport.ShowContours,
            };
            contours.PropertyChanged += OnValueChanged(contours, () => viewport.ShowContours = contours.Value);

            var hover = new BoolPropertyItem
            {
                Name = "Hover Pre-highlight", Category = "Viewport",
                Value = viewport.IsHoverHighlightEnabled,
            };
            hover.PropertyChanged += OnValueChanged(hover, () => viewport.IsHoverHighlightEnabled = hover.Value);

            Props.ItemsSource = new PropertyItem[] { deformScale, ortho, contours, hover };
        }

        /// <summary>Creates a PropertyChanged handler that invokes apply only when Value changes.</summary>
        private static PropertyChangedEventHandler OnValueChanged(PropertyItem item, Action apply) =>
            (_, e) =>
            {
                if (e.PropertyName == nameof(TextPropertyItem.Value))
                {
                    apply();
                }
            };

        // ================= Active document synchronization =================

        private void OnActiveContentChanged(object? sender, EventArgs e)
        {
            var doc = _documents.FirstOrDefault(d => ReferenceEquals(Dock.ActiveContent, d.Content));
            if (doc is not null && !ReferenceEquals(doc, _activeDocument))
            {
                SetActiveDocument(doc);
                UpdatePropertyTarget();
            }
        }

        private void SetActiveDocument(ShellDocument doc)
        {
            _activeDocument = doc;
            Legend.Scale = doc.Scale;
            Legend.Title = doc.LegendTitle;
            ScaleEditor.Scale = doc.Scale;
            UpdateStatsStatus();
        }

        private void OnGeometryBuildCompleted(ShellDocument doc)
        {
            if (ReferenceEquals(doc, _activeDocument))
            {
                UpdateStatsStatus();
            }
        }

        private void UpdateStatsStatus()
        {
            if (_activeDocument is not { } doc)
            {
                return;
            }

            var stats = doc.Viewport.GetStatistics();
            StatsStatus.Text = string.Create(
                CultureInfo.InvariantCulture,
                $"{doc.Title}: {stats.TriangleCount:N0} triangles / Build {stats.LastGeometryBuildTime.TotalMilliseconds:N0} ms");
        }

        // ================= Probe -> Log =================

        private void OnProbePicked(object? sender, ProbePickedEventArgs e)
        {
            // Leave Handled false to keep the library's default annotation behavior
            var label = ViewportStatic.ProbeLabelFormatter?.Invoke(e.Result) ?? e.Result.ToString();
            _logBuffer.Append(LogLevel.Info, $"Probe: {label} @ ({e.Result.X:0.0}, {e.Result.Y:0.0}, {e.Result.Z:0.0})");
        }

        // ================= In-document UI =================

        private void OnPickModeChanged(object sender, RoutedEventArgs e)
        {
            if (ViewportStatic is null)
            {
                return;
            }

            ViewportStatic.PickMode = true switch
            {
                _ when PickPartRadio.IsChecked == true => ViewportPickMode.Part,
                _ when PickFaceRadio.IsChecked == true => ViewportPickMode.Face,
                _ when PickNodeRadio.IsChecked == true => ViewportPickMode.Node,
                _ when PickProbeRadio.IsChecked == true => ViewportPickMode.Probe,
                _ => ViewportPickMode.None,
            };
        }

        private void OnClearSelection(object sender, RoutedEventArgs e)
        {
            foreach (var doc in _documents)
            {
                doc.Viewport.Selection.Clear();
            }
        }

        private void OnFitStatic(object sender, RoutedEventArgs e) => ViewportStatic.FitToView();

        private void OnFitTransient(object sender, RoutedEventArgs e) => ViewportTransient.FitToView();

        private void OnPlaybackFrameChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
        {
            if (_transientFrames is null || e.NewValue < 0 || e.NewValue >= _transientFrames.Length)
            {
                return;
            }

            var t = ShellScenes.TransientDuration * e.NewValue / (ShellScenes.TransientFrameCount - 1);
            Playback.FrameLabel = $"t = {t:0.00} s";

            // Swapping Displacements performs a lightweight partial update of the displacement buffer only
            _transientPlate.Displacements = _transientFrames[e.NewValue];
        }

        // ================= Layout / Tool Windows =================

        private void OnSaveLayout(object sender, RoutedEventArgs e)
        {
            DockLayout.Save(Dock, LayoutPath);
            _logBuffer.Append(LogLevel.Info, $"Layout saved: {LayoutPath}");
            StatusMessage.Content = "Layout saved";
        }

        private void OnLoadLayout(object sender, RoutedEventArgs e)
        {
            if (DockLayout.Load(Dock, LayoutPath, ResolveContent))
            {
                _logBuffer.Append(LogLevel.Info, "Restored saved layout.");
                StatusMessage.Content = "Layout restored";
            }
            else
            {
                _logBuffer.Append(LogLevel.Warning, "No saved layout found (run \"Save Layout\" first).");
                StatusMessage.Content = "No saved layout found";
            }
        }

        private void OnResetLayout(object sender, RoutedEventArgs e)
        {
            if (_defaultLayout is not null)
            {
                DockLayout.LoadFromString(Dock, _defaultLayout, ResolveContent);
                _logBuffer.Append(LogLevel.Info, "Reset to default layout.");
                StatusMessage.Content = "Reset to default layout";
            }
        }

        private void OnShowToolWindow(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem { Tag: string contentId })
            {
                return;
            }

            // Search both visible and hidden tool windows
            var anchorable = Dock.Layout.Descendents().OfType<LayoutAnchorable>()
                .Concat(Dock.Layout.Hidden)
                .FirstOrDefault(a => a.ContentId == contentId);

            if (anchorable is not null)
            {
                anchorable.Show();
                anchorable.IsActive = true;
            }
        }

        private object? ResolveContent(string contentId) => _contentMap.GetValueOrDefault(contentId);
    }
}
