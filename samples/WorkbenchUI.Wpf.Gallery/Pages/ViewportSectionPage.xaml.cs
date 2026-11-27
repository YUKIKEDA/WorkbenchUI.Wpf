using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Viewport3D;

namespace WorkbenchUI.Wpf.Gallery.Pages;

/// <summary>
/// Section cut (clipping plane) demo.
/// Cuts a pressurized thick-walled cylinder (Lamé analytical solution with von Mises contours)
/// using a SectionPlane configured via axis preset, offset, and flip. Because cut surfaces are
/// open, also demonstrates overlaying an application-computed intersection slice (IsClippable=false).
/// </summary>
public partial class ViewportSectionPage : UserControl
{
    private const double InnerRadius = 30.0;  // a [mm]
    private const double OuterRadius = 50.0;  // b [mm]
    private const double HalfLength = 60.0;   // Cylinder z in [-HL, +HL]
    private const double Pressure = 100.0;    // Internal pressure p [MPa]
    private const int ThetaDivisions = 64;
    private const int AxialDivisions = 24;
    private const int RadialDivisions = 8;

    private readonly ObservableCollection<ViewportMesh> _meshes = [];
    private readonly SectionPlane _plane = new();
    private readonly ColorScale _scale;
    private ViewportMesh? _slice;

