using System.Numerics;

namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Turntable camera using pure GPU-independent math for straightforward unit testing.
/// <para>
/// State consists of Target, Yaw, Pitch, and Distance, allowing only turntable orbit
/// around <see cref="UpAxis"/> (no free trackball roll).
/// Orthographic view height is derived from distance so switching projections preserves apparent model size.
/// </para>
/// </summary>
public sealed class ViewportCamera
{
    /// <summary>Maximum pitch angle (avoids gimbal singularity straight up or straight down).</summary>
    private const double MaxPitch = 89.5 * Math.PI / 180.0;

    private Vector3 _target;
    private double _yaw = 45.0 * Math.PI / 180.0;
    private double _pitch = 30.0 * Math.PI / 180.0;
    private double _distance = 10.0;
    private double _fieldOfView = 45.0 * Math.PI / 180.0;
    private double _sceneRadius = 5.0;
    private ViewportProjection _projection = ViewportProjection.Perspective;
    private ViewportUpAxis _upAxis = ViewportUpAxis.ZUp;

    /// <summary>Raised whenever the camera state changes (used by the viewport to trigger redraws).</summary>
    public event EventHandler? Changed;

    /// <summary>Look-at target in model-local (recentered) coordinates.</summary>
    public Vector3 Target
    {
        get => _target;
        set => SetField(ref _target, value);
    }

    /// <summary>Azimuth angle in radians around the up-axis.</summary>
    public double Yaw
    {
        get => _yaw;
        set => SetField(ref _yaw, value);
    }

    /// <summary>Elevation angle in radians, clamped to +/-89.5 degrees.</summary>
    public double Pitch
    {
        get => _pitch;
        set => SetField(ref _pitch, Math.Clamp(value, -MaxPitch, MaxPitch));
    }

    /// <summary>Distance from the target to the camera, clamped to a positive value.</summary>
    public double Distance
    {
        get => _distance;
        set => SetField(ref _distance, Math.Max(value, 1e-9));
    }

    /// <summary>Vertical field of view in radians (default 45 degrees). Also used to derive orthographic height.</summary>
    public double FieldOfView
    {
        get => _fieldOfView;
        set => SetField(ref _fieldOfView, Math.Clamp(value, 0.01, Math.PI - 0.01));
    }

    /// <summary>Representative scene radius used to compute near/far clip planes (updated by FitToBounds).</summary>
    public double SceneRadius
    {
        get => _sceneRadius;
        set => SetField(ref _sceneRadius, Math.Max(value, 1e-9));
    }

    /// <summary>Camera projection mode (perspective or orthographic).</summary>
    public ViewportProjection Projection
    {
        get => _projection;
        set => SetField(ref _projection, value);
    }

    /// <summary>Upward axis for turntable rotation (Z-up or Y-up).</summary>
    public ViewportUpAxis UpAxis
    {
        get => _upAxis;
        set => SetField(ref _upAxis, value);
    }

    /// <summary>Unit vector along the configured up-axis.</summary>
    public Vector3 UpDirection =>
        _upAxis == ViewportUpAxis.ZUp ? Vector3.UnitZ : Vector3.UnitY;

    /// <summary>
    /// Effective up vector passed to LookAt. Falls back to a horizontal axis when looking straight
    /// up or down (eye direction parallel to up-axis) to avoid singularities in Top/Bottom views.
    /// </summary>
    private Vector3 GetEffectiveUp(Vector3 eyeDirection)
    {
        var up = UpDirection;
        if (Math.Abs(Vector3.Dot(eyeDirection, up)) < 0.99999f)
        {
            return up;
        }

        // Under Z-up, Top view puts +Y up on screen; under Y-up, Top view puts -Z up on screen (CAD convention)
        return _upAxis == ViewportUpAxis.ZUp ? Vector3.UnitY : -Vector3.UnitZ;
    }

    /// <summary>Unit vector pointing from the target toward the camera eye.</summary>
    public Vector3 GetEyeDirection()
    {
        var cosP = Math.Cos(_pitch);
        var sinP = Math.Sin(_pitch);
        var cosY = Math.Cos(_yaw);
        var sinY = Math.Sin(_yaw);

        return _upAxis == ViewportUpAxis.ZUp
            ? new Vector3((float)(cosP * cosY), (float)(cosP * sinY), (float)sinP)
            : new Vector3((float)(cosP * sinY), (float)sinP, (float)(cosP * cosY));
    }

