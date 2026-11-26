using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Viewport3D;

namespace WorkbenchUI.Wpf.Gallery.Pages;

/// <summary>
/// Deformation display and animation demo.
/// Displays Euler-Bernoulli bending eigenmodes of a cantilever plate and demonstrates
/// deformation scaling, harmonic animation, undeformed wireframe overlay, and PlaybackBar transient playback.
/// </summary>
public partial class ViewportDeformationPage : UserControl
{
    private const double BeamLength = 100.0;  // [mm] x in [0, L], clamped at x=0
    private const double BeamWidth = 40.0;    // [mm] y in [-W/2, W/2]
    private const double TipAmplitude = 1.0;  // [mm] tip amplitude of mode shapes
    private const int DivisionsX = 50;
    private const int DivisionsY = 20;
    private const int TransientFrameCount = 90;
    private const double TransientDuration = 3.0; // [s]

    /// <summary>Bending eigenvalues βL of a cantilever beam (roots of cosh·cos = -1).</summary>
    private static readonly double[] BetaL = [1.8751040687, 4.6940911330, 7.8547574382];

    private readonly ObservableCollection<ViewportMesh> _meshes = [];
    private readonly ColorScale _scale;
    private readonly ViewportMesh _plate;
    private readonly double[][] _modeShapes;      // [mode][node] normalized deflection (tip = 1)
    private readonly double[][] _transientFrames; // [frame][3N] displacement arrays

    public ViewportDeformationPage()
    {
        InitializeComponent();

        _scale = new ColorScale
        {
            ColorMap = ColorMap.Jet,
            Minimum = 0.0,
            Maximum = TipAmplitude,
        };

        (_plate, var nodeX) = CreateCantileverPlate();
        _modeShapes = [.. BetaL.Select(bl => ComputeModeShape(bl, nodeX))];
        _transientFrames = ComputeTransientFrames(nodeX);
        _meshes.Add(_plate);

        ApplyMode(0);

        Viewport.ColorScale = _scale;
        Viewport.MeshSource = _meshes;
        Legend.Scale = _scale;
        Playback.FrameCount = TransientFrameCount;

        Loaded += (_, _) => RendererInfo.Text = Viewport.IsSoftwareRendering
            ? "Rendering path: WARP (software) + WriteableBitmap fallback"
            : "Rendering path: Hardware D3D11 + D3DImage (shared surface)";
    }

    /// <summary>Builds the structured grid mesh of the cantilever plate and each node's x coordinate.</summary>
    private static (ViewportMesh Mesh, double[] NodeX) CreateCantileverPlate()
    {
        var vertexCount = (DivisionsX + 1) * (DivisionsY + 1);
        var positions = new double[vertexCount * 3];
        var nodeX = new double[vertexCount];

        for (var i = 0; i <= DivisionsX; i++)
        {
            var x = BeamLength * i / DivisionsX;
            for (var j = 0; j <= DivisionsY; j++)
            {
                var node = i * (DivisionsY + 1) + j;
                positions[node * 3] = x;
                positions[node * 3 + 1] = -BeamWidth / 2.0 + BeamWidth * j / DivisionsY;
                positions[node * 3 + 2] = 0.0;
                nodeX[node] = x;
            }
        }

        var triangles = new List<int>(DivisionsX * DivisionsY * 6);
        for (var i = 0; i < DivisionsX; i++)
        {
            for (var j = 0; j < DivisionsY; j++)
            {
                var i00 = i * (DivisionsY + 1) + j;
                var i01 = i00 + 1;
                var i10 = (i + 1) * (DivisionsY + 1) + j;
                var i11 = i10 + 1;

                triangles.Add(i00);
                triangles.Add(i10);
                triangles.Add(i11);
                triangles.Add(i00);
                triangles.Add(i11);
                triangles.Add(i01);
            }
        }

        var mesh = new ViewportMesh
        {
            Name = "Cantilever Plate",
            Positions = positions,
            TriangleIndices = [.. triangles],
        };
        return (mesh, nodeX);
    }

