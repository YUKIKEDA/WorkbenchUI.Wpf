---
title: Theme Architecture
description: Why WorkbenchUI.Wpf uses a custom high-density neutral-gray theme system instead of WPF Fluent ThemeMode.
sidebar_position: 1
---

# Theme Architecture

## Why a Custom Theme Instead of WPF's Built-in Fluent `ThemeMode`?

WPF introduced built-in Fluent styling (`ThemeMode`), which works well for consumer productivity apps but creates friction in dense engineering workbenches:

1. **Density**: Consumer Fluent themes favor spacious padding and tall hit targets (`32–40 px`). Engineering inspectors, ribbons, property grids, and toolbars pack dozens of parameters onto one screen (`24 px` `Wb.ControlHeight`, `4 px` spacing grid, `2 px` corner radius).
2. **Achromatic Neutrality**: In post-processing and visualization tools, users read physical quantities directly from color-mapped contours, plots, and status badges. Tinted mica/acrylic surfaces or colorful chrome distort color perception. WorkbenchUI.Wpf keeps all structural surfaces strictly neutral gray, reserving color for the single active accent and semantic status indicators.
3. **Stability**: Own-authored control templates avoid relying on internal WPF Fluent template structures that can shift across .NET updates.

## Two-Layer Semantic Token Model

Design tokens in `Tokens.Dark.xaml` and `Tokens.Light.xaml` are structured in two layers:

1. **Layer 1 — Semantic Colors (`Wb.Color.*`)**: Role-based `Color` primitives (`Wb.Color.Surface.Window`, `Wb.Color.Accent.Default`, etc.). Components that cannot consume WPF `Brush` resources directly—such as AvalonDock theme dictionaries and ScottPlot raster rendering—read these color keys.
2. **Layer 2 — Semantic Brushes (`Wb.Brush.*`)**: `SolidColorBrush` resources referencing Layer 1, consumed by WPF control templates.

Literal palette names (such as `Gray800`) are intentionally avoided because a "dark gray" name becomes meaningless under a Light theme. Both `Tokens.Dark.xaml` and `Tokens.Light.xaml` maintain identical keys.

## `DynamicResource` vs. `StaticResource`

- **Colors and brushes** are always referenced via **`DynamicResource`**, enabling instant runtime switching via `ThemeManager.SetTheme` and dynamic accent overrides via `ThemeManager.SetAccent` without reloading windows.
- **Metrics and spacing** (`Wb.ControlHeight`, `Wb.Spacing.*`, `Wb.FontSize.*`, `Wb.CornerRadius`) are invariant across theme variants and use **`StaticResource`**.

## Lookless Controls & Localization Separation

Every custom control inherits from `Control` (or `ItemsControl` / `Selector` / `Button` / `Window`) with its default template in `Themes/Generic.xaml`—no `UserControl` black boxes—so consumers can re-template any control. Built-in strings default to English and are exposed as dependency properties (for example `PropertyGrid.FilterPlaceholder` or `WorkbenchRibbon.ApplicationLabel`) rather than embedded `.resx` tables, leaving localization in the host application's hands.
