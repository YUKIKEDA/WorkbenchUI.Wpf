---
title: Introduction
description: Overview of WorkbenchUI.Wpf, prerequisites, package selection, and documentation map.
sidebar_position: 1
---

# Welcome to WorkbenchUI.Wpf

**WorkbenchUI.Wpf** is a high-density, low-chroma WPF UI library for engineering, scientific, and technical desktop applications targeting **.NET 10 (`net10.0-windows`)**.

Its neutral-gray surfaces, compact 24 px control metrics, and dynamic Dark/Light theming are built so your application chrome stays sharp without competing against color-mapped simulation contours, plots, or 3D viewports.

## Prerequisites

- **OS**: Windows 10 / 11 (WPF + Direct3D 11 for the 3D viewport)
- **SDK**: [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0-windows`)

## Package Overview

| Package | Role | External Dependencies |
| --- | --- | --- |
| `WorkbenchUI.Wpf` | Theme foundation (`Dark`/`Light`), semantic tokens, 67 vector icons, core & workbench controls | None (WPF only) |
| `WorkbenchUI.Wpf.Charts` | ScottPlot 5 theme synchronization & composite engineering plots | `ScottPlot.WPF` (MIT) |
| `WorkbenchUI.Wpf.Docking` | AvalonDock token-driven theme & XML layout persistence | `Dirkster.AvalonDock` (Ms-PL) |
| `WorkbenchUI.Wpf.Viewport3D` | Direct3D 11 3D viewport for meshes, sparse voxels, contours, deformation, and probing | `Silk.NET` Direct3D / DXGI (MIT) |

All four packages share a single unified XAML namespace URI:

```xml
xmlns:wb="https://schemas.workbenchui.dev/wpf"
```

## Documentation

### Tutorials
- [Build Your First Workbench App](tutorials/first-workbench-app.md)

### How-to Guides
- [Customize Themes & Accent Colors](how-to/theming-and-accent.md)
- [Work with Units & Numeric Inputs](how-to/units-and-numeric-input.md)
- [Build Dynamic Property Grids](how-to/property-grid.md)
- [Use ModelTree with Tri-State Visibility](how-to/model-tree.md)
- [Integrate Docking & Charts](how-to/docking-and-charts.md)
- [Render 3D Meshes & Voxels](how-to/viewport-3d.md)

### Reference
- [Design Tokens & Named Styles](reference/design-tokens.md)
- [Controls Catalog](reference/controls-catalog.md)
- [Built-in Icons](reference/icons.md)
- [Extension Packages](reference/extension-packages.md)

### Explanation
- [Theme Architecture](explanation/theme-architecture.md)
- [Package & API Architecture](explanation/package-architecture.md)
