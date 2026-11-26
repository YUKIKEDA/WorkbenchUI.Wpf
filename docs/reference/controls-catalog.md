---
title: Controls Catalog
description: Complete reference of all custom controls, models, assistants, and styled standard controls in WorkbenchUI.Wpf.
sidebar_position: 2
---

# Controls Catalog (`WorkbenchUI.Wpf`)

All custom controls live in `WorkbenchUI.Wpf.Controls` (theming APIs live in `WorkbenchUI.Wpf.Theming`) and are exposed in XAML via `xmlns:wb="https://schemas.workbenchui.dev/wpf"`.

## Windowing, Shell & Overlays

| Type | Base Class | Key Members |
| --- | --- | --- |
| `WorkbenchWindow` | `Window` | Custom `WindowChrome` title bar, `TitleBarContent` slot for embedded menus/search |
| `WorkbenchDialogWindow` | `WorkbenchWindow` | Dialog base with standardized button bar conventions |
| `WorkbenchMessageBox` | `static` | `Show(...)` confirmation, warning, and error dialogs |
| `WorkbenchRibbon` | `TabControl` | `ApplicationLabel`, `ApplicationMenu`, `QuickAccessContent` |
| `WorkbenchRibbonTab` | `TabItem` | `Groups` (`ObservableCollection<WorkbenchRibbonGroup>`, content property) |
| `WorkbenchRibbonGroup` | `HeaderedItemsControl` | Labeled command group inside a ribbon tab |
| `InfoBar` | `Control` | `Severity` (`Info`/`Success`/`Warning`/`Error`), `Title`, `Message`, `IsOpen`, `IsClosable`, `ActionContent`, `Closed` |
| `ToastHost` | `Control` | `Show(message, level, duration)` explicit overlay host (`ToastLevel`, `ToastItem`) |
| `BusyOverlay` | `ContentControl` | `IsBusy`, `Message` — blocks input inside the wrapped region |

## Engineering & Numeric Inputs

| Type | Base Class | Key Members |
| --- | --- | --- |
| `NumericBox` | `Control` | `Value` (`double?`), `Minimum`, `Maximum`, `Increment`, `Unit`, `UnitProvider` (`IUnitProvider`), `Format`, `IsReadOnly` |
| `Vector3Box` | `Control` | `X`, `Y`, `Z` (`double?`), `Minimum`, `Maximum`, `Increment`, `Unit`, `UnitProvider`, `Format` |
| `MatrixBox` | `Control` | `Rows`, `Columns`, `Values` (`double[]?`), `IsSymmetric`, `IsReadOnly`, `UnitProvider`, `Format` |
| `RangeSlider` | `Control` | `Minimum`, `Maximum`, `LowerValue`, `UpperValue`, `Orientation`, draggable selected range bar |
| `PathBox` | `Control` | `Text`, `Mode` (`OpenFile`/`SaveFile`/`Folder`), `Filter`, `BrowseRequested` |
| `SearchBox` | `Control` | `Text`, `SearchText` (debounced), `SearchDelay`, `Placeholder` |
| `CheckComboBox` | `ItemsControl` | Multi-select dropdown (`CheckComboBoxItem`) |
| `KeyGestureBox` | `Control` | Captures keyboard shortcut `KeyGesture` |
| `ToggleSwitch` | `ToggleButton` | Compact `24 px`-compatible toggle switch |
| `ColorPicker` | `Control` | Swatch button opening `ColorEditor` popup (`SelectedColor`, `IsAlphaEnabled`) |
| `ColorEditor` | `Control` | Standalone HSV plane, hue/alpha sliders, hex/RGB inputs, swatches |
| `DropDownButton` | `Button` | `DropDownMenu` (`ContextMenu`) |
| `SplitButton` | `DropDownButton` | Left primary `Command` + right dropdown trigger |

## Structured Data, Trees, Logs & Color Maps

| Type | Kind | Key Members |
| --- | --- | --- |
| `PropertyGrid` | `Control` | `ItemsSource` (`PropertyItem`), `FilterText`, `ShowFilterBox`, `FilterPlaceholder` |
| `PropertyItem` | `abstract` | `Name`, `Category`, `Description`, `IsReadOnly`; subclasses: `TextPropertyItem`, `BoolPropertyItem`, `NumericPropertyItem`, `ChoicePropertyItem`, `PathPropertyItem`, `ColorPropertyItem`, `Vector3PropertyItem` |
| `ModelTree` | `Control` | `ItemsSource` (`IEnumerable<ITreeNode>`), `SelectionMode`, `ShowVisibilityToggles`, `FlatItems`, `ToggleVisibility`, `SetVisibility`, `GetSelectedNodes`, `BeginRename` |
| `ITreeNode` / `IRenamableNode` / `TreeNode` | Model / Contract | `Name`, `Children`, `IsExpanded`, `IsVisible` (`bool?`), `IsSelected`, `Icon` |
| `ColorMap` | `sealed` | `Jet`, `Viridis`, `Coolwarm`, `Grayscale`, `BuiltIn`, `GetColor(t)` |
| `ColorScale` | `sealed` | `ColorMap`, `Minimum`, `Maximum`, `IsLogarithmic`, `LevelCount`, `BelowRangeColor`, `AboveRangeColor`, `NaNColor`, `GetColor(value)` |
| `ColorMapLegend` | `Control` | `Scale`, `TickCount`, `LabelFormat`, `Title` |
| `ColorScaleEditor` | `Control` | `Scale` — interactive min/max, map, levels, and log-scale editor |
| `LogConsole` | `Control` | `Source` (`LogBuffer`), `FlushInterval`, `ShowTimestamps` |
| `LogBuffer` | `sealed` | Thread-safe `Append(LogLevel, string)` / `Append(LogEntry)`, `Clear()`, `Flush()`, `Capacity`, `Entries` |

## Workflow, Progress & Helpers

- **Workflow & Playback**: `PlaybackBar`, `ProcessRail` (`ProcessRailItem`, `IProcessRailStep`, `ProcessChapterState`), `ChapterHost` (`ChapterHostItem`), `Wizard` (`WizardStep`, `WizardNavigatingEventArgs`), `StepIndicator` (`StepIndicatorItem`, `StepState`).
- **Progress**: `ProgressDisplay` (bar + status + elapsed time + cancel `ICommand`), `ProgressRing`.
- **Icons**: `WorkbenchIcon` (`Icon`, `Size`), `WorkbenchIcons` (68 built-in `Geometry` properties + `Wb.Icon.*` resources).
- **Attached Assistants & Converters**: `ButtonAssist` (`Icon`, `IconSize`), `DataGridAssist` (`AutoApplyColumnStyles`), `ColorMapToBrushConverter`, `TreeIndentConverter`, `VisibilityToBooleanConverter`.

## Styled Standard WPF Controls

`WorkbenchTheme` provides consistent Dark and Light templates/styles for:
`Button`, `ToggleButton`, `RepeatButton`, `CheckBox`, `RadioButton`, `TextBox`, `PasswordBox`, `RichTextBox`, `ComboBox`, `ListBox`, `ListView` (+ `GridView`), `TreeView`, `DataGrid` (headers, cells, editing, validation error template, row details, grouping), `TabControl`, `Menu`, `ContextMenu`, `ToolBar`, `ToolBarTray`, `StatusBar`, `Expander`, `GroupBox`, `Slider`, `ProgressBar`, `ScrollBar`, `ToolTip`, `GridSplitter`, `Separator`, `Label`, `Hyperlink`.
