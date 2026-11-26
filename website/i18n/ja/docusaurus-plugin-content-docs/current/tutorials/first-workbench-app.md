---
title: 最初の Workbench アプリを作る
description: WorkbenchUI.Wpf を使って高密度 WPF ワークベンチアプリケーションを作成するステップバイステップ・チュートリアル。
sidebar_position: 1
---

# チュートリアル: 最初の Workbench アプリを作る

このチュートリアルでは、ダークテーマ、`ModelTree`、型付き `PropertyGrid`、`NumericBox`、`LogConsole` を備えた最小構成のエンジニアリング Workbench ウィンドウを作成します。

## 1. WPF プロジェクトの作成

.NET 10 の WPF プロジェクトを作成し、`WorkbenchUI.Wpf` を参照します:

```bash
dotnet new wpf -n MyWorkbenchApp -f net10.0-windows
cd MyWorkbenchApp
dotnet add package WorkbenchUI.Wpf
```

*（このリポジトリを手元でチェックアウトして試す場合は、代わりに `src/WorkbenchUI.Wpf/WorkbenchUI.Wpf.csproj` へのプロジェクト参照を追加してください。）*

## 2. `App.xaml` でテーマを有効にする

`App.xaml` の `Application.Resources` に `WorkbenchTheme` を 1 行追加すると、セマンティックカラートークン、ブラシ、内蔵アイコン、コントロールスタイルが一括でマージされます:

```xml
<Application x:Class="MyWorkbenchApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:wb="https://schemas.workbenchui.dev/wpf"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <wb:WorkbenchTheme Theme="Dark" />
    </Application.Resources>
</Application>
```

## 3. `MainWindow.xaml` でワークベンチ画面を組む

ルートの `Window` を `wb:WorkbenchWindow` に置き換えてテーマ連動ウィンドウクロームを有効にし、ツールバー・`ModelTree`・`PropertyGrid`・`LogConsole` を配置します:

```xml
<wb:WorkbenchWindow x:Class="MyWorkbenchApp.MainWindow"
                    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:wb="https://schemas.workbenchui.dev/wpf"
                    Title="My Engineering Workbench"
                    Width="1000" Height="640">
    <DockPanel>
        <!-- 上部ツールバー -->
        <ToolBarTray DockPanel.Dock="Top">
            <ToolBar>
                <Button Content="Run"
                        Style="{StaticResource Wb.Button.Accent}"
                        wb:ButtonAssist.Icon="{x:Static wb:WorkbenchIcons.Play}"
                        Click="OnRunClicked" />
                <Separator />
                <ToggleButton Content="Light Theme"
                              Checked="OnLightChecked"
                              Unchecked="OnLightUnchecked" />
            </ToolBar>
        </ToolBarTray>

        <!-- 下部ログコンソール -->
        <wb:LogConsole x:Name="Console"
                       DockPanel.Dock="Bottom"
                       Height="150"
                       BorderThickness="0,1,0,0"
                       BorderBrush="{DynamicResource Wb.Brush.Border.Default}" />

        <!-- メイン分割エリア -->
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="260" />
                <ColumnDefinition Width="4" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>

            <wb:ModelTree x:Name="Tree"
                          Grid.Column="0"
                          SelectionChanged="OnTreeSelectionChanged" />

            <GridSplitter Grid.Column="1" HorizontalAlignment="Stretch" />

            <wb:PropertyGrid x:Name="Inspector"
                             Grid.Column="2" />
        </Grid>
    </DockPanel>
</wb:WorkbenchWindow>
```

## 4. `MainWindow.xaml.cs` でモデルを接続する

`TreeNode` で階層データを構築し、`LogBuffer` を `LogConsole` に接続して、ツリー選択変更時に `PropertyGrid` を更新します:

```csharp
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Theming;

namespace MyWorkbenchApp;

public partial class MainWindow : WorkbenchWindow
{
    private readonly LogBuffer _log = new();
    private readonly ObservableCollection<ITreeNode> _roots = [];

    public MainWindow()
    {
        InitializeComponent();

        Console.Source = _log;
        _log.Append(LogLevel.Info, "Workbench initialized.");

        var assembly = new TreeNode { Name = "Assembly", IsExpanded = true, Icon = WorkbenchIcons.Layers };
        assembly.Children.Add(new TreeNode { Name = "Housing", Icon = WorkbenchIcons.Cube });
        assembly.Children.Add(new TreeNode { Name = "Mesh", Icon = WorkbenchIcons.Mesh });
        _roots.Add(assembly);

        Tree.ItemsSource = _roots;
    }

    private void OnTreeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = Tree.GetSelectedNodes().FirstOrDefault();
        if (selected is null)
        {
            Inspector.ItemsSource = null;
            return;
        }

        Inspector.ItemsSource = new PropertyItem[]
        {
            new TextPropertyItem { Category = "General", Name = "Name", Value = selected.Name },
            new BoolPropertyItem { Category = "Display", Name = "Visible", Value = selected.IsVisible ?? true },
            new NumericPropertyItem
            {
                Category = "Mesh",
                Name = "Element Size",
                Value = 0.005,
                Minimum = 1e-4,
                Maximum = 1.0,
                Unit = "m",
                Format = "G4",
            },
        };
    }

    private void OnRunClicked(object sender, RoutedEventArgs e)
    {
        _log.Append(LogLevel.Info, "Simulation started...");
    }

    private void OnLightChecked(object sender, RoutedEventArgs e) =>
        ThemeManager.SetTheme(WorkbenchThemeVariant.Light);

    private void OnLightUnchecked(object sender, RoutedEventArgs e) =>
        ThemeManager.SetTheme(WorkbenchThemeVariant.Dark);
}
```

## 5. アプリを実行する

```bash
dotnet run
```

これで、仮想化ツリー、型付きプロパティインスペクタ、ベクタアイコン、バッチ更新ログ出力、Dark/Light テーマ切替を備えた高密度ワークベンチが動作します。

## 次のステップ

- [テーマとアクセントカラーを変更する](../how-to/theming-and-accent.md)
- [`IUnitProvider` で単位換算を実装する](../how-to/units-and-numeric-input.md)
- [コントロールカタログを見る](../reference/controls-catalog.md)