    public ViewportSectionPage()
    {
        InitializeComponent();

        _scale = new ColorScale
        {
            ColorMap = ColorMap.Jet,
            Minimum = Math.Floor(VonMises(OuterRadius)),
            Maximum = Math.Ceiling(VonMises(InnerRadius)),
        };

        _meshes.Add(CreateThickCylinder());

        Viewport.ColorScale = _scale;
        Viewport.MeshSource = _meshes;
        Viewport.SelectionChanged += OnSelectionChanged;
        Viewport.GeometryBuildCompleted += (_, _) => UpdateRendererInfo();
        Legend.Scale = _scale;

        UpdateSection();

        Loaded += (_, _) =>
        {
            UpdateRendererInfo();
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, UpdateRendererInfo);
        };
    }

    private void UpdateRendererInfo() => RendererInfo.Text = Viewport.IsSoftwareRendering
        ? "Rendering path: WARP (software) + WriteableBitmap fallback"
        : "Rendering path: Hardware D3D11 + D3DImage (shared surface)";

    // ================= Lamé solution (pressurized thick-walled cylinder, closed-end) =================

    /// <summary>von Mises stress at radius r: σr = A − B/r², σθ = A + B/r², σz = A.</summary>
    private static double VonMises(double r)
    {
        var a2 = InnerRadius * InnerRadius;
        var b2 = OuterRadius * OuterRadius;
        var coefA = Pressure * a2 / (b2 - a2);
        var coefB = coefA * b2;

        var sigmaR = coefA - coefB / (r * r);
        var sigmaT = coefA + coefB / (r * r);
        var sigmaZ = coefA;

        var d1 = sigmaR - sigmaT;
        var d2 = sigmaT - sigmaZ;
        var d3 = sigmaZ - sigmaR;
        return Math.Sqrt(0.5 * (d1 * d1 + d2 * d2 + d3 * d3));
    }

    // ================= Geometry generation =================

    /// <summary>Builds the thick-walled cylinder (outer wall + inner bore + annular caps) as a single part.</summary>
    private static ViewportMesh CreateThickCylinder()
    {
        var builder = new MeshBuilder();

        // Outer and inner walls (θ × z grid)
        builder.AppendGrid(ThetaDivisions, AxialDivisions, (u, v) =>
        {
            var theta = 2.0 * Math.PI * u;
            var z = -HalfLength + 2.0 * HalfLength * v;
            return (OuterRadius * Math.Cos(theta), OuterRadius * Math.Sin(theta), z, VonMises(OuterRadius));
        });
        builder.AppendGrid(ThetaDivisions, AxialDivisions, (u, v) =>
        {
            var theta = 2.0 * Math.PI * u;
            var z = -HalfLength + 2.0 * HalfLength * v;
            return (InnerRadius * Math.Cos(theta), InnerRadius * Math.Sin(theta), z, VonMises(InnerRadius));
        });

        // Annular end caps (θ × r grid)
        foreach (var zEnd in (double[])[-HalfLength, HalfLength])
        {
            builder.AppendGrid(ThetaDivisions, RadialDivisions, (u, v) =>
            {
                var theta = 2.0 * Math.PI * u;
                var r = InnerRadius + (OuterRadius - InnerRadius) * v;
                return (r * Math.Cos(theta), r * Math.Sin(theta), zEnd, VonMises(r));
            });
        }

        var mesh = builder.ToMesh();
        mesh.Name = "Thick-Walled Cylinder";
        return mesh;
    }

    /// <summary>
    /// Builds the section slice (intersection of the cut plane with the cylinder). Because the
    /// library leaves cut openings hollow, applications wanting cross-section contours mesh the
    /// intersection region and add it with IsClippable=false so it is excluded from clipping.
    /// </summary>
    private static ViewportMesh? CreateSlice(int axis, double offset)
    {
        var builder = new MeshBuilder();

        if (axis == 2)
        {
            // Cut perpendicular to axis: intersection is an annulus (empty outside cylinder length)
            if (Math.Abs(offset) >= HalfLength)
            {
                return null;
            }

            builder.AppendGrid(ThetaDivisions, RadialDivisions, (u, v) =>
            {
                var theta = 2.0 * Math.PI * u;
                var r = InnerRadius + (OuterRadius - InnerRadius) * v;
                return (r * Math.Cos(theta), r * Math.Sin(theta), offset, VonMises(r));
            });
        }
        else
        {
            // Cut parallel to axis (normal X or Y): intersection strips through wall thickness.
            // Two strips inside the bore (|c| < a), one strip within the wall (a <= |c| < b).
            if (Math.Abs(offset) >= OuterRadius)
            {
                return null;
            }

            var tOuter = Math.Sqrt(OuterRadius * OuterRadius - offset * offset);
            var strips = Math.Abs(offset) < InnerRadius
                ? new (double Min, double Max)[]
                {
                    (Math.Sqrt(InnerRadius * InnerRadius - offset * offset), tOuter),
                    (-tOuter, -Math.Sqrt(InnerRadius * InnerRadius - offset * offset)),
                }
                : [(-tOuter, tOuter)];

            foreach (var (tMin, tMax) in strips)
            {
                builder.AppendGrid(12, 16, (u, v) =>
                {
                    var t = tMin + (tMax - tMin) * u;
                    var z = -HalfLength + 2.0 * HalfLength * v;
                    var scalar = VonMises(Math.Sqrt(offset * offset + t * t));
                    return axis == 0 ? (offset, t, z, scalar) : (t, offset, z, scalar);
                });
            }
        }

        var mesh = builder.ToMesh();
        mesh.Name = "Section Slice";
        mesh.IsClippable = false; // Exclude from clipping (otherwise the slice itself would be clipped)
        mesh.ShowEdges = false;
        return mesh;
    }

    /// <summary>Small helper that combines multiple structured quad patches into a single ViewportMesh.</summary>
    private sealed class MeshBuilder
    {
        private readonly List<double> _positions = [];
        private readonly List<int> _triangles = [];
        private readonly List<double> _scalars = [];

        public void AppendGrid(int uDivisions, int vDivisions, Func<double, double, (double X, double Y, double Z, double Scalar)> sample)
        {
            var baseIndex = _positions.Count / 3;

            for (var i = 0; i <= uDivisions; i++)
            {
                for (var j = 0; j <= vDivisions; j++)
                {
                    var (x, y, z, scalar) = sample((double)i / uDivisions, (double)j / vDivisions);
                    _positions.Add(x);
                    _positions.Add(y);
                    _positions.Add(z);
                    _scalars.Add(scalar);
                }
            }

            for (var i = 0; i < uDivisions; i++)
            {
                for (var j = 0; j < vDivisions; j++)
                {
                    var i00 = baseIndex + i * (vDivisions + 1) + j;
                    var i01 = i00 + 1;
                    var i10 = i00 + (vDivisions + 1);
                    var i11 = i10 + 1;
                    _triangles.AddRange([i00, i10, i11, i00, i11, i01]);
                }
            }
        }

        public ViewportMesh ToMesh() => new()
        {
            Positions = [.. _positions],
            TriangleIndices = [.. _triangles],
            ScalarValues = [.. _scalars],
        };
    }

    // ================= Section update =================

    /// <summary>Rebuilds the SectionPlane and slice mesh from current UI state.</summary>
    private void UpdateSection()
    {
        var axis = AxisCombo.SelectedIndex; // 0=X, 1=Y, 2=Z
        var offset = OffsetSlider.Value;

        // Default normal is -axis so the cut face points toward the default isometric camera (+X+Y quadrant),
        // revealing both the bore and the section slice. Flip reverses to +axis.
        var sign = FlipToggle.IsChecked == true ? 1.0 : -1.0;

        _plane.OriginX = axis == 0 ? offset : 0.0;
        _plane.OriginY = axis == 1 ? offset : 0.0;
        _plane.OriginZ = axis == 2 ? offset : 0.0;
        _plane.NormalX = axis == 0 ? sign : 0.0;
        _plane.NormalY = axis == 1 ? sign : 0.0;
        _plane.NormalZ = axis == 2 ? sign : 0.0;

        var clipEnabled = ClipToggle.IsChecked == true;
        Viewport.SectionPlane = clipEnabled ? _plane : null;

        // Recreate the slice whenever the plane axis or offset changes
        if (_slice is not null)
        {
            _meshes.Remove(_slice);
            _slice = null;
        }

        if (clipEnabled && SliceToggle.IsChecked == true)
        {
            _slice = CreateSlice(axis, offset);
            if (_slice is not null)
            {
                _meshes.Add(_slice);
            }
        }
    }

    // Note: Events may fire during XAML parsing (before field initialization), so guard against null.

    private void OnSectionOptionChanged(object sender, RoutedEventArgs e)
    {
        if (_meshes.Count > 0)
        {
            UpdateSection();
        }
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        var selection = Viewport.Selection;
        SelectionSummary.Text =
            $"Parts: {selection.PartCount}, Faces: {selection.FaceCount}, Nodes: {selection.NodeCount}";
    }

    private void OnFitClick(object sender, RoutedEventArgs e) => Viewport.FitToView();

    private void OnClearSelectionClick(object sender, RoutedEventArgs e) => Viewport.Selection.Clear();
}
