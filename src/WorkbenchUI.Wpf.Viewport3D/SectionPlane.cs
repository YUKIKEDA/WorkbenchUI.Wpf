using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Clipping plane for section cuts, defined by an origin point and a normal vector.
/// The **positive half-space (normal side) remains visible**. Coordinates are specified in
/// pre-recentering model space (the same space as <see cref="ViewportMesh.Positions"/>).
/// <para>
/// Property changes raise notifications so the viewport updates automatically even while
/// assigned to <see cref="WorkbenchViewport.SectionPlane"/> (e.g., when dragging an offset slider).
/// Negate the normal vector to flip which side is kept.
/// </para>
/// </summary>
public sealed class SectionPlane : INotifyPropertyChanged
{
    private double _originX;
    private double _originY;
    private double _originZ;
    private double _normalX;
    private double _normalY;
    private double _normalZ = 1.0;

    /// <summary>Initializes a new <see cref="SectionPlane"/> at the origin with normal (0, 0, 1).</summary>
    public SectionPlane()
    {
    }

    /// <summary>Initializes a new <see cref="SectionPlane"/> with the specified origin point and normal vector.</summary>
    public SectionPlane(
        double originX, double originY, double originZ,
        double normalX, double normalY, double normalZ)
    {
        _originX = originX;
        _originY = originY;
        _originZ = originZ;
        _normalX = normalX;
        _normalY = normalY;
        _normalZ = normalZ;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>X coordinate of a point on the plane (model space).</summary>
    public double OriginX
    {
        get => _originX;
        set => SetField(ref _originX, value);
    }

    /// <summary>Y coordinate of a point on the plane (model space).</summary>
    public double OriginY
    {
        get => _originY;
        set => SetField(ref _originY, value);
    }

    /// <summary>Z coordinate of a point on the plane (model space).</summary>
    public double OriginZ
    {
        get => _originZ;
        set => SetField(ref _originZ, value);
    }

    /// <summary>X component of the plane normal (normalization not required; this side remains visible). Default is +Z.</summary>
    public double NormalX
    {
        get => _normalX;
        set => SetField(ref _normalX, value);
    }

    /// <summary>Y component of the plane normal.</summary>
    public double NormalY
    {
        get => _normalY;
        set => SetField(ref _normalY, value);
    }

    /// <summary>Z component of the plane normal.</summary>
    public double NormalZ
    {
        get => _normalZ;
        set => SetField(ref _normalZ, value);
    }

    private void SetField(ref double field, double value, [CallerMemberName] string? propertyName = null)
    {
        if (field != value)
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
