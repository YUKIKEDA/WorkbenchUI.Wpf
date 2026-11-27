using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Clickable ViewCube overlay, using the same WPF overlay approach as the axis triad
/// to render a pseudo-3D cube synchronized with the camera orientation in the top-right corner.
/// Clicking a face snaps to a standard view; clicking a corner jumps to an octant view
/// (smooth interpolation is handled by <see cref="WorkbenchViewport"/>).
/// Drag-orbiting on the cube itself is omitted since it duplicates middle-button orbit.
/// </summary>
internal sealed class ViewCubeOverlay
{
    private const double HalfSize = 26.0;   // Cube half-size (px)
    private const double Margin = 58.0;     // Center offset from top-right of canvas
    private const double CornerRadius = 5.0;

    /// <summary>World-space face normals in the order (+X, -X, +Y, -Y, +Z, -Z).</summary>
    private static readonly Vector3[] FaceNormals =
    [
        Vector3.UnitX, -Vector3.UnitX,
        Vector3.UnitY, -Vector3.UnitY,
        Vector3.UnitZ, -Vector3.UnitZ,
    ];

    private static readonly Vector3[] CornerDirections =
    [
        new(1, 1, 1), new(1, 1, -1), new(1, -1, 1), new(1, -1, -1),
        new(-1, 1, 1), new(-1, 1, -1), new(-1, -1, 1), new(-1, -1, -1),
    ];

    private readonly Canvas _canvas;
    private readonly Polygon[] _facePolygons = new Polygon[6];
    private readonly TextBlock[] _faceLabels = new TextBlock[6];
    private readonly Ellipse[] _cornerDots = new Ellipse[8];

    private ViewportUpAxis _upAxis = ViewportUpAxis.ZUp;

    public ViewCubeOverlay(Canvas canvas)
    {
        _canvas = canvas;

        for (var i = 0; i < 6; i++)
        {
            var faceIndex = i;
            var polygon = new Polygon
            {
                StrokeThickness = 1.0,
                Cursor = Cursors.Hand,
            };
            polygon.SetResourceReference(Shape.FillProperty, "Wb.Brush.Surface.Elevated");
            polygon.SetResourceReference(Shape.StrokeProperty, "Wb.Brush.Border.Strong");
            polygon.MouseEnter += (_, _) =>
                polygon.SetResourceReference(Shape.FillProperty, "Wb.Brush.Accent.Muted");
            polygon.MouseLeave += (_, _) =>
                polygon.SetResourceReference(Shape.FillProperty, "Wb.Brush.Surface.Elevated");
            polygon.MouseLeftButtonDown += (_, e) =>
            {
                OnFaceClicked(faceIndex);
                e.Handled = true;
            };

            var label = new TextBlock
            {
                FontSize = 8.0,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                Width = 60.0,
                IsHitTestVisible = false,
            };
            label.SetResourceReference(TextBlock.ForegroundProperty, "Wb.Brush.Text.Primary");

            _facePolygons[i] = polygon;
            _faceLabels[i] = label;
            _canvas.Children.Add(polygon);
        }

        // Place labels above all polygons so they are never occluded across face boundaries
        foreach (var label in _faceLabels)
        {
            _canvas.Children.Add(label);
        }

        for (var i = 0; i < 8; i++)
        {
            var cornerIndex = i;
            var dot = new Ellipse
            {
                Width = CornerRadius * 2.0,
                Height = CornerRadius * 2.0,
                StrokeThickness = 1.0,
                Cursor = Cursors.Hand,
            };
            dot.SetResourceReference(Shape.FillProperty, "Wb.Brush.Control.Background");
            dot.SetResourceReference(Shape.StrokeProperty, "Wb.Brush.Border.Strong");
            dot.MouseEnter += (_, _) =>
                dot.SetResourceReference(Shape.FillProperty, "Wb.Brush.Accent.Default");
            dot.MouseLeave += (_, _) =>
                dot.SetResourceReference(Shape.FillProperty, "Wb.Brush.Control.Background");
            dot.MouseLeftButtonDown += (_, e) =>
            {
                OnCornerClicked(cornerIndex);
                e.Handled = true;
            };

            _cornerDots[i] = dot;
            _canvas.Children.Add(dot);
        }
    }

    /// <summary>Raised when a view orientation change is requested (Yaw, Pitch in radians). Interpolation is handled by the subscriber.</summary>
    public event Action<double, double>? OrientationRequested;

