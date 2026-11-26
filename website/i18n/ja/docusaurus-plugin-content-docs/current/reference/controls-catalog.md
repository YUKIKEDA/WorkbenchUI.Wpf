---
title: コントロールカタログ
description: WorkbenchUI.Wpf に収録されたカスタムコントロール、モデル、添付ヘルパー、標準コントロールスタイルの一覧。
sidebar_position: 2
---

# コントロールカタログ (`WorkbenchUI.Wpf`)

すべてのカスタムコントロールは `WorkbenchUI.Wpf.Controls`（テーマ API は `WorkbenchUI.Wpf.Theming`）に属し、XAML では `xmlns:wb="https://schemas.workbenchui.dev/wpf"` から参照できます。

## ウィンドウ・シェル・オーバーレイ

| 型 | 基底クラス | 主なメンバー |
| --- | --- | --- |
| `WorkbenchWindow` | `Window` | カスタム `WindowChrome` タイトルバー、メニューや検索欄を埋め込める `TitleBarContent` |
| `WorkbenchDialogWindow` | `WorkbenchWindow` | ダイアログ基盤（ボタンバー規約内蔵） |
| `WorkbenchMessageBox` | `static` | 確認・警告・エラーの定型ダイアログ (`Show(...)`) |
| `WorkbenchRibbon` | `TabControl` | `ApplicationLabel`, `ApplicationMenu`, `QuickAccessContent` |
| `WorkbenchRibbonTab` | `TabItem` | `Groups` (`ObservableCollection<WorkbenchRibbonGroup>`, コンテンツプロパティ) |
| `WorkbenchRibbonGroup` | `HeaderedItemsControl` | リボンタブ内のコマンドグループ |
| `InfoBar` | `Control` | `Severity` (`Info`/`Success`/`Warning`/`Error`), `Title`, `Message`, `IsOpen`, `IsClosable`, `ActionContent`, `Closed` |
| `ToastHost` | `Control` | 明示配置型トースト通知ホスト (`Show(message, level, duration)`) |
| `BusyOverlay` | `ContentControl` | `IsBusy`, `Message` — 囲んだ領域の入力を遮断 |

## 数値・エンジニアリング入力

| 型 | 基底クラス | 主なメンバー |
| --- | --- | --- |
| `NumericBox` | `Control` | `Value` (`double?`), `Minimum`, `Maximum`, `Increment`, `Unit`, `UnitProvider` (`IUnitProvider`), `Format`, `IsReadOnly` |
| `Vector3Box` | `Control` | `X`, `Y`, `Z` (`double?`), `Minimum`, `Maximum`, `Increment`, `Unit`, `UnitProvider`, `Format` |
| `MatrixBox` | `Control` | `Rows`, `Columns`, `Values` (`double[]?`), `IsSymmetric`, `IsReadOnly`, `UnitProvider`, `Format` |
| `RangeSlider` | `Control` | `Minimum`, `Maximum`, `LowerValue`, `UpperValue`, `Orientation`, 中央範囲バーのドラッグ移動 |
| `PathBox` | `Control` | `Text`, `Mode` (`OpenFile`/`SaveFile`/`Folder`), `Filter`, `BrowseRequested` |
| `SearchBox` | `Control` | `Text`, `SearchText` (デバウンス確定), `SearchDelay`, `Placeholder` |
| `CheckComboBox` | `ItemsControl` | 複数選択コンボボックス (`CheckComboBoxItem`) |
| `KeyGestureBox` | `Control` | キーボードショートカット `KeyGesture` 入力欄 |
| `ToggleSwitch` | `ToggleButton` | 24 px 枠に収まる高密度トグルスイッチ |
| `ColorPicker` | `Control` | `ColorEditor` をポップアップで開くスウォッチボタン (`SelectedColor`, `IsAlphaEnabled`) |
| `ColorEditor` | `Control` | HSV 面・色相/アルファスライダー・Hex/RGB 入力 |
| `DropDownButton` | `Button` | `DropDownMenu` (`ContextMenu`) |
| `SplitButton` | `DropDownButton` | 左側 `Command` 実行 + 右側ドロップダウン |

## 構造化データ・ツリー・ログ・カラーマップ

