using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Viewport3D;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class Viewport3DPage : UserControl
{
    private const double PlateHalfWidth = 60.0;   // [mm]
    private const double PlateHalfHeight = 40.0;  // [mm]
    private const double HoleRadius = 10.0;       // [mm]
    private const double NominalStress = 100.0;   // [MPa]

    private readonly ObservableCollection<ViewportMesh> _meshes = [];
    private readonly ColorScale _scale;
    private readonly ViewportMesh _boss;

    public Viewport3DPage()
    {
        InitializeComponent();

        // Stress concentration factor at the hole edge is 3 (Kirsch), so set range to 0..3S
        _scale = new ColorScale
        {
            ColorMap = ColorMap.Jet,
            Minimum = 0.0,
            Maximum = 3.0 * NominalStress,
        };

        _meshes.Add(CreatePlateWithHole());
        _boss = CreateBoss();
        _meshes.Add(_boss);

        Viewport.ColorScale = _scale;
        Viewport.MeshSource = _meshes;
        Viewport.SelectionChanged += OnSelectionChanged;
        Viewport.HoverChanged += OnHoverChanged;
        Legend.Scale = _scale;

        Loaded += (_, _) => RendererInfo.Text = Viewport.IsSoftwareRendering
            ? "Rendering path: WARP (software) + WriteableBitmap fallback"
            : "Rendering path: Hardware D3D11 + D3DImage (shared surface)";
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        var selection = Viewport.Selection;
        SelectionSummary.Text =
            $"Parts: {selection.PartCount}, Faces: {selection.FaceCount}, Nodes: {selection.NodeCount}";
    }

    /// <summary>Displays the hover pre-highlight target (also used by UIA verification).</summary>
    private void OnHoverChanged(object? sender, EventArgs e)
    {
        HoverText.Text = Viewport.HoverInfo switch
        {
            null => "Hover: -",
            { IsPart: true } h => $"Hover: Part {h.Mesh.Name}",
            { NodeIndex: >= 0 } h => $"Hover: Node {h.NodeIndex}",
            { } h => $"Hover: Face {h.TriangleIndex}",
        };
    }

    /// <summary>
    /// Plate with circular hole (structured ring grid radially blended to the rectangular boundary),
    /// triangulated with von Mises stress from the Kirsch analytical solution.
    /// </summary>
    private static ViewportMesh CreatePlateWithHole()
    {
        const int radialDivisions = 24;      // Hole -> outer boundary
        const int angularDivisions = 96;     // Circumferential (closed)

        var vertexCount = (radialDivisions + 1) * angularDivisions;
        var positions = new double[vertexCount * 3];
        var scalars = new double[vertexCount];

        for (var a = 0; a < angularDivisions; a++)
        {
            var theta = 2.0 * Math.PI * a / angularDivisions;
            var (cos, sin) = (Math.Cos(theta), Math.Sin(theta));

            // Distance along direction (cosθ, sinθ) to the rectangular boundary
            var kx = Math.Abs(cos) < 1e-12 ? double.MaxValue : PlateHalfWidth / Math.Abs(cos);
            var ky = Math.Abs(sin) < 1e-12 ? double.MaxValue : PlateHalfHeight / Math.Abs(sin);
            var boundaryRadius = Math.Min(kx, ky);

            for (var r = 0; r <= radialDivisions; r++)
            {
                // Concentrate radial divisions near the hole where the stress gradient is steep
                var t = (double)r / radialDivisions;
                var radius = HoleRadius + (boundaryRadius - HoleRadius) * t * t;

                var index = (a * (radialDivisions + 1) + r) * 3;
                positions[index] = radius * cos;
                positions[index + 1] = radius * sin;
                positions[index + 2] = 0.0;

                scalars[a * (radialDivisions + 1) + r] = KirschVonMises(radius, theta);
            }
        }

        var triangles = new List<int>(radialDivisions * angularDivisions * 6);
        for (var a = 0; a < angularDivisions; a++)
        {
            var nextA = (a + 1) % angularDivisions; // Wrap around circumferentially
            for (var r = 0; r < radialDivisions; r++)
            {
                var i00 = a * (radialDivisions + 1) + r;
                var i01 = a * (radialDivisions + 1) + r + 1;
                var i10 = nextA * (radialDivisions + 1) + r;
                var i11 = nextA * (radialDivisions + 1) + r + 1;

                triangles.Add(i00);
                triangles.Add(i10);
                triangles.Add(i11);
                triangles.Add(i00);
                triangles.Add(i11);
                triangles.Add(i01);
            }
        }

        return new ViewportMesh
        {
            Name = "Plate with Hole",
            Positions = positions,
            TriangleIndices = [.. triangles],
            ScalarValues = scalars,
        };
    }

    /// <summary>
    /// Plane-stress von Mises from Kirsch's exact solution (uniaxial x-tension around a circular hole in an infinite plate).
    /// </summary>
    private static double KirschVonMises(double r, double theta)
    {
        var s = NominalStress;
        var a2 = HoleRadius * HoleRadius / (r * r);
        var a4 = a2 * a2;
        var cos2T = Math.Cos(2.0 * theta);
        var sin2T = Math.Sin(2.0 * theta);

        var sigmaR = s / 2.0 * (1.0 - a2) + s / 2.0 * (1.0 - 4.0 * a2 + 3.0 * a4) * cos2T;
        var sigmaT = s / 2.0 * (1.0 + a2) - s / 2.0 * (1.0 + 3.0 * a4) * cos2T;
        var tauRT = -s / 2.0 * (1.0 + 2.0 * a2 - 3.0 * a4) * sin2T;

        return Math.Sqrt(sigmaR * sigmaR + sigmaT * sigmaT - sigmaR * sigmaT + 3.0 * tauRT * tauRT);
    }

    /// <summary>Cylindrical boss passing through the hole (solid-color part demonstrating multi-part display).</summary>
    private static ViewportMesh CreateBoss()
    {
        const int segments = 48;
        const double radius = HoleRadius * 0.72;
        const double halfLength = 18.0;

        // Side wall + top/bottom disk caps (triangle fans)
        var vertexCount = segments * 2 + 2;
        var positions = new double[vertexCount * 3];
        for (var i = 0; i < segments; i++)
        {
            var theta = 2.0 * Math.PI * i / segments;
            var (x, y) = (radius * Math.Cos(theta), radius * Math.Sin(theta));
            positions[i * 3] = x;
            positions[i * 3 + 1] = y;
            positions[i * 3 + 2] = halfLength;
            positions[(segments + i) * 3] = x;
            positions[(segments + i) * 3 + 1] = y;
            positions[(segments + i) * 3 + 2] = -halfLength;
        }

        var topCenter = segments * 2;
        var bottomCenter = segments * 2 + 1;
        positions[topCenter * 3 + 2] = halfLength;
        positions[bottomCenter * 3 + 2] = -halfLength;

        var triangles = new List<int>(segments * 12);
        for (var i = 0; i < segments; i++)
        {
            var next = (i + 1) % segments;

            // Side wall
            triangles.Add(i);
            triangles.Add(segments + i);
            triangles.Add(segments + next);
            triangles.Add(i);
            triangles.Add(segments + next);
            triangles.Add(next);

            // Top / bottom caps
            triangles.Add(topCenter);
            triangles.Add(i);
            triangles.Add(next);
            triangles.Add(bottomCenter);
            triangles.Add(segments + next);
            triangles.Add(segments + i);
        }

        return new ViewportMesh
        {
            Name = "Cylindrical Boss",
            Positions = positions,
            TriangleIndices = [.. triangles],
            Color = Color.FromRgb(0x8A, 0x9B, 0xB0),
            ShowEdges = false,
        };
    }

    // Note: Toggles with IsChecked="True" raise Checked during XAML parsing (before field initialization),
    // so handlers must guard against null fields.

    private void OnOrthoChanged(object sender, RoutedEventArgs e)
    {
        if (Viewport is not null)
        {
            Viewport.Projection = OrthoToggle.IsChecked == true
                ? ViewportProjection.Orthographic
                : ViewportProjection.Perspective;
        }
    }

    private void OnDiscreteChanged(object sender, RoutedEventArgs e)
    {
        if (_scale is not null)
        {
            _scale.LevelCount = DiscreteToggle.IsChecked == true ? 10 : null;
        }
    }

    private void OnBossChanged(object sender, RoutedEventArgs e)
    {
        if (_boss is not null)
        {
            _boss.IsVisible = BossToggle.IsChecked == true;
        }
    }

    private void OnFitClick(object sender, RoutedEventArgs e) => Viewport.FitToView();

    private void OnPickModeChanged(object sender, RoutedEventArgs e)
    {
        if (Viewport is null)
        {
            return;
        }

        Viewport.PickMode = true switch
        {
            _ when PickPartRadio.IsChecked == true => ViewportPickMode.Part,
            _ when PickFaceRadio.IsChecked == true => ViewportPickMode.Face,
            _ when PickNodeRadio.IsChecked == true => ViewportPickMode.Node,
            _ => ViewportPickMode.None,
        };
    }

    private void OnClearSelectionClick(object sender, RoutedEventArgs e) => Viewport.Selection.Clear();

    private void OnHoverToggleChanged(object sender, RoutedEventArgs e)
    {
        if (Viewport is not null)
        {
            Viewport.IsHoverHighlightEnabled = HoverToggle.IsChecked == true;
        }
    }

    private void OnThroughToggleChanged(object sender, RoutedEventArgs e)
    {
        if (Viewport is not null)
        {
            Viewport.RubberBandSelectionMode = ThroughToggle.IsChecked == true
                ? ViewportRubberBandSelectionMode.Through
                : ViewportRubberBandSelectionMode.VisibleOnly;
        }
    }

    private void OnStandardViewClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string tag }
            && Enum.TryParse<ViewportStandardView>(tag, out var view))
        {
            Viewport.SetStandardView(view);
        }
    }
}
