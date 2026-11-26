# WorkbenchUI.Wpf

A high-density, low-chroma WPF UI library for engineering, scientific, and technical desktop workbenches on .NET 10.

> **[日本語版 README (Japanese Overview)](README.ja.md)** | **[Architecture & Design Decisions](docs/design-decisions.md)**

---

## Key Features

- **Zero-dependency core (`WorkbenchUI.Wpf`)**: Built strictly on WPF framework assemblies—no third-party runtime dependencies.
- **High-density, neutral-gray aesthetic**: Compact 24 px control height, 4 px spacing grid, 2 px corner radius, and low-chroma Dark & Light themes designed so UI chrome never competes with color-mapped data or 3D viewports.
- **Runtime theming & dynamic accents**: Switch between `Dark` and `Light` variants or override the accent color at runtime via `ThemeManager` with automatic HSL hover/pressed/muted shade derivation.
- **Built-in vector icon set**: 60+ crisp 16×16 stroke-based `Geometry` icons via `WorkbenchIcon` and `WorkbenchIcons` (`Wb.Icon.*` resources).
- **Engineering & workbench controls**:
  - `NumericBox` (nullable `double?`, scientific notation, `IUnitProvider` hook), `Vector3Box`, `MatrixBox`, `RangeSlider`, `PathBox`, `SearchBox`, `CheckComboBox`, `KeyGestureBox`, `ToggleSwitch`, `ColorPicker` / `ColorEditor`, `DropDownButton` / `SplitButton`
  - `PropertyGrid` (explicit typed `PropertyItem` model, collapsible categories, search filter)
  - `ModelTree` (virtualized flat-projected `ListBox` tree with multi-select, tri-state visibility propagation, and inline rename)
  - `ColorMapLegend` & `ColorScaleEditor` (Jet, Viridis, Coolwarm, Grayscale, discrete levels, logarithmic scale)
  - `LogConsole` & `LogBuffer` (thread-safe ring buffer with batched UI updates)
  - `PlaybackBar`, `ProcessRail`, `ChapterHost`, `Wizard`, `StepIndicator`, `ProgressDisplay`, `ProgressRing`
  - `WorkbenchWindow`, `WorkbenchDialogWindow`, `WorkbenchMessageBox`, `WorkbenchRibbon`, `InfoBar`, `ToastHost`, `BusyOverlay`, and styled standard WPF controls (`DataGrid`, `TreeView`, `ListView`, `ToolBar`, `StatusBar`, etc.)
- **Optional extension packages** with external dependencies strictly isolated:
  - **`WorkbenchUI.Wpf.Charts`**: ScottPlot 5 theme adapter (`WorkbenchChartTheme`, `WorkbenchPlot`) and engineering composite plots (`ConvergenceMonitor`, `HistoryChart`, `FrequencyResponsePlot`, `HistogramChart`).
  - **`WorkbenchUI.Wpf.Docking`**: AvalonDock theme (`WorkbenchDockTheme`) synchronized with design tokens and `DockLayout` XML serialization helpers.
  - **`WorkbenchUI.Wpf.Viewport3D`**: Custom Direct3D 11 (`Silk.NET`) 3D viewport (`WorkbenchViewport`) supporting surface & voxel meshes, scalar contours, GPU vertex deformation, section planes, probing, annotations, vector glyphs, brick LOD, and D3DImage / WriteableBitmap fallback.

---

## Packages

| Package | Purpose | External Dependencies | License |
| --- | --- | --- | --- |
| `WorkbenchUI.Wpf` | Theme foundation, tokens, icons, core & workbench controls | None | MIT |
| `WorkbenchUI.Wpf.Charts` | ScottPlot 5 theme & engineering charts | `ScottPlot.WPF` (MIT) | MIT |
| `WorkbenchUI.Wpf.Docking` | AvalonDock theme & layout persistence | `Dirkster.AvalonDock` (Ms-PL) | MIT (code) / Ms-PL (AvalonDock) |
| `WorkbenchUI.Wpf.Viewport3D` | Direct3D 11 3D mesh & voxel viewport | `Silk.NET.Direct3D11` / `Direct3D9` / `DXGI` / `Compilers` (MIT) | MIT |

**Target Framework**: `.NET 10` (`net10.0-windows`)

---

## Quick Start

1. Reference `WorkbenchUI.Wpf` (and optionally `Charts`, `Docking`, or `Viewport3D`).
2. Add `WorkbenchTheme` to `App.xaml`:

```xml
<Application x:Class="MyApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:wb="https://schemas.workbenchui.dev/wpf">
    <Application.Resources>
        <wb:WorkbenchTheme Theme="Dark" />
    </Application.Resources>
</Application>
```

3. Use `xmlns:wb="https://schemas.workbenchui.dev/wpf"` in any XAML file—all four packages map onto this single URI:

```xml
<wb:WorkbenchWindow x:Class="MyApp.MainWindow"
                    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:wb="https://schemas.workbenchui.dev/wpf"
                    Title="Engineering Workbench" Width="1200" Height="800">
    <StackPanel Margin="12">
        <wb:NumericBox Value="1.5e-3" Unit="m" Width="160" />
        <Button Content="Run" Style="{StaticResource Wb.Button.Accent}" Margin="0,8,0,0" />
    </StackPanel>
</wb:WorkbenchWindow>
```

4. Switch theme or accent dynamically at runtime:

```csharp
using System.Windows.Media;
using WorkbenchUI.Wpf.Theming;

ThemeManager.SetTheme(WorkbenchThemeVariant.Light);
ThemeManager.SetAccent(Color.FromRgb(0x2E, 0xA0, 0x43));
```

---

## Building from Source

```bash
dotnet build WorkbenchUI.Wpf.slnx -c Release
dotnet test WorkbenchUI.Wpf.slnx -c Release
dotnet run --project samples/WorkbenchUI.Wpf.Gallery
```

---

## Documentation

- **User Documentation Site (English / 日本語)**: [https://yukikeda.github.io/WorkbenchUI.Wpf/](https://yukikeda.github.io/WorkbenchUI.Wpf/)
  - [Introduction](docs/intro.md) · [Tutorial: First Workbench App](docs/tutorials/first-workbench-app.md) · [How-to Guides](docs/how-to/theming-and-accent.md) · [Reference](docs/reference/controls-catalog.md) · [Explanation](docs/explanation/theme-architecture.md)
- [Architecture & Design Decisions (ADRs)](docs/design-decisions.md)
- [Core Package (`WorkbenchUI.Wpf`)](src/WorkbenchUI.Wpf/README.md)
- [Charts (`WorkbenchUI.Wpf.Charts`)](src/WorkbenchUI.Wpf.Charts/README.md)
- [Docking (`WorkbenchUI.Wpf.Docking`)](src/WorkbenchUI.Wpf.Docking/README.md)
- [Viewport3D (`WorkbenchUI.Wpf.Viewport3D`)](src/WorkbenchUI.Wpf.Viewport3D/README.md)

## License

This project is licensed under the [MIT License](LICENSE). Note that `WorkbenchUI.Wpf.Docking` depends on `Dirkster.AvalonDock`, which is distributed under the Microsoft Public License (Ms-PL).
