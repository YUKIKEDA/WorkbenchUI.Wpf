using System.Windows.Media;

namespace WorkbenchUI.Wpf.Theming;

/// <summary>
/// HSL and HSV color math utilities used for dynamic accent palette generation.
/// </summary>
internal static class ColorMath
{
    /// <summary>Converts an RGB color to HSL, where H is in [0, 360] and S/L are in [0, 1]. Alpha is ignored.</summary>
    public static (double H, double S, double L) ToHsl(Color color)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double l = (max + min) / 2.0;

        if (delta == 0)
        {
            return (0, 0, l);
        }

        double s = delta / (1 - Math.Abs(2 * l - 1));

        double h;
        if (max == r)
        {
            h = 60 * (((g - b) / delta) % 6);
        }
        else if (max == g)
        {
            h = 60 * ((b - r) / delta + 2);
        }
        else
        {
            h = 60 * ((r - g) / delta + 4);
        }

        if (h < 0)
        {
            h += 360;
        }

        return (h, s, l);
    }

    /// <summary>Converts HSL values (H in [0, 360], S/L in [0, 1]) to an opaque RGB <see cref="Color"/>.</summary>
    public static Color FromHsl(double h, double s, double l)
    {
        s = Math.Clamp(s, 0, 1);
        l = Math.Clamp(l, 0, 1);
        h = ((h % 360) + 360) % 360;

        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        double m = l - c / 2;

        (double r, double g, double b) = (h / 60) switch
        {
            < 1 => (c, x, 0.0),
            < 2 => (x, c, 0.0),
            < 3 => (0.0, c, x),
            < 4 => (0.0, x, c),
            < 5 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };

        return Color.FromRgb(
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255));
    }

    /// <summary>Converts an RGB color to HSV, where H is in [0, 360] and S/V are in [0, 1]. Alpha is ignored.</summary>
    public static (double H, double S, double V) ToHsv(Color color)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double v = max;
        double s = max == 0 ? 0 : delta / max;

        if (delta == 0)
        {
            return (0, s, v);
        }

        double h;
        if (max == r)
        {
            h = 60 * (((g - b) / delta) % 6);
        }
        else if (max == g)
        {
            h = 60 * ((b - r) / delta + 2);
        }
        else
        {
            h = 60 * ((r - g) / delta + 4);
        }

        if (h < 0)
        {
            h += 360;
        }

        return (h, s, v);
    }

    /// <summary>Converts HSV values (H in [0, 360], S/V in [0, 1]) to an opaque RGB <see cref="Color"/>.</summary>
    public static Color FromHsv(double h, double s, double v)
    {
        s = Math.Clamp(s, 0, 1);
        v = Math.Clamp(v, 0, 1);
        h = ((h % 360) + 360) % 360;

        double c = v * s;
        double x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        double m = v - c;

        (double r, double g, double b) = (h / 60) switch
        {
            < 1 => (c, x, 0.0),
            < 2 => (x, c, 0.0),
            < 3 => (0.0, c, x),
            < 4 => (0.0, x, c),
            < 5 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };

        return Color.FromRgb(
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255));
    }

    /// <summary>Returns a color lightened by the specified HSL lightness amount in [0, 1].</summary>
    public static Color Lighten(Color color, double amount)
    {
        var (h, s, l) = ToHsl(color);
        return FromHsl(h, s, l + amount);
    }

    /// <summary>Returns a color darkened by the specified HSL lightness amount in [0, 1].</summary>
    public static Color Darken(Color color, double amount)
    {
        var (h, s, l) = ToHsl(color);
        return FromHsl(h, s, l - amount);
    }
}
