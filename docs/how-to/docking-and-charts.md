---
title: Integrate Docking & Charts
description: How to style AvalonDock with WorkbenchDockTheme, persist layouts, and use ScottPlot-backed engineering charts.
sidebar_position: 5
---

# How to Integrate Docking & Charts

## Docking (`WorkbenchUI.Wpf.Docking`)

`WorkbenchUI.Wpf.Docking` themes `Dirkster.AvalonDock` with `Wb.Color.*` tokens and wraps `XmlLayoutSerializer`.

> **Third-Party License Note**: `Dirkster.AvalonDock` is licensed under the Microsoft Public License (**Ms-PL**).

### Apply `WorkbenchDockTheme`

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

Because `WorkbenchDockTheme` references `Wb.Color.*` via `DynamicResource`, switching between `Dark` and `Light` or calling `ThemeManager.SetAccent` updates docking captions, tabs, and floating windows automatically.

### Save and Restore Layouts

Assign a unique `ContentId` to every pane, then save and restore via `DockLayout`:

```csharp
using WorkbenchUI.Wpf.Docking;

// Save to file (creates parent directories automatically)
DockLayout.Save(DockManager, layoutFilePath);

// Restore from file (return null to drop obsolete panes)
bool loaded = DockLayout.Load(DockManager, layoutFilePath, contentId => contentId switch
{
    "ModelTreePane" => _treeView,
    "InspectorPane" => _inspectorView,
    _ => null,
});
```

`DockLayout.SaveToString` and `DockLayout.LoadFromString` are also available for storing layouts inside user settings.

---

## Charts (`WorkbenchUI.Wpf.Charts`)

`WorkbenchUI.Wpf.Charts` provides `WorkbenchPlot`, `WorkbenchChartTheme`, and four engineering plot controls built on ScottPlot 5.

### Real-Time Residual Monitoring (`ConvergenceMonitor`)

`ConvergenceSeries` is thread-safe (`Append` and `Clear` can be called from solver worker threads):

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

// From a background solver loop:
series.Append(1.2e-3);
```

### Multi-Series History, Frequency Response, and Histograms

- **`HistoryChart`**: Bind `SeriesSource` to `IEnumerable<ChartSeries>` (`X`, `Y`, `Name`, `Color`, `LineWidth`). Supports `ShowLegend` and `ShowCrosshair`.
- **`FrequencyResponsePlot`**: Bind `SeriesSource` to `IEnumerable<FrequencyResponseSeries>` (`Frequencies`, `Magnitudes`, optional `Phases`) with `ShowPhase` and `MagnitudeInDecibels`.
- **`HistogramChart`**: Bind `Values` (`double[]?`), `BinCount`, and `Normalize`.

### Custom ScottPlot Charts (`WorkbenchPlot`)

For full access to `ScottPlot.Plot.Add.*`, place `wb:WorkbenchPlot` in XAML. It applies `WorkbenchChartTheme.Apply` automatically, tracks `ThemeManager.ThemeChanged`, and requires `Ctrl+Wheel` for zooming (`WheelZoomRequiresCtrl="True"`) so plain wheel scrolling passes through to outer `ScrollViewer` containers:

```xml
<wb:WorkbenchPlot x:Name="CustomPlot" WheelZoomRequiresCtrl="True" />
```