    /// <summary>World-space position of the camera eye.</summary>
    public Vector3 GetEyePosition() => _target + GetEyeDirection() * (float)_distance;

    /// <summary>Right-handed view matrix.</summary>
    public Matrix4x4 GetViewMatrix()
    {
        var eyeDir = GetEyeDirection();
        return Matrix4x4.CreateLookAt(_target + eyeDir * (float)_distance, _target, GetEffectiveUp(eyeDir));
    }

    /// <summary>
    /// Right-handed projection matrix with near/far planes derived from distance and scene radius.
    /// Orthographic height matches the perspective view height at the target depth.
    /// </summary>
    public Matrix4x4 GetProjectionMatrix(double aspectRatio)
    {
        var aspect = Math.Max(aspectRatio, 1e-6);
        var far = _distance + _sceneRadius * 3.0;
        var near = Math.Max(_sceneRadius * 1e-3, _distance - _sceneRadius * 3.0);

        if (_projection == ViewportProjection.Perspective)
        {
            return Matrix4x4.CreatePerspectiveFieldOfView(
                (float)_fieldOfView, (float)aspect, (float)near, (float)far);
        }

        var height = GetViewHeightAtTarget();
        return Matrix4x4.CreateOrthographic(
            (float)(height * aspect), (float)height, (float)near, (float)far);
    }

    /// <summary>World-space view height at the target depth (common basis for pan, zoom, and orthographic sizing).</summary>
    public double GetViewHeightAtTarget() =>
        2.0 * _distance * Math.Tan(_fieldOfView / 2.0);

    /// <summary>World-space length corresponding to one viewport pixel at the target depth.</summary>
    public double GetWorldPerPixel(double viewportHeightPixels) =>
        GetViewHeightAtTarget() / Math.Max(viewportHeightPixels, 1.0);

    /// <summary>Camera right and up unit basis vectors (used for pan and cursor-centered zoom).</summary>
    public (Vector3 Right, Vector3 Up) GetViewBasis()
    {
        var eyeDir = GetEyeDirection();
        var forward = -eyeDir; // View direction toward the target
        var right = Vector3.Normalize(Vector3.Cross(forward, GetEffectiveUp(eyeDir)));
        var up = Vector3.Normalize(Vector3.Cross(right, forward));
        return (right, up);
    }

    /// <summary>Applies turntable rotation by <paramref name="deltaYaw"/> and <paramref name="deltaPitch"/> (in radians).</summary>
    public void Orbit(double deltaYaw, double deltaPitch)
    {
        _yaw = NormalizeAngle(_yaw + deltaYaw);
        _pitch = Math.Clamp(_pitch + deltaPitch, -MaxPitch, MaxPitch);
        RaiseChanged();
    }

    /// <summary>
    /// Pans the target based on pixel mouse deltas so the model follows the cursor drag direction.
    /// </summary>
    public void Pan(double deltaXPixels, double deltaYPixels, double viewportHeightPixels)
    {
        var wpp = GetWorldPerPixel(viewportHeightPixels);
        var (right, up) = GetViewBasis();
        _target -= right * (float)(deltaXPixels * wpp);
        _target += up * (float)(deltaYPixels * wpp); // Screen Y points downward
        RaiseChanged();
    }

    /// <summary>Scales camera distance by <paramref name="factor"/> (values &lt; 1 zoom in).</summary>
    public void Zoom(double factor)
    {
        _distance = Math.Max(_distance * factor, 1e-9);
        RaiseChanged();
    }

    /// <summary>
    /// Cursor-anchored zoom. <paramref name="cursorXPixels"/> and <paramref name="cursorYPixels"/> are relative to the
    /// top-left corner of the viewport; shifts the target so the point under the cursor at target depth remains fixed.
    /// </summary>
    public void ZoomAt(
        double factor,
        double cursorXPixels, double cursorYPixels,
        double viewportWidthPixels, double viewportHeightPixels)
    {
        var wpp = GetWorldPerPixel(viewportHeightPixels);
        var dx = cursorXPixels - viewportWidthPixels / 2.0;
        var dy = cursorYPixels - viewportHeightPixels / 2.0;

        var (right, up) = GetViewBasis();
        var cursorOffset = right * (float)(dx * wpp) - up * (float)(dy * wpp);

        _target += cursorOffset * (float)(1.0 - factor);
        _distance = Math.Max(_distance * factor, 1e-9);
        RaiseChanged();
    }

