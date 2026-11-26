---
title: Build Your First Workbench App
description: Step-by-step tutorial to create a high-density WPF workbench application using WorkbenchUI.Wpf.
sidebar_position: 1
---

# Tutorial: Build Your First Workbench App

In this tutorial, you will build a minimal engineering workbench window with a dark theme, a hierarchical `ModelTree`, an inspector `PropertyGrid`, a `NumericBox`, and a `LogConsole`.

## 1. Create a WPF Project

Create a new .NET 10 WPF project and reference `WorkbenchUI.Wpf`:

```bash
dotnet new wpf -n MyWorkbenchApp -f net10.0-windows
cd MyWorkbenchApp
dotnet add package WorkbenchUI.Wpf
```

*(If you are working inside a source checkout of this repository, add a project reference to `src/WorkbenchUI.Wpf/WorkbenchUI.Wpf.csproj` instead.)*

## 2. Enable the Workbench Theme in `App.xaml`

Open `App.xaml` and add `WorkbenchTheme` to `Application.Resources`. One line merges all semantic color tokens, brushes, built-in icons, and control styles:

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

## 3. Build the Workbench Layout in `MainWindow.xaml`

Replace `Window` with `wb:WorkbenchWindow` to get a theme-aware custom window chrome. Then arrange a toolbar, a `ModelTree` on the left, a `PropertyGrid` in the center, and a `LogConsole` at the bottom:

```xml
<wb:WorkbenchWindow x:Class="MyWorkbenchApp.MainWindow"
                    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:wb="https://schemas.workbenchui.dev/wpf"
                    Title="My Engineering Workbench"
                    Width="1000" Height="640">
    <DockPanel>
        <!-- Top toolbar -->
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

        <!-- Bottom log console -->
        <wb:LogConsole x:Name="Console"
                       DockPanel.Dock="Bottom"
                       Height="150"
                       BorderThickness="0,1,0,0"
                       BorderBrush="{DynamicResource Wb.Brush.Border.Default}" />

        <!-- Main split workspace -->
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

## 4. Wire Up the Models in `MainWindow.xaml.cs`

Populate the `ModelTree` with `TreeNode` items, bind `LogBuffer` to `LogConsole`, and update `PropertyGrid` when the selection changes:

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

## 5. Run the Application

```bash
dotnet run
```

You now have a working dark/light workbench shell with a virtualized tree, typed property inspector, vector icons, and batched log output.

## Next Steps

- [How to customize themes and accent colors](../how-to/theming-and-accent.md)
- [How to implement unit conversion with `IUnitProvider`](../how-to/units-and-numeric-input.md)
- [Explore the controls catalog](../reference/controls-catalog.md)
