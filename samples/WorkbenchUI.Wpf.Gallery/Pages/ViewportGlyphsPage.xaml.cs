using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Viewport3D;

namespace WorkbenchUI.Wpf.Gallery.Pages;

/// <summary>
/// Vector glyphs demo. Renders the radial displacement field u(r) = Ar + B/r of a pressurized
/// thick-walled cylinder (Lamé analytical solution) as 3D arrows. Since every vector points
/// radially, direction, magnitude, and color mapping can be verified visually. Includes controls
/// for scale, stride, deformation tracking (same array in Displacements), and section clipping.
/// </summary>
public partial class ViewportGlyphsPage : UserControl
{
    private const double InnerRadius = 30.0;   // a [mm]
    private const double OuterRadius = 50.0;   // b [mm]
    private const double HalfLength = 60.0;    // Cylinder z in [-HL, +HL]
    private const double Pressure = 100.0;     // Internal pressure p [MPa]
    private const double YoungsModulus = 200000.0; // E [MPa]
    private const double PoissonRatio = 0.3;   // ν

    private readonly ObservableCollection<ViewportMesh> _meshes = [];
    private readonly SectionPlane _plane = new()
    {
        // Normal = -Y so the inner bore faces the default isometric camera
        NormalX = 0.0,
        NormalY = -1.0,
        NormalZ = 0.0,
    };

    private readonly ColorScale _glyphScale;
    private double _suggestedGlyphScale = 1.0;

