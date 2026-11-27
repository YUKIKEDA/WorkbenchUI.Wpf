---
title: 拡張パッケージ API
description: WorkbenchUI.Wpf.Charts、WorkbenchUI.Wpf.Docking、WorkbenchUI.Wpf.Viewport3D の公開 API リファレンス。
sidebar_position: 4
---

# 拡張パッケージ リファレンス

各拡張パッケージは `WorkbenchUI.Wpf` に依存し、外部パッケージへの依存を隔離しつつ `https://schemas.workbenchui.dev/wpf` に公開型を集約しています。

---

## `WorkbenchUI.Wpf.Charts`

- **外部依存**: `ScottPlot.WPF` (`5.1.59`, MIT)
- **名前空間**: `WorkbenchUI.Wpf.Charts`

| 型 | 役割 | 主なメンバー |
| --- | --- | --- |
| `WorkbenchPlot` | `ScottPlot.WPF.WpfPlot` 派生 | `WheelZoomRequiresCtrl` (既定 `true`), `ThemeApplied`, `ThemeManager.ThemeChanged` 自動追従 |
| `WorkbenchChartTheme` | テーマ適用ヘルパー | `Apply(ScottPlot.Plot)`, `GetSeriesPalette()`, `ToScottPlotColor(Color)` |
| `ConvergenceMonitor` | 対数残差モニタ | `SeriesSource`, `Threshold`, `IsLogScale`, `XLabel`, `YLabel` |
| `ConvergenceSeries` | スレッドセーフ残差系列 | `Name`, `Color`, `Count`, `Append(double)`, `Clear()`, `Snapshot()`, `Changed` |
| `HistoryChart` | 複数系列 XY プロット | `SeriesSource`, `XLabel`, `YLabel`, `ShowLegend`, `ShowCrosshair` |
| `ChartSeries` | XY 系列モデル | `Name`, `X`, `Y`, `Color`, `LineWidth` |
| `FrequencyResponsePlot` | 2 パネル Bode 線図 | `SeriesSource`, `ShowPhase`, `MagnitudeInDecibels`, `XLabel` |
| `FrequencyResponseSeries` | 周波数応答系列モデル | `Name`, `Frequencies`, `Magnitudes`, `Phases`, `Color` |
| `HistogramChart` | ヒストグラム | `Values`, `BinCount`, `Normalize`, `XLabel` |

---

## `WorkbenchUI.Wpf.Docking`

- **外部依存**: `Dirkster.AvalonDock` & `Dirkster.AvalonDock.Themes.VS2013` (`4.74.1`, **Ms-PL**)
- **名前空間**: `WorkbenchUI.Wpf.Docking`

| 型 | 役割 | 主なメンバー |
| --- | --- | --- |
| `WorkbenchDockTheme` | AvalonDock `DictionaryTheme` | `Wb.Color.*` に動的バインドされた VS2013 テンプレートを適用 |
| `DockLayout` | レイアウト永続化ヘルパー | `Save(...)`, `SaveToString(...)`, `Load(...)`, `LoadFromString(...)` |

---

## `WorkbenchUI.Wpf.Viewport3D`

- **外部依存**: `Silk.NET.Direct3D11`, `Direct3D9`, `DXGI`, `Direct3D.Compilers` (`2.23.0`, MIT)
- **名前空間**: `WorkbenchUI.Wpf.Viewport3D`

### データモデル・補助型