| 型 | 種別 | 主なメンバー |
| --- | --- | --- |
| `PropertyGrid` | `Control` | `ItemsSource` (`PropertyItem`), `FilterText`, `ShowFilterBox`, `FilterPlaceholder` |
| `PropertyItem` | `abstract` | `Name`, `Category`, `Description`, `IsReadOnly`; 派生: `TextPropertyItem`, `BoolPropertyItem`, `NumericPropertyItem`, `ChoicePropertyItem`, `PathPropertyItem`, `ColorPropertyItem`, `Vector3PropertyItem` |
| `ModelTree` | `Control` | `ItemsSource` (`IEnumerable<ITreeNode>`), `SelectionMode`, `ShowVisibilityToggles`, `FlatItems`, `ToggleVisibility`, `SetVisibility`, `GetSelectedNodes`, `BeginRename` |
| `ITreeNode` / `IRenamableNode` / `TreeNode` | モデル / 契約 | `Name`, `Children`, `IsExpanded`, `IsVisible` (`bool?`), `IsSelected`, `Icon` |
| `ColorMap` | `sealed` | `Jet`, `Viridis`, `Coolwarm`, `Grayscale`, `BuiltIn`, `GetColor(t)` |
| `ColorScale` | `sealed` | `ColorMap`, `Minimum`, `Maximum`, `IsLogarithmic`, `LevelCount`, `BelowRangeColor`, `AboveRangeColor`, `NaNColor`, `GetColor(value)` |
| `ColorMapLegend` | `Control` | `Scale`, `TickCount`, `LabelFormat`, `Title` |
| `ColorScaleEditor` | `Control` | 最小/最大、マップ、離散分割、対数スケールの対話エディタ |
| `LogConsole` | `Control` | `Source` (`LogBuffer`), `FlushInterval`, `ShowTimestamps` |
| `LogBuffer` | `sealed` | スレッドセーフな `Append(LogLevel, string)` / `Append(LogEntry)`, `Clear()`, `Flush()`, `Capacity`, `Entries` |

## 工程ワークフロー・進捗・アイコン・ヘルパー

| 型 | 種別 | 主なメンバー・用途 |
| --- | --- | --- |
| `PlaybackBar` | `Control` | フレーム再生コントロール（再生/一時停止、ステップ、速度、ループ、スライダー） |
| `ProcessRail` | `Selector` | 工程レール (`ProcessRailItem`, `IProcessRailStep`, `ProcessChapterState`) |
| `ChapterHost` | `ItemsControl` | アクティブな工程章を表示するホスト (`ChapterHostItem`) |
| `Wizard` | `ItemsControl` | マルチステップウィザード (`WizardStep`, `WizardNavigatingEventArgs`) |
| `StepIndicator` | `ItemsControl` | ステップ進捗インジケータ (`StepIndicatorItem`, `StepState`) |
| `ProgressDisplay` | `Control` | 単一ジョブの進捗バー + ステータス + 経過時間 + キャンセル `ICommand` |
| `ProgressRing` | `Control` | 不確定モードの円形スピナー |
| `WorkbenchIcon` | `Control` | ベクタ `Geometry` (`Icon`, `Size`) を `Viewbox` でスケーリング描画 |
| `WorkbenchIcons` | `static` | 67 種の Frozen 済み `16×16` `Geometry` と `Wb.Icon.*` リソース |
| `ButtonAssist` | `static` | ボタンにアイコンを付与する添付プロパティ (`Icon`, `IconSize`) |
| `DataGridAssist` | `static` | `DataGrid` 列スタイルの自動適用 (`AutoApplyColumnStyles`) |
| `ColorMapToBrushConverter` | `IValueConverter` | `ColorMap` を水平 `LinearGradientBrush` へ変換 |
| `TreeIndentConverter` | `IValueConverter` | ツリー階層の深さから左インデント `Thickness` を算出 |
| `VisibilityToBooleanConverter` | `IValueConverter` | `Visibility` と `bool` の双方向コンバータ |

## スタイル提供済みの WPF 標準コントロール

`WorkbenchTheme` には以下の標準コントロールの Dark / Light スタイルが含まれます:

| カテゴリ | コントロール |
| --- | --- |
| **ボタン系** | `Button`, `ToggleButton`, `RepeatButton`, `CheckBox`, `RadioButton` |
| **テキスト入力** | `TextBox`, `PasswordBox`, `RichTextBox` |
| **選択・リスト** | `ComboBox`, `ListBox`, `ListView`（`GridView` 含む）, `TreeView` |
| **表・データ** | `DataGrid`（列/行ヘッダー、セル編集、検証エラーツールチップ、行詳細、グループ化） |
| **ナビゲーション・メニュー** | `TabControl`, `Menu`, `ContextMenu`, `ToolBar`, `ToolBarTray`, `StatusBar` |
| **レイアウト・コンテナ** | `Expander`, `GroupBox`, `GridSplitter`, `Separator` |
| **フィードバック・補助** | `Slider`, `ProgressBar`, `ScrollBar`, `ToolTip`, `Label`, `Hyperlink` |