    public ViewportGlyphsPage()
    {
        InitializeComponent();

        // Dedicated color scale for glyphs (|u| [mm], independent of contour ColorScale)
        _glyphScale = new ColorScale
        {
            ColorMap = ColorMap.Jet,
            Minimum = Math.Floor(RadialDisplacement(OuterRadius) * 1000.0) / 1000.0,
            Maximum = Math.Ceiling(RadialDisplacement(InnerRadius) * 1000.0) / 1000.0,
        };

        _meshes.Add(CreateThickCylinder());

        Viewport.MeshSource = _meshes;
        Viewport.GlyphColorScale = _glyphScale;
        Viewport.GlyphStride = 4; // Matches StrideCombo default selection
        Viewport.GeometryBuildCompleted += (_, _) => UpdateRendererInfo();
        GlyphLegend.Scale = _glyphScale;

        Loaded += (_, _) =>
        {
            // Scale relative to the suggested scale (max arrow = 5% of model diagonal)
            _suggestedGlyphScale = Viewport.GetSuggestedGlyphScale();
            ApplyGlyphScale();
            UpdateRendererInfo();
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, UpdateRendererInfo);
        };
    }

    private void UpdateRendererInfo() => RendererInfo.Text = Viewport.IsSoftwareRendering
        ? "Rendering path: WARP (software) + WriteableBitmap fallback"
        : "Rendering path: Hardware D3D11 + D3DImage (shared surface)";

    // ================= Lamé solution (thick-walled cylinder under internal pressure) =================

    /// <summary>u(r) = (pa² / E(b²−a²))·((1−ν)r + (1+ν)b²/r) (plane stress).</summary>
    private static double RadialDisplacement(double r)
    {
        var a2 = InnerRadius * InnerRadius;
        var b2 = OuterRadius * OuterRadius;
        var coef = Pressure * a2 / (YoungsModulus * (b2 - a2));
        return coef * ((1.0 - PoissonRatio) * r + (1.0 + PoissonRatio) * b2 / r);
    }

    // ================= Geometry generation =================

    /// <summary>
    /// Builds a thick-walled cylinder (outer wall + inner bore + annular end caps) as a single part
    /// and assigns radial displacement vectors to every node. Both VectorValues (glyphs) and
    /// Displacements (deformation tracking) receive the same array.
    /// </summary>
    private static ViewportMesh CreateThickCylinder()
    {
        const int thetaDivisions = 64;
        const int axialDivisions = 24;
        const int radialDivisions = 8;

        var positions = new List<double>();
        var triangles = new List<int>();

        void AppendGrid(int uDiv, int vDiv, Func<double, double, (double X, double Y, double Z)> sample)
        {
            var baseIndex = positions.Count / 3;
            for (var i = 0; i <= uDiv; i++)
            {
                for (var j = 0; j <= vDiv; j++)
                {
                    var (x, y, z) = sample((double)i / uDiv, (double)j / vDiv);
                    positions.Add(x);
                    positions.Add(y);
                    positions.Add(z);
                }
            }

            for (var i = 0; i < uDiv; i++)
            {
                for (var j = 0; j < vDiv; j++)
                {
                    var i00 = baseIndex + i * (vDiv + 1) + j;
                    var i01 = i00 + 1;
                    var i10 = i00 + (vDiv + 1);
                    var i11 = i10 + 1;
                    triangles.AddRange([i00, i10, i11, i00, i11, i01]);
                }
            }
        }

        // Outer and inner cylindrical walls (θ × z grid)
        foreach (var radius in (double[])[OuterRadius, InnerRadius])
        {
            AppendGrid(thetaDivisions, axialDivisions, (u, v) =>
            {
                var theta = 2.0 * Math.PI * u;
                var z = -HalfLength + 2.0 * HalfLength * v;
                return (radius * Math.Cos(theta), radius * Math.Sin(theta), z);
            });
        }

        // Annular end caps at both ends (θ × r grid)
        foreach (var zEnd in (double[])[-HalfLength, HalfLength])
        {
            AppendGrid(thetaDivisions, radialDivisions, (u, v) =>
            {
                var theta = 2.0 * Math.PI * u;
                var r = InnerRadius + (OuterRadius - InnerRadius) * v;
                return (r * Math.Cos(theta), r * Math.Sin(theta), zEnd);
            });
        }

        // Radial displacement vector u(r)·(cosθ, sinθ, 0) at every node
        var nodeCount = positions.Count / 3;
        var vectors = new double[nodeCount * 3];
        for (var node = 0; node < nodeCount; node++)
        {
            var x = positions[node * 3];
            var y = positions[node * 3 + 1];
            var r = Math.Sqrt(x * x + y * y);
            var u = RadialDisplacement(r);
            vectors[node * 3] = u * x / r;
            vectors[node * 3 + 1] = u * y / r;
            vectors[node * 3 + 2] = 0.0;
        }

        return new ViewportMesh
        {
            Name = "Thick-Walled Cylinder",
            Positions = [.. positions],
            TriangleIndices = [.. triangles],
            VectorValues = vectors,      // Glyph arrow vector field
            Displacements = vectors,     // Reused for deformation tracking demo
            ShowEdges = false,           // Keep focus on arrows rather than dense wireframe edges
        };
    }

    // ================= UI handlers =================
    // Note: Events may fire during XAML parsing (before field initialization), so guard against null.

    private void ApplyGlyphScale()
    {
        var factor = ScaleSlider.Value;
        Viewport.GlyphScale = _suggestedGlyphScale * factor;
        ScaleLabel.Text = string.Create(CultureInfo.InvariantCulture, $"×{factor:0.0}");
    }

    private void OnGlyphToggleChanged(object sender, RoutedEventArgs e)
    {
        if (Viewport is not null)
        {
            Viewport.ShowGlyphs = GlyphToggle.IsChecked == true;
        }
    }

    private void OnScaleChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (Viewport is not null)
        {
            ApplyGlyphScale();
        }
    }

    private void OnStrideChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Viewport is not null
            && StrideCombo.SelectedItem is ComboBoxItem { Content: string text }
            && int.TryParse(text, out var stride))
        {
            Viewport.GlyphStride = stride;
        }
    }

    private void OnClipToggleChanged(object sender, RoutedEventArgs e)
    {
        if (Viewport is not null)
        {
            Viewport.SectionPlane = ClipToggle.IsChecked == true ? _plane : null;
        }
    }

    private void OnAnimToggleChanged(object sender, RoutedEventArgs e)
    {
        if (Viewport is null)
        {
            return;
        }

        if (AnimToggle.IsChecked == true)
        {
            Viewport.DeformationScale = Viewport.GetSuggestedDeformationScale();
            Viewport.IsDeformationAnimated = true;
        }
        else
        {
            Viewport.IsDeformationAnimated = false;
            Viewport.DeformationScale = 0.0;
        }
    }

    private void OnFitClick(object sender, RoutedEventArgs e) => Viewport.FitToView();
}
