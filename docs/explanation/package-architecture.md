---
title: Package & API Architecture
description: Design principles behind zero-dependency core isolation, thread-safe data models, and slim public APIs.
sidebar_position: 2
---

# Package & API Architecture

## Zero-Dependency Core + Isolated Extensions

`WorkbenchUI.Wpf` has **zero external NuGet dependencies**. Complex integrations are split into separate packages:

- **`WorkbenchUI.Wpf.Charts`** isolates `ScottPlot.WPF` (MIT).
- **`WorkbenchUI.Wpf.Docking`** isolates `Dirkster.AvalonDock` (**Ms-PL**), keeping Ms-PL licensing boundaries cleanly separated from the MIT core.
- **`WorkbenchUI.Wpf.Viewport3D`** isolates `Silk.NET` Direct3D 11 / D3D9 / DXGI bindings (MIT).

All four assemblies map their public CLR namespaces onto `https://schemas.workbenchui.dev/wpf`, so XAML consumers write a single `xmlns:wb` declaration regardless of which extension packages they install.

## UI-Decoupled Data Models

High-throughput engineering controls separate thread-safe / UI-independent models from WPF visual trees:

- **`LogBuffer` & `LogConsole`**: Worker threads append to `LogBuffer` lock-free via `ConcurrentQueue<LogEntry>`; `LogConsole` flushes batches on a `DispatcherTimer` into a virtualized `ListBox`, sustaining thousands of log lines per second without stalling the UI thread.
- **`ConvergenceSeries` & `ConvergenceMonitor`**: Solver threads call `ConvergenceSeries.Append` safely; the chart snapshots data on a throttled refresh cadence.
- **`ViewportMesh` & `ViewportVoxelGrid`**: Arrays (`Positions`, `ScalarValues`, `Displacements`, `VectorValues`) use **instance replacement** rather than in-place mutation, enabling partial GPU buffer uploads (scalar contours or transient displacements) without tearing down geometry.

## Slim Public API Surface

Internal algorithmic helpers (such as `ColorMath`, `AccentPalette`, `NumericText`, `FlatTreeSource`, and internal `Viewport3D` culling/LOD/surface extraction helpers) are marked `internal` and exposed to unit test assemblies via `InternalsVisibleTo`. This keeps IntelliSense focused on supported application contracts and allows internal optimization without breaking semantic versioning.
