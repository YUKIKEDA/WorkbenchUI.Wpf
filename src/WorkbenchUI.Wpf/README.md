# WorkbenchUI.Wpf

Zero-dependency theme foundation, semantic design tokens, built-in vector icons, and high-density engineering workbench controls for WPF on .NET 10.

## Highlights

- **Theme Foundation**: `WorkbenchTheme` (`Dark` / `Light`) and `ThemeManager` (`SetTheme`, `SetAccent`, `ResetAccent`, `GetSystemTheme`, `ThemeChanged`)
- **Windowing & Shell**: `WorkbenchWindow`, `WorkbenchDialogWindow`, `WorkbenchMessageBox`, `WorkbenchRibbon`, `InfoBar`, `ToastHost`, `BusyOverlay`
- **Engineering Inputs**: `NumericBox`, `Vector3Box`, `MatrixBox`, `RangeSlider`, `PathBox`, `SearchBox`, `CheckComboBox`, `KeyGestureBox`, `ToggleSwitch`, `ColorPicker`, `ColorEditor`, `DropDownButton`, `SplitButton`
- **Data & Post-Processing**: `PropertyGrid`, `ModelTree`, `ColorMapLegend`, `ColorScaleEditor`, `LogConsole` / `LogBuffer`, `PlaybackBar`, `ProcessRail`, `ChapterHost`, `Wizard`, `ProgressDisplay`, `ProgressRing`
- **Styled Standard Controls**: `Button`, `TextBox`, `PasswordBox`, `RichTextBox`, `ComboBox`, `CheckBox`, `RadioButton`, `ListBox`, `ListView` (`GridView`), `TreeView`, `DataGrid`, `TabControl`, `Menu`, `ToolBar`, `StatusBar`, `Expander`, `GroupBox`, `Slider`, `ProgressBar`, `ScrollBar`, `ToolTip`, `GridSplitter`

## Usage

```xml
<Application.Resources>
    <wb:WorkbenchTheme Theme="Dark" />
</Application.Resources>
```

Use `xmlns:wb="https://schemas.workbenchui.dev/wpf"` in your XAML files.
