---
title: Extension Packages
description: Public API reference for WorkbenchUI.Wpf.Charts, WorkbenchUI.Wpf.Docking, and WorkbenchUI.Wpf.Viewport3D.
sidebar_position: 4
---

# Extension Packages Reference

Each extension package depends on `WorkbenchUI.Wpf` and isolates its external dependency behind the unified `https://schemas.workbenchui.dev/wpf` XAML namespace.

---

## `WorkbenchUI.Wpf.Charts`

- **External Dependency**: `ScottPlot.WPF` (`5.1.59`, MIT)
- **Namespace**: `WorkbenchUI.Wpf.Charts`

| Type | Role | Key Members |
| --- | --- | --- |
| `WorkbenchPlot` | `ScottPlot.WPF.WpfPlot` subclass | `WheelZoomRequiresCtrl` (default `true`), `ThemeApplied`, automatic `ThemeManager.ThemeChanged` sync |
| `WorkbenchChartTheme` | Theme helper | `Apply(ScottPlot.Plot)`, `GetSeriesPalette()`, `ToScottPlotColor(Color)` |
| `ConvergenceMonitor` | Log residual monitor | `SeriesSource`, `Threshold`, `IsLogScale`, `XLabel`, `YLabel` |
| `ConvergenceSeries` | Thread-safe residual series | `Name`, `Color`, `Count`, `Append(double)`, `Clear()`, `Snapshot()`, `Changed` |
| `HistoryChart` | Multi-series XY plot | `SeriesSource`, `XLabel`, `YLabel`, `ShowLegend`, `ShowCrosshair` |
| `ChartSeries` | XY series model | `Name`, `X`, `Y`, `Color`, `LineWidth` |
| `FrequencyResponsePlot` | Dual Bode magnitude/phase plot | `SeriesSource`, `ShowPhase`, `MagnitudeInDecibels`, `XLabel` |
| `FrequencyResponseSeries` | FRF series model | `Name`, `Frequencies`, `Magnitudes`, `Phases`, `Color` |
| `HistogramChart` | Distribution bar histogram | `Values`, `BinCount`, `Normalize`, `XLabel` |

---

## `WorkbenchUI.Wpf.Docking`

- **External Dependency**: `Dirkster.AvalonDock` & `Dirkster.AvalonDock.Themes.VS2013` (`4.74.1`, **Ms-PL**)
- **Namespace**: `WorkbenchUI.Wpf.Docking`

| Type | Role | Key Members |
| --- | --- | --- |
| `WorkbenchDockTheme` | AvalonDock `DictionaryTheme` | Applies VS2013 templates dynamically bound to `Wb.Color.*` tokens |
| `DockLayout` | Layout persistence helper | `Save(manager, path)`, `SaveToString(manager)`, `Load(manager, path, resolveContent)`, `LoadFromString(manager, xml, resolveContent)` |

---

## `WorkbenchUI.Wpf.Viewport3D`

- **External Dependency**: `Silk.NET.Direct3D11`, `Direct3D9`, `DXGI`, `Direct3D.Compilers` (`2.23.0`, MIT)
- **Namespace**: `WorkbenchUI.Wpf.Viewport3D`

| Type | Role | Key Members |
| --- | --- | --- |
| `WorkbenchViewport` | Direct3D 11 3D viewport control | `MeshSource`, `VoxelSource`, `ColorScale`, `ShowContours`, `ShowEdges`, `Projection`, `UpAxis`, `ShowAxisTriad`, `ShowViewCube`, `PickMode`, `DeformationScale`, `IsDeformationAnimated`, `ShowUndeformedWireframe`, `SectionPlane`, `ShowSectionPlaneIndicator`, `ShowGlyphs`, `GlyphScale`, `GlyphColorScale`, `IsHoverHighlightEnabled`, `RubberBandSelectionMode`, `IsGeometryBuilding`, `Camera`, `Selection`, `Annotations`, `FitToView()`, `SetStandardView(...)`, `GetStatistics()`, `GetSuggestedDeformationScale()`, `GetSuggestedGlyphScale()`, `InvalidateViewport()` |
| `ViewportMesh` | Triangle surface mesh model | `Name`, `Positions`, `TriangleIndices`, `ScalarValues`, `Displacements`, `VectorValues`, `Color`, `IsVisible`, `ShowEdges`, `IsClippable`, `Opacity` |
| `ViewportVoxelGrid` | Sparse voxel grid model | `OriginX/Y/Z`, `CellSizeX/Y/Z`, `ResolutionX/Y/Z`, `ActiveCellIndices`, `RegionIds`, `ScalarValues`, `VectorValues`, `Color`, `IsVisible`, `ShowEdges`, `IsClippable` |
| `ViewportVoxelIndexing` | Linear index helpers | `Encode(i, j, k, nx, ny)`, `TryDecode(...)` |
| `ViewportCamera` | Orbit/pan/zoom camera | `Target`, `Distance`, `Yaw`, `Pitch`, `UpAxis`, `Projection`, `SetStandardView(...)` |
| `ViewportSelection` | Multi-granularity selection | `SelectedParts`, `GetSelectedFaces`, `GetSelectedNodes`, `AddPart`/`Face`/`Node`, `Clear`, `BeginUpdate`/`EndUpdate`, `Changed` |
| `SectionPlane` | Clipping plane definition | `OriginX/Y/Z`, `NormalX/Y/Z` |
| `ViewportAnnotation` | 3D callout marker | `Mesh`, `NodeIndex`, `X/Y/Z`, `Text`, `Tag` |
| `ProbeResult` / `VoxelProbeResult` | Probe hit records | Hit coordinates, mesh/grid, node/cell indices, scalar value |
| `ViewportStatistics` | Build & frame telemetry | Mesh/triangle/edge/glyph counts, build timing |
| `Bounds3D` | Axis-aligned bounding box | Min/max coordinates, center, size, diagonal |
| `ViewportHoverInfo` | Hover pre-highlight payload | `Mesh`, `TriangleIndex`, `NodeIndex`, `IsPart` |
| Enums | Viewport options | `ViewportProjection`, `ViewportUpAxis`, `ViewportPickMode`, `ViewportStandardView`, `ViewportRubberBandSelectionMode` |