| 型 | 役割 | 主なメンバー |
| --- | --- | --- |
| `WorkbenchViewport` | D3D11 3D ビューポート | 下表の [WorkbenchViewport メンバー一覧](#workbenchviewport-メンバー一覧) を参照 |
| `ViewportMesh` | 三角形サーフェスメッシュ | `Name`, `Positions`, `TriangleIndices`, `ScalarValues`, `Displacements`, `VectorValues`, `Color`, `IsVisible`, `ShowEdges`, `IsClippable`, `Opacity` |
| `ViewportVoxelGrid` | 疎ボクセル格子モデル | `OriginX/Y/Z`, `CellSizeX/Y/Z`, `ResolutionX/Y/Z`, `ActiveCellIndices`, `RegionIds`, `ScalarValues`, `VectorValues`, `Color`, `IsVisible`, `ShowEdges`, `IsClippable` |
| `ViewportVoxelIndexing` | 線形索引ヘルパー | `Encode(i, j, k, nx, ny)`, `TryDecode(...)` |
| `ViewportCamera` | オービット／パン／ズームカメラ | `Target`, `Distance`, `Yaw`, `Pitch`, `UpAxis`, `Projection`, `SetStandardView(...)` |
| `ViewportSelection` | 多粒度選択モデル | `SelectedParts`, `GetSelectedFaces`, `GetSelectedNodes`, `AddPart`/`Face`/`Node`, `Clear`, `BeginUpdate`/`EndUpdate`, `Changed` |
| `SectionPlane` | 断面カット平面定義 | `OriginX/Y/Z`, `NormalX/Y/Z` |
| `ViewportAnnotation` | 3D 引き出し線注釈 | `Mesh`, `NodeIndex`, `X/Y/Z`, `Text`, `Tag` |
| `ProbeResult` / `VoxelProbeResult` | プローブヒット情報 | ヒット座標、対象メッシュ/格子、節点/セル索引、スカラー値 |
| `ViewportStatistics` | 統計情報 | `TriangleCount`, `VertexCount`, `MeshCount`, `ChunkCount`, `EdgeSkippedMeshCount`, `LodTriangleCount`, `IsLodActive`, `LastDrawnChunkCount`, `LastGeometryBuildTime`, `LastRenderTime` |
| `Bounds3D` | 軸平行境界ボックス | `Empty`, `MinX`/`MinY`/`MinZ`, `MaxX`/`MaxY`/`MaxZ`, `IsEmpty`, `CenterX`/`CenterY`/`CenterZ`, `Radius`, `Union(...)` |
| `ViewportHoverInfo` | ホバープリハイライト情報 | `Mesh`, `TriangleIndex`, `NodeIndex`, `IsPart` |

### `WorkbenchViewport` メンバー一覧

| 分類 | メンバー |
| --- | --- |
| **データソース** | `MeshSource`, `VoxelSource`, `IsGeometryBuilding`, `GeometryBuildProgressChanged`, `GeometryBuildCompleted` |
| **コンター・表示** | `ColorScale`, `ShowContours`, `ShowEdges`, `Projection`, `UpAxis`, `ShowAxisTriad`, `ShowViewCube` |
| **変形・断面カット** | `DeformationScale`, `IsDeformationAnimated`, `DeformationAnimationPeriod`, `ShowUndeformedWireframe`, `SectionPlane`, `ShowSectionPlaneIndicator`, `GetSuggestedDeformationScale()` |
| **ベクトルグリフ** | `ShowGlyphs`, `GlyphScale`, `GlyphStride`, `GlyphColorScale`, `GetSuggestedGlyphScale()` |
| **選択・プローブ** | `PickMode`, `Selection`, `SelectionChanged`, `IsHoverHighlightEnabled`, `HoverChanged`, `RubberBandSelectionMode`, `ProbePicked`, `VoxelProbePicked`, `ProbeLabelFormatter`, `VoxelProbeLabelFormatter`, `Annotations` |
| **カメラ・操作** | `Camera`, `FitToView()`, `SetStandardView(...)`, `GetStatistics()`, `InvalidateViewport()`, `IsSoftwareRendering` |

### ビューポート列挙型

| 列挙型 | 用途 |
| --- | --- |
| `ViewportProjection` | 投影方式（`Perspective` / `Orthographic`） |
| `ViewportUpAxis` | 上方向軸（`ZUp` / `YUp`） |
| `ViewportPickMode` | ピック粒度（`None`, `Part`, `Face`, `Node`, `Probe`, `Cell`） |
| `ViewportStandardView` | 標準視点（`Front`, `Back`, `Left`, `Right`, `Top`, `Bottom`, `Isometric`） |
| `ViewportRubberBandSelectionMode` | 矩形選択判定（`VisibleOnly`, `Through`） |
