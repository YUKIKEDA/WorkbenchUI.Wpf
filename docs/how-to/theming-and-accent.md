---
title: Customize Themes & Accent Colors
description: How to configure Dark/Light themes, follow Windows OS preference, and override accent colors at runtime.
sidebar_position: 1
---

# How to Customize Themes & Accent Colors

## Registering the Theme Dictionary

Add `WorkbenchTheme` to `App.xaml` with your preferred initial variant (`Dark` or `Light`):

```xml
<Application.Resources>
    <wb:WorkbenchTheme Theme="Dark" />
</Application.Resources>
```

## Switching Between Dark and Light at Runtime

Call `ThemeManager.SetTheme` on the UI thread. Every registered `WorkbenchTheme` dictionary updates immediately because control styles reference semantic brush tokens via `DynamicResource`:

```csharp
using WorkbenchUI.Wpf.Theming;

ThemeManager.SetTheme(WorkbenchThemeVariant.Light);
```

You can inspect the active variant at any time via `ThemeManager.CurrentTheme`.

## Matching the Windows App Theme on Startup

Use `ThemeManager.GetSystemTheme()` to read the user's Windows light/dark preference (`AppsUseLightTheme`) and apply it at startup:

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    ThemeManager.SetTheme(ThemeManager.GetSystemTheme());
}
```

## Changing the Accent Color Dynamically

Use `ThemeManager.SetAccent` to replace the accent color across all controls, focus borders, charts, and docking chrome. `ThemeManager` automatically derives `Hover`, `Pressed`, and `Muted` shades using HSL lightness adjustments:

```csharp
using System.Windows.Media;
using WorkbenchUI.Wpf.Theming;

// Apply a green engineering accent
ThemeManager.SetAccent(Color.FromRgb(0x2E, 0xA0, 0x43));

// Query the current override (null if using theme default)
Color? activeAccent = ThemeManager.CurrentAccent;

// Restore the theme default accent (#007ACC)
ThemeManager.ResetAccent();
```

## Reacting to Theme Changes in Custom Code

If you maintain non-WPF or raster-rendered visuals (such as custom Skia/Direct2D drawings), subscribe to `ThemeManager.ThemeChanged`:

```csharp
Loaded += (_, _) => ThemeManager.ThemeChanged += OnThemeChanged;
Unloaded += (_, _) => ThemeManager.ThemeChanged -= OnThemeChanged;

private void OnThemeChanged(object? sender, EventArgs e)
{
    // Re-read Wb.Color.* resources from Application.Current and redraw
}
```

*(Note: `WorkbenchPlot`, `WorkbenchDockTheme`, and `WorkbenchViewport` already subscribe to theme/token changes automatically.)*

## Referencing Tokens in Your Own XAML

Follow these two rules so custom styles remain theme-aware:

1. Use **`DynamicResource`** for all `Wb.Brush.*` and `Wb.Color.*` tokens.
2. Use **`StaticResource`** for `Wb.Spacing.*`, `Wb.FontSize.*`, `Wb.CornerRadius`, and `Wb.ControlHeight`.

```xml
<Border Background="{DynamicResource Wb.Brush.Surface.Panel}"
        BorderBrush="{DynamicResource Wb.Brush.Border.Default}"
        BorderThickness="1"
        CornerRadius="{StaticResource Wb.CornerRadius}"
        Padding="{StaticResource Wb.Padding.Panel}">
    <TextBlock Text="Inspector"
               Foreground="{DynamicResource Wb.Brush.Text.Primary}"
               FontSize="{StaticResource Wb.FontSize.M}" />
</Border>
```
