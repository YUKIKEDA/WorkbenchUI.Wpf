using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Viewport3D;

namespace WorkbenchUI.Wpf.Gallery.Pages;

/// <summary>
/// Probe and annotation demo. Displays von Mises stress contours on a Kirsch plate with a hole
/// (exact uniaxial tension solution) so probing the hole edge reveals the stress concentration
/// factor of 3. Includes ProbeLabelFormatter unit formatting and annotation list management.
/// </summary>
public partial class ViewportProbePage : UserControl
{
    private const double PlateHalfWidth = 60.0;   // [mm]
    private const double PlateHalfHeight = 40.0;  // [mm]
    private const double HoleRadius = 10.0;       // [mm]
    private const double NominalStress = 100.0;   // [MPa]

    private readonly ObservableCollection<ViewportMesh> _meshes = [];
    private readonly ColorScale _scale;

    public ViewportProbePage()
    {
        InitializeComponent();

        _scale = new ColorScale
        {
            ColorMap = ColorMap.Jet,
            Minimum = 0.0,
            Maximum = 3.0 * NominalStress, // Stress concentration factor 3 at hole edge (Kirsch)
        };

        _meshes.Add(CreateKirschPlate());

        Viewport.ColorScale = _scale;
        Viewport.MeshSource = _meshes;
        Legend.Scale = _scale;

        // Reference ProbeLabelFormatter with physical units (application responsibility).
        // Invariant formatting so UIA tests can parse values deterministically.
        Viewport.ProbeLabelFormatter = r =>
            FormattableString.Invariant($"σ_vM = {r.ScalarValue:0.0} MPa (N{r.NodeIndex})");

        AnnotationList.ItemsSource = Viewport.Annotations;
        Viewport.Annotations.CollectionChanged += (_, _) =>
            AnnotationSummary.Text = $"Annotations: {Viewport.Annotations.Count}";

        Loaded += (_, _) =>
        {
            // Start in Top view facing the plate for convenient probing
            Viewport.SetStandardView(ViewportStandardView.Top, animate: false);
            RendererInfo.Text = Viewport.IsSoftwareRendering
                ? "Rendering path: WARP (software) + WriteableBitmap fallback"
                : "Rendering path: Hardware D3D11 + D3DImage (shared surface)";
        };
    }

    /// <summary>
    /// Plate with circular hole (structured ring grid radially blended to the rectangular boundary),
    /// triangulated with Kirsch von Mises stress and an illustrative out-of-plane Z displacement for tracking demo.
    /// </summary>
    private static ViewportMesh CreateKirschPlate()
    {
        const int radialDivisions = 24;   // Hole -> outer boundary
        const int angularDivisions = 96;  // Circumferential (closed)

        var vertexCount = (radialDivisions + 1) * angularDivisions;
        var positions = new double[vertexCount * 3];
        var scalars = new double[vertexCount];
        var displacements = new double[vertexCount * 3];

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
                var (x, y) = (radius * cos, radius * sin);

                var node = a * (radialDivisions + 1) + r;
                positions[node * 3] = x;
                positions[node * 3 + 1] = y;
                positions[node * 3 + 2] = 0.0;

                scalars[node] = KirschVonMises(radius, theta);

                // Illustrative out-of-plane deflection for annotation tracking demo (not a physical solution)
                displacements[node * 3 + 2] =
                    Math.Cos(0.5 * Math.PI * x / PlateHalfWidth)
                    * Math.Cos(0.5 * Math.PI * y / PlateHalfHeight);
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
            Name = "Kirsch Plate",
            Positions = positions,
            TriangleIndices = [.. triangles],
            ScalarValues = scalars,
            Displacements = displacements,
        };
    }

    /// <summary>Plane-stress von Mises from Kirsch's exact solution (uniaxial x-tension around a circular hole in an infinite plate).</summary>
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

    // Note: Toggles with IsChecked="True" raise Checked during XAML parsing (before field initialization),
    // so handlers must guard against null fields.

    private void OnProbeToggleChanged(object sender, RoutedEventArgs e)
    {
        if (Viewport is not null)
        {
            Viewport.PickMode = ProbeToggle.IsChecked == true
                ? ViewportPickMode.Probe
                : ViewportPickMode.None;
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
            Viewport.DeformationScale = 0.0; // Return to undeformed state for accurate probing
        }
    }

    private void OnTopViewClick(object sender, RoutedEventArgs e) =>
        Viewport.SetStandardView(ViewportStandardView.Top);

    private void OnFitClick(object sender, RoutedEventArgs e) => Viewport.FitToView();

    private void OnRemoveAnnotationClick(object sender, RoutedEventArgs e)
    {
        if (AnnotationList.SelectedItem is ViewportAnnotation annotation)
        {
            Viewport.Annotations.Remove(annotation);
        }
    }

    private void OnClearAnnotationsClick(object sender, RoutedEventArgs e) =>
        Viewport.Annotations.Clear();
}