    /// <summary>
    /// Sets yaw and pitch directly (used for standard views and view animations).
    /// Unlike <see cref="Pitch"/>, permits exact +/-90 degree elevation (the view matrix handles the up-vector singularity).
    /// </summary>
    public void SetOrientation(double yaw, double pitch)
    {
        _yaw = NormalizeAngle(yaw);
        _pitch = Math.Clamp(pitch, -Math.PI / 2.0, Math.PI / 2.0);
        RaiseChanged();
    }

    /// <summary>Snaps immediately to a standard view (smooth interpolation is handled by <see cref="WorkbenchViewport.SetStandardView"/>).</summary>
    public void SetStandardView(ViewportStandardView view)
    {
        var (yaw, pitch) = GetStandardViewAngles(view, _upAxis);
        SetOrientation(yaw, pitch);
    }

    /// <summary>
    /// Returns (Yaw, Pitch) in radians for a standard viewpoint.
    /// Z-up: Front looks from -Y, Right from +X (CAD convention).
    /// Y-up: Front looks from +Z, Right from +X (DCC convention).
    /// Isometric looks along (1, +/-1, 1) with a positive up-axis component.
    /// </summary>
    public static (double Yaw, double Pitch) GetStandardViewAngles(
        ViewportStandardView view, ViewportUpAxis upAxis)
    {
        var halfPi = Math.PI / 2.0;
        var isoPitch = Math.Asin(1.0 / Math.Sqrt(3.0)); // ~= 35.264 deg

        // Yaw convention: Z-up uses eyeDir=(cosP*cosY, cosP*sinY, sinP);
        //                 Y-up uses eyeDir=(cosP*sinY, sinP, cosP*cosY)
        return (view, upAxis) switch
        {
            (ViewportStandardView.Front, ViewportUpAxis.ZUp) => (-halfPi, 0.0),
            (ViewportStandardView.Back, ViewportUpAxis.ZUp) => (halfPi, 0.0),
            (ViewportStandardView.Right, ViewportUpAxis.ZUp) => (0.0, 0.0),
            (ViewportStandardView.Left, ViewportUpAxis.ZUp) => (Math.PI, 0.0),
            (ViewportStandardView.Top, ViewportUpAxis.ZUp) => (-halfPi, halfPi),
            (ViewportStandardView.Bottom, ViewportUpAxis.ZUp) => (-halfPi, -halfPi),
            (ViewportStandardView.Isometric, ViewportUpAxis.ZUp) => (-Math.PI / 4.0, isoPitch),

            (ViewportStandardView.Front, ViewportUpAxis.YUp) => (0.0, 0.0),
            (ViewportStandardView.Back, ViewportUpAxis.YUp) => (Math.PI, 0.0),
            (ViewportStandardView.Right, ViewportUpAxis.YUp) => (halfPi, 0.0),
            (ViewportStandardView.Left, ViewportUpAxis.YUp) => (-halfPi, 0.0),
            (ViewportStandardView.Top, ViewportUpAxis.YUp) => (0.0, halfPi),
            (ViewportStandardView.Bottom, ViewportUpAxis.YUp) => (0.0, -halfPi),
            (ViewportStandardView.Isometric, ViewportUpAxis.YUp) => (Math.PI / 4.0, isoPitch),

            _ => (0.0, 0.0),
        };
    }

    /// <summary>Fits the view to enclose the bounding sphere of <paramref name="bounds"/>.</summary>
    public void FitToBounds(Bounds3D bounds, double margin = 1.1)
    {
        if (bounds.IsEmpty)
        {
            return;
        }

        var radius = Math.Max(bounds.Radius, 1e-9);
        _target = new Vector3((float)bounds.CenterX, (float)bounds.CenterY, (float)bounds.CenterZ);
        _sceneRadius = radius;
        _distance = radius * margin / Math.Sin(Math.Min(_fieldOfView, Math.PI / 2.0) / 2.0);
        RaiseChanged();
    }

    private static double NormalizeAngle(double angle)
    {
        var twoPi = 2.0 * Math.PI;
        angle %= twoPi;
        return angle < 0 ? angle + twoPi : angle;
    }

    private void SetField<T>(ref T field, T value)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            RaiseChanged();
        }
    }

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
