using System.Windows.Media;

namespace WorkbenchUI.Wpf.Theming;

/// <summary>
/// Provides runtime theme switching and dynamic accent color overrides.
/// Tracks merged <see cref="WorkbenchTheme"/> instances via weak references
/// and updates them on the UI thread.
/// </summary>
public static class ThemeManager
{
    private static readonly List<WeakReference<WorkbenchTheme>> Themes = [];

    /// <summary>Current accent override color set via <see cref="SetAccent"/>, or <see langword="null"/> if using theme defaults.</summary>
    private static Color? _accentOverride;

    private static readonly string[] AccentKeys =
    [
        "Wb.Brush.Accent.Default",
        "Wb.Brush.Accent.Hover",
        "Wb.Brush.Accent.Pressed",
        "Wb.Brush.Accent.Muted",
        "Wb.Brush.Border.Focus",
        "Wb.Color.Accent.Default",
        "Wb.Color.Accent.Hover",
        "Wb.Color.Accent.Pressed",
        "Wb.Color.Accent.Muted",
    ];

    /// <summary>
    /// Occurs when the active theme variant or accent color changes at runtime.
    /// Useful for raster-based consumers (such as <c>WorkbenchUI.Wpf.Charts</c>) that cannot use <c>DynamicResource</c>.
    /// Raised on the caller's UI thread.
    /// </summary>
    public static event EventHandler? ThemeChanged;

    /// <summary>
    /// Gets the current theme variant. Defaults to <see cref="WorkbenchThemeVariant.Dark"/>.
    /// </summary>
    public static WorkbenchThemeVariant CurrentTheme { get; private set; } = WorkbenchThemeVariant.Dark;

    /// <summary>
    /// Gets the active accent override color, or <see langword="null"/> when the theme default accent is active.
    /// </summary>
    public static Color? CurrentAccent => _accentOverride;

    /// <summary>
    /// Reads the Windows "Apps" light/dark preference from the registry.
    /// Whether to apply it on startup or track OS changes is left to the host application.
    /// Returns <see cref="WorkbenchThemeVariant.Light"/> if the setting cannot be read.
    /// </summary>
    public static WorkbenchThemeVariant GetSystemTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0
                ? WorkbenchThemeVariant.Dark
                : WorkbenchThemeVariant.Light;
        }
        catch (Exception e) when (e is System.Security.SecurityException or System.IO.IOException)
        {
            return WorkbenchThemeVariant.Light;
        }
    }

    /// <summary>Switches all registered <see cref="WorkbenchTheme"/> dictionaries to the specified variant.</summary>
    public static void SetTheme(WorkbenchThemeVariant variant)
    {
        CurrentTheme = variant;
        foreach (var theme in AliveThemes())
        {
            theme.Apply(variant, notifyManager: false);

            // Recompute Muted when an accent override is active, as its lightness direction depends on the variant.
            if (_accentOverride is { } accent)
            {
                ApplyAccentTo(theme, AccentPalette.FromBase(accent, variant));
            }
        }

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Overrides the theme accent color. Derived Hover, Pressed, and Muted colors
    /// are automatically calculated via HSL lightness adjustments.
    /// </summary>
    public static void SetAccent(Color accent)
    {
        _accentOverride = accent;
        foreach (var theme in AliveThemes())
        {
            ApplyAccentTo(theme, AccentPalette.FromBase(accent, theme.Theme));
        }

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Clears any accent color override and restores the active theme's default accent.</summary>
    public static void ResetAccent()
    {
        _accentOverride = null;
        foreach (var theme in AliveThemes())
        {
            foreach (var key in AccentKeys)
            {
                theme.Remove(key);
            }
        }

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    internal static void SyncFromTheme(WorkbenchTheme theme, WorkbenchThemeVariant variant, bool raiseThemeChanged = true)
    {
        CurrentTheme = variant;
        if (_accentOverride is { } accent)
        {
            ApplyAccentTo(theme, AccentPalette.FromBase(accent, variant));
        }

        if (raiseThemeChanged)
        {
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    private static void ApplyAccentTo(WorkbenchTheme theme, AccentPalette palette)
    {
        // Local entries on WorkbenchTheme shadow merged dictionaries, so overrides survive theme switches.
        theme["Wb.Brush.Accent.Default"] = CreateFrozenBrush(palette.Default);
        theme["Wb.Brush.Accent.Hover"] = CreateFrozenBrush(palette.Hover);
        theme["Wb.Brush.Accent.Pressed"] = CreateFrozenBrush(palette.Pressed);
        theme["Wb.Brush.Accent.Muted"] = CreateFrozenBrush(palette.Muted);
        theme["Wb.Brush.Border.Focus"] = CreateFrozenBrush(palette.Default);

        // Update color primitives as well for consumers that cannot bind brush resources directly.
        theme["Wb.Color.Accent.Default"] = palette.Default;
        theme["Wb.Color.Accent.Hover"] = palette.Hover;
        theme["Wb.Color.Accent.Pressed"] = palette.Pressed;
        theme["Wb.Color.Accent.Muted"] = palette.Muted;
    }

    internal static void Register(WorkbenchTheme theme)
    {
        Themes.RemoveAll(r => !r.TryGetTarget(out _));
        Themes.Add(new WeakReference<WorkbenchTheme>(theme));
    }

    private static IEnumerable<WorkbenchTheme> AliveThemes()
    {
        foreach (var reference in Themes)
        {
            if (reference.TryGetTarget(out var theme))
            {
                yield return theme;
            }
        }
    }

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