    /// <summary>
    /// Euler-Bernoulli cantilever mode shape φ(x) = cosh βx − cos βx − σ(sinh βx − sin βx),
    /// normalized so tip deflection equals 1.
    /// </summary>
    private static double[] ComputeModeShape(double betaL, double[] nodeX)
    {
        var beta = betaL / BeamLength;
        var sigma = (Math.Sinh(betaL) - Math.Sin(betaL)) / (Math.Cosh(betaL) + Math.Cos(betaL));

        double Phi(double x) =>
            Math.Cosh(beta * x) - Math.Cos(beta * x)
            - sigma * (Math.Sinh(beta * x) - Math.Sin(beta * x));

        var tip = Phi(BeamLength);
        return [.. nodeX.Select(x => Phi(x) / tip)];
    }

    /// <summary>
    /// Transient response frames: damped free vibration from initial tip displacement.
    /// w(x,t) = Σ aᵢ φᵢ(x) cos(ωᵢt) e^(−ζωᵢt), ωᵢ ∝ (βᵢL)² (1st mode normalized to 1 Hz).
    /// </summary>
    private double[][] ComputeTransientFrames(double[] nodeX)
    {
        double[] amplitudes = [0.7, 0.22, 0.08]; // Modal weights (sum = 1 mm initial tip displacement)
        const double zeta = 0.04;                // Damping ratio
        var omega1 = 2.0 * Math.PI;              // 1st mode = 1 Hz

        var frames = new double[TransientFrameCount][];
        for (var f = 0; f < TransientFrameCount; f++)
        {
            var t = TransientDuration * f / (TransientFrameCount - 1);
            var displacements = new double[nodeX.Length * 3];

            for (var mode = 0; mode < BetaL.Length; mode++)
            {
                var omega = omega1 * (BetaL[mode] * BetaL[mode]) / (BetaL[0] * BetaL[0]);
                var factor = amplitudes[mode] * TipAmplitude
                    * Math.Cos(omega * t) * Math.Exp(-zeta * omega * t);
                var shape = _modeShapes[mode];
                for (var node = 0; node < nodeX.Length; node++)
                {
                    displacements[node * 3 + 2] += factor * shape[node];
                }
            }

            frames[f] = displacements;
        }

        return frames;
    }

    /// <summary>Applies the static mode shape of the selected mode to Displacements and ScalarValues.</summary>
    private void ApplyMode(int modeIndex)
    {
        var shape = _modeShapes[modeIndex];
        var displacements = new double[shape.Length * 3];
        var scalars = new double[shape.Length];
        for (var node = 0; node < shape.Length; node++)
        {
            var w = TipAmplitude * shape[node];
            displacements[node * 3 + 2] = w;
            scalars[node] = Math.Abs(w);
        }

        // Replacing ScalarValues triggers a full geometry rebuild (heavy path),
        // whereas replacing Displacements updates only the displacement buffer (light path).
        _plate.ScalarValues = scalars;
        _plate.Displacements = displacements;
    }

    // Note: Events may fire during XAML parsing (before field initialization), so guard against null.

    private void OnModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_modeShapes is not null && ModeCombo.SelectedIndex >= 0)
        {
            Playback.IsPlaying = false;
            ApplyMode(ModeCombo.SelectedIndex);
        }
    }

    private void OnAutoScaleClick(object sender, RoutedEventArgs e) =>
        ScaleSlider.Value = Math.Min(Viewport.GetSuggestedDeformationScale(), ScaleSlider.Maximum);

    private void OnFitClick(object sender, RoutedEventArgs e) => Viewport.FitToView();

    private void OnPlaybackFrameChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
    {
        if (_transientFrames is null || e.NewValue < 0 || e.NewValue >= _transientFrames.Length)
        {
            return;
        }

        var t = TransientDuration * e.NewValue / (TransientFrameCount - 1);
        Playback.FrameLabel = $"t = {t:0.00} s";

        // Replacing Displacements updates only the dynamic displacement buffer, so it is fast per frame.
        // Contours (ScalarValues) stay static during playback because updating them triggers a geometry rebuild.
        _plate.Displacements = _transientFrames[e.NewValue];
    }
}
