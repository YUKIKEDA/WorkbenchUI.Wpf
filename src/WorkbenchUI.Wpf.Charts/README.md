# WorkbenchUI.Wpf.Charts

ScottPlot 5 integration for `WorkbenchUI.Wpf`, providing theme synchronization and engineering composite chart controls.

## Included Components

- `WorkbenchPlot`: `ScottPlot.WPF.WpfPlot` subclass with `WorkbenchChartTheme` applied and automatic `ThemeManager.ThemeChanged` synchronization (Ctrl+Wheel zoom by default to play cleanly inside `ScrollViewer`).
- `WorkbenchChartTheme`: Applies `Wb.Color.*` design tokens and Dark/Light-aware series palettes to any `ScottPlot.Plot`.
- `ConvergenceMonitor`: Real-time solver residual monitor on a log10 vertical scale using thread-safe `ConvergenceSeries`.
- `HistoryChart`: Multi-series XY time/parameter history plot (`ChartSeries`).
- `FrequencyResponsePlot`: Dual-panel Bode magnitude (dB) and phase (deg) plot on a logarithmic frequency axis (`FrequencyResponseSeries`).
- `HistogramChart`: Distribution histogram with configurable bin count.

## License

MIT License. Depends on `ScottPlot.WPF` (MIT).
