---
title: Design Tokens & Named Styles
description: Complete reference for semantic Color, Brush, metric tokens, and keyed styles in WorkbenchUI.Wpf.
sidebar_position: 1
---

# Design Tokens & Named Styles

All token keys follow the `Wb.<Category>.<Role>[.<State>]` convention.

## Core Metric & Typography Tokens (`Tokens.Core.xaml`)

Use `StaticResource` for metric tokens (they do not change across theme variants).

| Key | Type | Value | Notes |
| --- | --- | --- | --- |
| `Wb.FontFamily.Default` | `FontFamily` | `Segoe UI, Yu Gothic UI` | Primary UI font |
| `Wb.FontFamily.Monospace` | `FontFamily` | `Cascadia Mono, Consolas` | Log console, numeric/code alignment |
| `Wb.FontSize.S` | `Double` | `11` | Captions, axis labels |
| `Wb.FontSize.M` | `Double` | `12` | Default control text |
| `Wb.FontSize.L` | `Double` | `14` | Section headers, titles |
| `Wb.Spacing.XS` | `Double` | `2` | Tight spacing |
| `Wb.Spacing.S` | `Double` | `4` | Base 4 px grid unit |
| `Wb.Spacing.M` | `Double` | `8` | Standard gap |
| `Wb.Spacing.L` | `Double` | `12` | Panel / section margin |
| `Wb.ControlHeight` | `Double` | `24` | Standard compact control height |
| `Wb.IconSize` | `Double` | `16` | Default `WorkbenchIcon` size |
| `Wb.CornerRadius` | `CornerRadius` | `2` | Near-square workbench radius |
| `Wb.Padding.Input` | `Thickness` | `4,2` | Input padding |
| `Wb.Padding.Panel` | `Thickness` | `8` | Panel container padding |

## Semantic Color Tokens (`Tokens.Dark.xaml` / `Tokens.Light.xaml`)

Use `DynamicResource` for all `Wb.Color.*` and `Wb.Brush.*` tokens. Every `Wb.Color.*` entry (except `Text.Muted`) has a corresponding `Wb.Brush.*` `SolidColorBrush` entry.

| Key | Dark | Light | Role |
| --- | --- | --- | --- |
| `Wb.Color.Surface.Window` | `#1E1E1E` | `#EEEEF2` | Window / chrome background |
| `Wb.Color.Surface.Panel` | `#252526` | `#F5F5F7` | Tool panels, sidebars |
| `Wb.Color.Surface.Elevated` | `#2D2D30` | `#FAFAFC` | Toolbars, menus, popups, headers |
| `Wb.Color.Surface.Input` | `#333337` | `#FFFFFF` | Text inputs, editors |
| `Wb.Color.Text.Primary` | `#F1F1F1` | `#1E1E1E` | Primary foreground text |
| `Wb.Color.Text.Muted` | `#C5C5C5` | `#4B4B52` | Muted text / chart axes |
| `Wb.Color.Text.Secondary` | `#969696` | `#6E6E76` | Secondary / hint text |
| `Wb.Color.Text.Disabled` | `#6E6E73` | `#9B9BA3` | Disabled text |
| `Wb.Color.Text.OnAccent` | `#FFFFFF` | `#FFFFFF` | Text on accent backgrounds |
| `Wb.Color.Control.Background` | `#333337` | `#ECECF0` | Default button / control surface |
| `Wb.Color.Control.Hover` | `#3F3F46` | `#DADAE0` | Hovered control surface |
| `Wb.Color.Control.Pressed` | `#55555A` | `#C8C8D0` | Pressed control surface |
| `Wb.Color.Border.Default` | `#3F3F46` | `#CCCEDB` | Standard border |
| `Wb.Color.Border.Strong` | `#55555A` | `#A0A2AE` | Emphasized border |
| `Wb.Color.State.Selected` | `#094771` | `#C7E0F4` | Active list/tree selection |
| `Wb.Color.State.SelectedInactive` | `#37373D` | `#E1E1E6` | Unfocused selection |
| `Wb.Color.Error` | `#F14C4C` | `#C42B1C` | Error border / icon |
| `Wb.Color.Warning` | `#CCA700` | `#9D5D00` | Warning status |
| `Wb.Color.Success` | `#89D185` | `#107C10` | Success status |
| `Wb.Color.Info` | `#75BEFF` | `#005FB8` | Informational status |
| `Wb.Color.Danger.Default` | `#C42B1C` | `#C42B1C` | Danger button background |
| `Wb.Color.Danger.Hover` | `#D13438` | `#A4262C` | Danger button hover |
| `Wb.Color.Danger.Pressed` | `#A4262C` | `#8B1F17` | Danger button pressed |
| `Wb.Color.Accent.Default` | `#007ACC` | `#007ACC` | Primary accent (overridable via `ThemeManager.SetAccent`) |
| `Wb.Color.Accent.Hover` | `#1C97EA` | `#1C97EA` | Accent hover (derived via HSL) |
| `Wb.Color.Accent.Pressed` | `#005A9E` | `#005A9E` | Accent pressed (derived via HSL) |
| `Wb.Color.Accent.Muted` | `#094771` | `#C7E0F4` | Accent muted fill (derived per variant) |

## Brush-Only Overlay Tokens

| Key | Dark | Light | Role |
| --- | --- | --- | --- |
| `Wb.Brush.Border.Focus` | Follows `Accent.Default` | Follows `Accent.Default` | Keyboard focus ring |
| `Wb.Brush.Surface.Alternate` | `#08FFFFFF` | `#06000000` | Alternating row shading |
| `Wb.Brush.State.Hover` | `#14FFFFFF` | `#0D000000` | Translucent row/item hover overlay |
| `Wb.Brush.State.Pressed` | `#1FFFFFFF` | `#1A000000` | Translucent row/item pressed overlay |

## Keyed Control Styles

| Style Key | Target Type | Purpose |
| --- | --- | --- |
| `Wb.Button` | `Button` | Default workbench button |
| `Wb.Button.Accent` | `Button` | Primary call-to-action button |
| `Wb.Button.Danger` | `Button` | Destructive action button |
| `Wb.Button.Subtle` | `Button` | Low-emphasis / flat button |
| `Wb.Button.Icon` | `Button` | Compact icon-only toolbar button (`ButtonAssist.Icon`) |
| `Wb.DropDownButton.Accent` / `.Subtle` | `DropDownButton` | Accent & subtle dropdown buttons |
| `Wb.SplitButton.Accent` / `.Subtle` | `SplitButton` | Accent & subtle split buttons |
| `Wb.Ribbon.Button.Large` / `.Large.Accent` / `.Small` | `Button` | Ribbon command button variants |
| `Wb.Ribbon.SplitButton.Large.Accent` | `SplitButton` | Ribbon split button accent variant |
| `Wb.Ribbon.Toggle.Large` / `.Small` | `ToggleButton` | Ribbon toggle button variants |
| `Wb.DataGrid.DetailsToggle` | `ToggleButton` | Row details expand/collapse toggle |
