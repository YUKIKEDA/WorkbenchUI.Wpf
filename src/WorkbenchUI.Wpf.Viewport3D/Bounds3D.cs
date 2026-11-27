namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Axis-aligned bounding box in double precision, used as the basis for recentering and Fit-to-View calculations.
/// </summary>
public readonly struct Bounds3D
{
    /// <summary>An empty bounding box with inverted infinite extents.</summary>
    public static Bounds3D Empty { get; } = new(
        double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity,
        double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

    /// <summary>Initializes a new <see cref="Bounds3D"/> with the specified minimum and maximum coordinates.</summary>
    public Bounds3D(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
    {
        MinX = minX;
        MinY = minY;
        MinZ = minZ;
        MaxX = maxX;
        MaxY = maxY;
        MaxZ = maxZ;
    }

    /// <summary>Minimum X coordinate.</summary>
    public double MinX { get; }

    /// <summary>Minimum Y coordinate.</summary>
    public double MinY { get; }

    /// <summary>Minimum Z coordinate.</summary>
    public double MinZ { get; }

    /// <summary>Maximum X coordinate.</summary>
    public double MaxX { get; }

    /// <summary>Maximum Y coordinate.</summary>
    public double MaxY { get; }

    /// <summary>Maximum Z coordinate.</summary>
    public double MaxZ { get; }

    /// <summary>Gets whether the bounding box is empty (any minimum exceeds its corresponding maximum).</summary>
    public bool IsEmpty => MinX > MaxX || MinY > MaxY || MinZ > MaxZ;

    /// <summary>X coordinate of the bounding box center.</summary>
    public double CenterX => (MinX + MaxX) / 2.0;

    /// <summary>Y coordinate of the bounding box center.</summary>
    public double CenterY => (MinY + MaxY) / 2.0;

    /// <summary>Z coordinate of the bounding box center.</summary>
    public double CenterZ => (MinZ + MaxZ) / 2.0;

    /// <summary>Radius of the bounding sphere (half the box diagonal).</summary>
    public double Radius
    {
        get
        {
            if (IsEmpty)
            {
                return 0.0;
            }

            var dx = MaxX - MinX;
            var dy = MaxY - MinY;
            var dz = MaxZ - MinZ;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz) / 2.0;
        }
    }

    /// <summary>Returns the union of this bounding box and <paramref name="other"/>.</summary>
    public Bounds3D Union(Bounds3D other)
    {
        if (IsEmpty)
        {
            return other;
        }

        if (other.IsEmpty)
        {
            return this;
        }

        return new Bounds3D(
            Math.Min(MinX, other.MinX), Math.Min(MinY, other.MinY), Math.Min(MinZ, other.MinZ),
            Math.Max(MaxX, other.MaxX), Math.Max(MaxY, other.MaxY), Math.Max(MaxZ, other.MaxZ));
    }
}
