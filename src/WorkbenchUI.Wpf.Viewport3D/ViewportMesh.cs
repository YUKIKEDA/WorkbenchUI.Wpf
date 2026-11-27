using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Model representing a single triangle mesh part displayed in <see cref="WorkbenchViewport"/>.
/// <para>
/// Represents render-level geometry (triangles and nodal scalars); extracting boundary surfaces
/// and triangulating solver elements is the application's responsibility. Coordinates are supplied in
/// double precision and recentered around the scene origin before converting to float for GPU upload
/// to prevent precision jitter on large-coordinate models.
/// </para>
/// <para>
/// Updates follow the pattern of replacing array instances rather than mutating contents in place,
/// avoiding data races during rendering.
/// </para>
/// </summary>
public sealed class ViewportMesh : INotifyPropertyChanged
{
    private string? _name;
    private double[] _positions = [];
    private int[] _triangleIndices = [];
    private double[]? _scalarValues;
    private double[]? _displacements;
    private double[]? _vectorValues;
    private Color _color = Color.FromRgb(0xB0, 0xB4, 0xBC);
    private bool _isVisible = true;
    private bool _showEdges = true;
    private bool _isClippable = true;
    private double _opacity = 1.0;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Part name for legends and debugging.</summary>
    public string? Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    /// <summary>Node coordinates as a flat array x0, y0, z0, x1, y1, z1, ... (length is a multiple of 3).</summary>
    public double[] Positions
    {
        get => _positions;
        set => SetField(ref _positions, value ?? []);
    }

    /// <summary>Triangle vertex indices (3 indices per triangle).</summary>
    public int[] TriangleIndices
    {
        get => _triangleIndices;
        set => SetField(ref _triangleIndices, value ?? []);
    }

    /// <summary>
    /// Nodal scalar values for contour rendering (length equals node count).
    /// When null, the part is shaded with its uniform <see cref="Color"/>.
    /// Replacing this array performs a partial vertex-scalar upload without rebuilding geometry,
    /// allowing efficient frame-by-frame playback (e.g., filling or solidification contours).
    /// </summary>
    public double[]? ScalarValues
    {
        get => _scalarValues;
        set => SetField(ref _scalarValues, value);
    }

    /// <summary>Synchronizes scalar values without raising notifications (used by derived voxel-surface meshes).</summary>
    internal void SyncScalarValuesWithoutNotify(double[]? values) => _scalarValues = values;

    /// <summary>
    /// Nodal displacement vectors (ux0, uy0, uz0, ux1, ..., length 3 * node count).
    /// Null or insufficient length is treated as zero displacement. Displayed deformation equals
    /// displacement multiplied by <see cref="WorkbenchViewport.DeformationScale"/> (applied on the GPU).
    /// Replacing this array performs a partial displacement buffer update without rebuilding geometry,
    /// enabling smooth transient playback.
    /// </summary>
    public double[]? Displacements
    {
        get => _displacements;
        set => SetField(ref _displacements, value);
    }

    /// <summary>
    /// Nodal vector field values for glyph rendering (vx0, vy0, vz0, vx1, ..., length 3 * node count).
    /// When null, no glyphs are drawn for this part. Arrow length is |v| * <see cref="WorkbenchViewport.GlyphScale"/>,
    /// and color maps |v| through <see cref="WorkbenchViewport.GlyphColorScale"/>.
    /// Switch fields (displacement, reaction force, principal stress, etc.) by replacing the array instance.
    /// </summary>
    public double[]? VectorValues
    {
        get => _vectorValues;
        set => SetField(ref _vectorValues, value);
    }

    /// <summary>Solid part color used when contours are disabled or <see cref="ScalarValues"/> is null.</summary>
    public Color Color
    {
        get => _color;
        set => SetField(ref _color, value);
    }

    /// <summary>Gets or sets whether this mesh is visible in the viewport.</summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => SetField(ref _isVisible, value);
    }

    /// <summary>Gets or sets whether mesh wireframe edges are overlaid on this part.</summary>
    public bool ShowEdges
    {
        get => _showEdges;
        set => SetField(ref _showEdges, value);
    }

    /// <summary>
    /// Gets or sets whether this part is clipped by <see cref="WorkbenchViewport.SectionPlane"/>.
    /// Set to false to keep reference fixtures, section-slice overlays, or auxiliary parts unclipped. Default is true.
    /// </summary>
    public bool IsClippable
    {
        get => _isClippable;
        set => SetField(ref _isClippable, value);
    }

    /// <summary>Opacity in the range [0, 1]. Values below 1 render with simple unsorted alpha blending.</summary>
    public double Opacity
    {
        get => _opacity;
        set => SetField(ref _opacity, Math.Clamp(value, 0.0, 1.0));
    }

    /// <summary>Number of nodes in <see cref="Positions"/>.</summary>
    public int VertexCount => _positions.Length / 3;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
