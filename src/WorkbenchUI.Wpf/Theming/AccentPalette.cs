using System.Windows.Media;

namespace WorkbenchUI.Wpf.Theming;

/// <summary>
/// State-derived accent color palette computed from a base accent color.
/// </summary>
internal readonly record struct AccentPalette(Color Default, Color Hover, Color Pressed, Color Muted)
{
    /// <summary>
    /// Computes derived accent colors from a base color using HSL lightness adjustments for the dark theme variant.
    /// <see cref="Muted"/> is darkened significantly for use in selection backgrounds and similar surfaces.
    /// </summary>
    public static AccentPalette FromBase(Color baseColor) =>
        FromBase(baseColor, WorkbenchThemeVariant.Dark);

    /// <summary>
    /// Computes derived accent colors from a base color for the specified theme variant.
    /// <see cref="Muted"/> is a low-contrast tint or shade used for selection backgrounds - darkened in dark mode
    /// and lightened in light mode to preserve foreground text contrast.
    /// </summary>
    public static AccentPalette FromBase(Color baseColor, WorkbenchThemeVariant variant) => new(
        Default: baseColor,
        Hover: ColorMath.Lighten(baseColor, 0.10),
        Pressed: ColorMath.Darken(baseColor, 0.10),
        Muted: variant == WorkbenchThemeVariant.Light
            ? ColorMath.Lighten(baseColor, 0.45)
            : ColorMath.Darken(baseColor, 0.35));
}
