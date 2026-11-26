using System.Windows.Media;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Defines a color map that maps normalized values in the range 0 to 1 to colors.
/// Includes built-in preset scales; custom maps can also be created via the constructor.
/// </summary>
public sealed class ColorMap
{
    private readonly (double Offset, Color Color)[] _stops;

    /// <summary>Initializes a new instance of the <see cref="ColorMap"/> class.</summary>
    /// <param name="name">The display name of the color map.</param>
    /// <param name="stops">Color stops with offsets in [0, 1]. At least two stops are required.</param>
    public ColorMap(string name, params (double Offset, Color Color)[] stops)
    {
        if (stops.Length < 2)
        {
            throw new ArgumentException("ColorMap requires at least two stops.", nameof(stops));
        }

        Name = name;
        _stops = [.. stops.OrderBy(s => s.Offset)];
    }

    /// <summary>Gets the display name of the color map.</summary>
    public string Name { get; }

    /// <summary>Gets the color stops ordered by offset (used for building legend gradients, etc.).</summary>
    public IReadOnlyList<(double Offset, Color Color)> Stops => _stops;

    /// <summary>Returns the linearly interpolated color for a normalized value <paramref name="t"/> (clamped to 0–1).</summary>
    public Color GetColor(double t)
    {
        if (double.IsNaN(t))
        {
            return _stops[0].Color;
        }

        t = Math.Clamp(t, 0.0, 1.0);

        for (var i = 1; i < _stops.Length; i++)
        {
            if (t <= _stops[i].Offset)
            {
                var (o0, c0) = _stops[i - 1];
                var (o1, c1) = _stops[i];
                var f = o1 > o0 ? (t - o0) / (o1 - o0) : 0.0;
                return Lerp(c0, c1, f);
            }
        }

        return _stops[^1].Color;
    }

    private static Color Lerp(Color a, Color b, double f) => Color.FromArgb(
        (byte)Math.Round(a.A + (b.A - a.A) * f),
        (byte)Math.Round(a.R + (b.R - a.R) * f),
        (byte)Math.Round(a.G + (b.G - a.G) * f),
        (byte)Math.Round(a.B + (b.B - a.B) * f));

    private static Color C(byte r, byte g, byte b) => Color.FromRgb(r, g, b);

    // ---------------- Built-in color maps ----------------

    /// <summary>Gets the built-in Jet (rainbow) color map.</summary>
    public static ColorMap Jet { get; } = new("Jet",
        (0.000, C(0x00, 0x00, 0x7F)),
        (0.125, C(0x00, 0x00, 0xFF)),
        (0.375, C(0x00, 0xFF, 0xFF)),
        (0.625, C(0xFF, 0xFF, 0x00)),
        (0.875, C(0xFF, 0x00, 0x00)),
        (1.000, C(0x7F, 0x00, 0x00)));

    /// <summary>Gets the built-in perceptually uniform Viridis color map.</summary>
    public static ColorMap Viridis { get; } = new("Viridis",
        (0.00, C(0x44, 0x01, 0x54)),
        (0.25, C(0x3B, 0x52, 0x8B)),
        (0.50, C(0x21, 0x91, 0x8C)),
        (0.75, C(0x5E, 0xC9, 0x62)),
        (1.00, C(0xFD, 0xE7, 0x25)));

    /// <summary>Gets the built-in diverging Coolwarm color map.</summary>
    public static ColorMap Coolwarm { get; } = new("Coolwarm",
        (0.0, C(0x3B, 0x4C, 0xC0)),
        (0.5, C(0xDD, 0xDD, 0xDD)),
        (1.0, C(0xB4, 0x04, 0x26)));

    /// <summary>Gets the built-in Grayscale color map.</summary>
    public static ColorMap Grayscale { get; } = new("Grayscale",
        (0.0, C(0x00, 0x00, 0x00)),
        (1.0, C(0xFF, 0xFF, 0xFF)));

    /// <summary>Gets the list of all built-in color maps.</summary>
    public static IReadOnlyList<ColorMap> BuiltIn { get; } = [Jet, Viridis, Coolwarm, Grayscale];
}
