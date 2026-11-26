using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Model representing a single sparse voxel grid displayed in <see cref="WorkbenchViewport"/>.
/// <para>
/// Exposes the logical grid domain (origin, cell size, resolution) together with linear active-cell indices
/// and parallel per-cell attributes. Brick LOD coarsening is managed internally without leaking into the public API.
/// </para>
/// <para>
/// Like <see cref="ViewportMesh"/>, array updates are signaled by replacing array instances rather than mutating in place.
/// </para>
/// </summary>
public sealed class ViewportVoxelGrid : INotifyPropertyChanged
{
    private string? _name;
    private double _originX;
    private double _originY;
    private double _originZ;
    private double _cellSizeX = 1.0;
    private double _cellSizeY = 1.0;
    private double _cellSizeZ = 1.0;
    private int _resolutionX = 1;
    private int _resolutionY = 1;
    private int _resolutionZ = 1;
    private int[] _activeCellIndices = [];
    private int[]? _regionIds;
    private double[]? _scalarValues;
    private double[]? _vectorValues;
    private Color _color = Color.FromRgb(0xB0, 0xB4, 0xBC);
    private bool _isVisible = true;
    private bool _isClippable = true;
    private bool _showEdges;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Display name for legends and debugging.</summary>
    public string? Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    /// <summary>X coordinate of the logical domain minimum corner (outer corner of cell (0, 0, 0)).</summary>
    public double OriginX
    {
        get => _originX;
        set => SetField(ref _originX, value);
    }

    /// <summary>Y coordinate of the logical domain minimum corner.</summary>
    public double OriginY
    {
        get => _originY;
        set => SetField(ref _originY, value);
    }

    /// <summary>Z coordinate of the logical domain minimum corner.</summary>
    public double OriginZ
    {
        get => _originZ;
        set => SetField(ref _originZ, value);
    }

    /// <summary>Cell size along X in model units.</summary>
    public double CellSizeX
    {
        get => _cellSizeX;
        set => SetField(ref _cellSizeX, value);
    }

    /// <summary>Cell size along Y in model units.</summary>
    public double CellSizeY
    {
        get => _cellSizeY;
        set => SetField(ref _cellSizeY, value);
    }

    /// <summary>Cell size along Z in model units.</summary>
    public double CellSizeZ
    {
        get => _cellSizeZ;
        set => SetField(ref _cellSizeZ, value);
    }

    /// <summary>Logical grid resolution along X (supports large sparse domains up to ~1000^3).</summary>
    public int ResolutionX
    {
        get => _resolutionX;
        set => SetField(ref _resolutionX, value);
    }

    /// <summary>Logical grid resolution along Y.</summary>
    public int ResolutionY
    {
        get => _resolutionY;
        set => SetField(ref _resolutionY, value);
    }

    /// <summary>Logical grid resolution along Z.</summary>
    public int ResolutionZ
    {
        get => _resolutionZ;
        set => SetField(ref _resolutionZ, value);
    }

    /// <summary>
    /// Linear indices of active cells (x + y * Nx + z * Nx * Ny). Does not need to be sorted, though duplicates should be avoided.
    /// </summary>
    public int[] ActiveCellIndices
    {
        get => _activeCellIndices;
        set => SetField(ref _activeCellIndices, value ?? []);
    }

    /// <summary>
    /// Region ID per active cell (matching <see cref="ActiveCellIndices"/> in length).
    /// Treated as region 0 for all cells when null.
    /// </summary>
    public int[]? RegionIds
    {
        get => _regionIds;
        set => SetField(ref _regionIds, value);
    }

    /// <summary>
    /// Scalar contour values per active cell (matching <see cref="ActiveCellIndices"/> in length).
    /// When null, cells render in solid <see cref="Color"/>. Replacing this array updates vertex scalars
    /// in place without re-extracting outer faces, enabling efficient transient playback.
    /// </summary>
    public double[]? ScalarValues
    {
        get => _scalarValues;
        set => SetField(ref _scalarValues, value);
    }

    /// <summary>Synchronizes scalar values without raising notifications (used by internal brick LOD grids).</summary>
    internal void SyncScalarValuesWithoutNotify(double[]? values) => _scalarValues = values;

    /// <summary>
    /// Vector field at active cell centers (vx, vy, vz, ..., length 3 * active count) for glyph rendering.
    /// When null, no glyphs are drawn.
    /// </summary>
    public double[]? VectorValues
    {
        get => _vectorValues;
        set => SetField(ref _vectorValues, value);
    }

    /// <summary>Solid color used when contours are disabled or <see cref="ScalarValues"/> is null.</summary>
    public Color Color
    {
        get => _color;
        set => SetField(ref _color, value);
    }

    /// <summary>Gets or sets whether this voxel grid is visible in the viewport.</summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => SetField(ref _isVisible, value);
    }

    /// <summary>Gets or sets whether this voxel grid is clipped by <see cref="WorkbenchViewport.SectionPlane"/>.</summary>
    public bool IsClippable
    {
        get => _isClippable;
        set => SetField(ref _isClippable, value);
    }

    /// <summary>Gets or sets whether outer wireframe edges are overlaid (defaults to false due to high face counts).</summary>
    public bool ShowEdges
    {
        get => _showEdges;
        set => SetField(ref _showEdges, value);
    }

    /// <summary>Number of active cells.</summary>
    public int ActiveCount => _activeCellIndices.Length;

    /// <summary>Decodes a linear cell index into (i, j, k); returns false if out of range.</summary>
    public bool TryDecodeIndex(int linear, out int i, out int j, out int k)
        => ViewportVoxelIndexing.TryDecode(linear, _resolutionX, _resolutionY, _resolutionZ, out i, out j, out k);

    /// <summary>Encodes (i, j, k) cell coordinates into a linear index.</summary>
    public int EncodeIndex(int i, int j, int k)
        => ViewportVoxelIndexing.Encode(i, j, k, _resolutionX, _resolutionY);

    /// <summary>Gets the minimum-corner coordinates of cell (i, j, k).</summary>
    public void GetCellMinCorner(int i, int j, int k, out double x, out double y, out double z)
    {
        x = _originX + i * _cellSizeX;
        y = _originY + j * _cellSizeY;
        z = _originZ + k * _cellSizeZ;
    }

    /// <summary>Gets the center coordinates of cell (i, j, k).</summary>
    public void GetCellCenter(int i, int j, int k, out double x, out double y, out double z)
    {
        GetCellMinCorner(i, j, k, out x, out y, out z);
        x += 0.5 * _cellSizeX;
        y += 0.5 * _cellSizeY;
        z += 0.5 * _cellSizeZ;
    }

    /// <summary>Computes the axis-aligned bounding box enclosing all active cells (or <see cref="Bounds3D.Empty"/> if empty).</summary>
    public Bounds3D ComputeBounds()
        => ViewportVoxelIndexing.ComputeActiveBounds(
            _activeCellIndices, _resolutionX, _resolutionY, _resolutionZ,
            _originX, _originY, _originZ, _cellSizeX, _cellSizeY, _cellSizeZ);

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