    /// <summary>Updates the ViewCube projection to match the current camera orientation.</summary>
    public void Update(ViewportCamera camera, double canvasWidth, double canvasHeight, bool visible)
    {
        visible &= canvasWidth > Margin * 2.0 && canvasHeight > Margin * 2.0;
        if (!visible)
        {
            SetAllVisibility(Visibility.Collapsed);
            return;
        }

        _upAxis = camera.UpAxis;
        var eyeDir = camera.GetEyeDirection();
        var (right, up) = camera.GetViewBasis();
        var centerX = canvasWidth - Margin;
        var centerY = Margin;

        Vector2 Project(Vector3 p) => new(
            (float)(centerX + Vector3.Dot(p, right) * HalfSize),
            (float)(centerY - Vector3.Dot(p, up) * HalfSize));

        for (var i = 0; i < 6; i++)
        {
            var normal = FaceNormals[i];
            var facing = Vector3.Dot(normal, eyeDir);
            var polygon = _facePolygons[i];
            var label = _faceLabels[i];

            if (facing < 0.05f)
            {
                polygon.Visibility = Visibility.Collapsed;
                label.Visibility = Visibility.Collapsed;
                continue;
            }

            var (u, v) = GetFaceTangents(normal);
            var points = new PointCollection(4);
            foreach (var (su, sv) in ((float, float)[])[(-1, -1), (1, -1), (1, 1), (-1, 1)])
            {
                var corner = Project(normal + u * su + v * sv);
                points.Add(new Point(corner.X, corner.Y));
            }

            polygon.Points = points;
            polygon.Opacity = 0.55 + 0.40 * facing;
            polygon.Visibility = Visibility.Visible;

            var center = Project(normal);
            label.Text = GetFaceLabel(i, _upAxis);
            label.Opacity = Math.Clamp((facing - 0.05) * 2.5, 0.0, 1.0);
            label.Visibility = Visibility.Visible;
            Canvas.SetLeft(label, center.X - label.Width / 2.0);
            Canvas.SetTop(label, center.Y - 6.0);
        }

        for (var i = 0; i < 8; i++)
        {
            var direction = Vector3.Normalize(CornerDirections[i]);
            var dot = _cornerDots[i];
            if (Vector3.Dot(direction, eyeDir) < 0.15f)
            {
                dot.Visibility = Visibility.Collapsed;
                continue;
            }

            var position = Project(CornerDirections[i]);
            Canvas.SetLeft(dot, position.X - CornerRadius);
            Canvas.SetTop(dot, position.Y - CornerRadius);
            dot.Visibility = Visibility.Visible;
        }
    }

    private void OnFaceClicked(int faceIndex)
    {
        var view = GetFaceView(faceIndex, _upAxis);
        var (yaw, pitch) = ViewportCamera.GetStandardViewAngles(view, _upAxis);
        OrientationRequested?.Invoke(yaw, pitch);
    }

    private void OnCornerClicked(int cornerIndex)
    {
        var direction = Vector3.Normalize(CornerDirections[cornerIndex]);
        double yaw, pitch;
        if (_upAxis == ViewportUpAxis.ZUp)
        {
            pitch = Math.Asin(direction.Z);
            yaw = Math.Atan2(direction.Y, direction.X);
        }
        else
        {
            pitch = Math.Asin(direction.Y);
            yaw = Math.Atan2(direction.X, direction.Z);
        }

        OrientationRequested?.Invoke(yaw, pitch);
    }

    /// <summary>Maps a face index (+X, -X, +Y, -Y, +Z, -Z) to its standard view; FRONT depends on the up-axis.</summary>
    internal static ViewportStandardView GetFaceView(int faceIndex, ViewportUpAxis upAxis) =>
        (faceIndex, upAxis) switch
        {
            (0, _) => ViewportStandardView.Right,
            (1, _) => ViewportStandardView.Left,
            (2, ViewportUpAxis.ZUp) => ViewportStandardView.Back,
            (3, ViewportUpAxis.ZUp) => ViewportStandardView.Front,
            (4, ViewportUpAxis.ZUp) => ViewportStandardView.Top,
            (5, ViewportUpAxis.ZUp) => ViewportStandardView.Bottom,
            (2, _) => ViewportStandardView.Top,
            (3, _) => ViewportStandardView.Bottom,
            (4, _) => ViewportStandardView.Front,
            (5, _) => ViewportStandardView.Back,
            _ => ViewportStandardView.Isometric,
        };

    private static string GetFaceLabel(int faceIndex, ViewportUpAxis upAxis) =>
        GetFaceView(faceIndex, upAxis) switch
        {
            ViewportStandardView.Front => "FRONT",
            ViewportStandardView.Back => "BACK",
            ViewportStandardView.Left => "LEFT",
            ViewportStandardView.Right => "RIGHT",
            ViewportStandardView.Top => "TOP",
            ViewportStandardView.Bottom => "BOTTOM",
            _ => string.Empty,
        };

    private static (Vector3 U, Vector3 V) GetFaceTangents(Vector3 normal)
    {
        var reference = Math.Abs(normal.Z) > 0.9f ? Vector3.UnitY : Vector3.UnitZ;
        var u = Vector3.Normalize(Vector3.Cross(reference, normal));
        var v = Vector3.Cross(normal, u);
        return (u, v);
    }

    private void SetAllVisibility(Visibility visibility)
    {
        foreach (var polygon in _facePolygons)
        {
            polygon.Visibility = visibility;
        }

        foreach (var label in _faceLabels)
        {
            label.Visibility = visibility;
        }

        foreach (var dot in _cornerDots)
        {
            dot.Visibility = visibility;
        }
    }
}
