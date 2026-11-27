using System.Windows.Media;
using WorkbenchUI.Wpf.Theming;

namespace WorkbenchUI.Wpf.Gallery.Infrastructure;

/// <summary>Represents a curated accent color option in the Gallery shell and settings.</summary>
public sealed record AccentPreset(string Name, Color? Color, Color PreviewColor);

/// <summary>Provides built-in accent color presets for the Gallery application.</summary>
public static class GalleryAccentPresets
{
    /// <summary>Ordered list of preset accent colors.</summary>
    public static IReadOnlyList<AccentPreset> Presets { get; } =
    [
        new("Workbench Blue", null, Color.FromRgb(0x00, 0x7A, 0xCC)),
        new("Engineering Orange", Color.FromRgb(0xCA, 0x51, 0x00), Color.FromRgb(0xCA, 0x51, 0x00)),
        new("Emerald Green", Color.FromRgb(0x2E, 0x8B, 0x57), Color.FromRgb(0x2E, 0x8B, 0x57)),
        new("Violet", Color.FromRgb(0x8E, 0x47, 0xCF), Color.FromRgb(0x8E, 0x47, 0xCF)),
        new("Teal", Color.FromRgb(0x00, 0x8B, 0x8B), Color.FromRgb(0x00, 0x8B, 0x8B)),
        new("Crimson", Color.FromRgb(0xC4, 0x2B, 0x1C), Color.FromRgb(0xC4, 0x2B, 0x1C)),
    ];

    /// <summary>Applies the given preset (or resets to theme default when <see cref="AccentPreset.Color"/> is null).</summary>
    public static void Apply(AccentPreset preset)
    {
        if (preset.Color is { } color)
        {
            ThemeManager.SetAccent(color);
        }
        else
        {
            ThemeManager.ResetAccent();
        }
    }
}
