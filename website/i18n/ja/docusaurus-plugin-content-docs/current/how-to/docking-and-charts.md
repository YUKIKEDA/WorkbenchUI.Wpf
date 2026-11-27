---
title: Docking と Charts の統合
description: AvalonDock への WorkbenchDockTheme 適用、レイアウト永続化、ScottPlot ベースチャートの使い方。
sidebar_position: 5
---

# Docking と Charts を統合する

## ドッキング (`WorkbenchUI.Wpf.Docking`)

`WorkbenchUI.Wpf.Docking` は `Dirkster.AvalonDock` を `Wb.Color.*` トークンでテーマ化し、`XmlLayoutSerializer` のラッパーを提供します。

> **サードパーティライセンス注記**: `Dirkster.AvalonDock` は Microsoft Public License (**Ms-PL**) の下で配布されています。

### `WorkbenchDockTheme` の適用

```xml
<Window xmlns:wb="https://schemas.workbenchui.dev/wpf"
        xmlns:ad="clr-namespace:AvalonDock;assembly=AvalonDock"
        xmlns:adl="clr-namespace:AvalonDock.Layout;assembly=AvalonDock">
    <ad:DockingManager x:Name="DockManager">
        <ad:DockingManager.Theme>
            <wb:WorkbenchDockTheme />
        </ad:DockingManager.Theme>
        <adl:LayoutRoot>
            <adl:LayoutPanel Orientation="Horizontal">
                <adl:LayoutAnchorablePane DockWidth="260">
                    <adl:LayoutAnchorable ContentId="ModelTreePane" Title="Model Tree">
                        <wb:ModelTree x:Name="Tree" />
                    </adl:LayoutAnchorable>
                </adl:LayoutAnchorablePane>
                <adl:LayoutDocumentPane />
            </adl:LayoutPanel>
        </adl:LayoutRoot>
    </ad:DockingManager>
</Window>
```

`WorkbenchDockTheme` は `Wb.Color.*` を `DynamicResource` で参照しているため、Dark / Light 切替や `ThemeManager.SetAccent` がタブ・キャプション・フローティングウィンドウへ即座に反映されます。

### レイアウトの保存と復元

各ペインに一意な `ContentId` を割り当て、`DockLayout` で保存・復元します:

```csharp
using WorkbenchUI.Wpf.Docking;

// ファイルへ保存（親ディレクトリは自動作成）
DockLayout.Save(DockManager, layoutFilePath);

// ファイルから復元（null を返した ContentId はレイアウトから除外される）
bool loaded = DockLayout.Load(DockManager, layoutFilePath, contentId => contentId switch
{
    "ModelTreePane" => _treeView,
    "InspectorPane" => _inspectorView,
    _ => null,
});
```

設定ファイルへ埋め込む用途向けに `DockLayout.SaveToString` と `DockLayout.LoadFromString` も用意されています。

---

## チャート (`WorkbenchUI.Wpf.Charts`)

`WorkbenchUI.Wpf.Charts` は ScottPlot 5 を土台に、`WorkbenchPlot`、`WorkbenchChartTheme`、および 4 種類の技術計算向け複合チャートを提供します。

### リアルタイム残差モニタ (`ConvergenceMonitor`)

`ConvergenceSeries` はスレッドセーフであり、ソルバーのワーカースレッドから直接 `Append` / `Clear` を呼べます:

```xml
<wb:ConvergenceMonitor x:Name="Residuals"
                       IsLogScale="True"
                       Threshold="1e-6"
                       XLabel="Iteration"
                       YLabel="Residual" />
```

```csharp
var series = new ConvergenceSeries("Momentum");
Residuals.SeriesSource = new[] { series };

// 計算スレッド内から:
series.Append(1.2e-3);
```

### 履歴・周波数応答・ヒストグラム

- **`HistoryChart`**: `IEnumerable<ChartSeries>` (`X`, `Y`, `Name`, `Color`, `LineWidth`) を表示。`ShowLegend` と `ShowCrosshair` をサポート。
- **`FrequencyResponsePlot`**: `IEnumerable<FrequencyResponseSeries>` (`Frequencies`, `Magnitudes`, 任意の `Phases`) を対数周波数軸で Bode 表示（`ShowPhase`, `MagnitudeInDecibels`）。
- **`HistogramChart`**: `Values` (`IEnumerable?`), `BinCount`, `Normalize` による分布表示。

### カスタム ScottPlot チャート (`WorkbenchPlot`)

`ScottPlot.Plot.Add.*` を直接使いたい場合は `wb:WorkbenchPlot` を配置します。`WorkbenchChartTheme.Apply` が適用済みで `ThemeManager.ThemeChanged` に自動追従し、既定では `Ctrl+ホイール` でズームするため（`WheelZoomRequiresCtrl="True"`）、`ScrollViewer` 内のページスクロールと競合しません:

```xml
<wb:WorkbenchPlot x:Name="CustomPlot" WheelZoomRequiresCtrl="True" />
```
