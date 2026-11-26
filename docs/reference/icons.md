---
title: Built-in Icons
description: Complete list of all 67 built-in 16x16 vector Geometry icons in WorkbenchIcons.
sidebar_position: 3
---

# Built-in Icons (`WorkbenchIcons`)

`WorkbenchIcons` provides **67** stroke-based `Geometry` icons drawn on a `16×16` grid (`WorkbenchIcons.CanvasSize`). Every icon is frozen for cross-thread safety and available both as a static property (`WorkbenchIcons.<Name>`) and as a resource key (`Wb.Icon.<Name>`).

## Usage

```xml
<!-- With WorkbenchIcon -->
<wb:WorkbenchIcon Icon="{x:Static wb:WorkbenchIcons.Save}" Size="16" />

<!-- With ButtonAssist on a Button -->
<Button Content="Run" wb:ButtonAssist.Icon="{x:Static wb:WorkbenchIcons.Play}" />

<!-- Icon-only toolbar button via resource key -->
<Button Style="{StaticResource Wb.Button.Icon}"
        wb:ButtonAssist.Icon="{StaticResource Wb.Icon.ZoomToFit}"
        ToolTip="Fit to View" />
```

## File & Project

| Name | Static Property | Resource Key |
| --- | --- | --- |
| `File` | `WorkbenchIcons.File` | `Wb.Icon.File` |
| `NewFile` | `WorkbenchIcons.NewFile` | `Wb.Icon.NewFile` |
| `Folder` | `WorkbenchIcons.Folder` | `Wb.Icon.Folder` |
| `FolderOpen` | `WorkbenchIcons.FolderOpen` | `Wb.Icon.FolderOpen` |
| `Save` | `WorkbenchIcons.Save` | `Wb.Icon.Save` |
| `SaveAs` | `WorkbenchIcons.SaveAs` | `Wb.Icon.SaveAs` |
| `History` | `WorkbenchIcons.History` | `Wb.Icon.History` |
| `Settings` | `WorkbenchIcons.Settings` | `Wb.Icon.Settings` |
| `Download` | `WorkbenchIcons.Download` | `Wb.Icon.Download` |
| `Upload` | `WorkbenchIcons.Upload` | `Wb.Icon.Upload` |
| `Close` | `WorkbenchIcons.Close` | `Wb.Icon.Close` |

## Editing

| Name | Static Property | Resource Key |
| --- | --- | --- |
| `Undo` | `WorkbenchIcons.Undo` | `Wb.Icon.Undo` |
| `Redo` | `WorkbenchIcons.Redo` | `Wb.Icon.Redo` |
| `Delete` | `WorkbenchIcons.Delete` | `Wb.Icon.Delete` |
| `Add` | `WorkbenchIcons.Add` | `Wb.Icon.Add` |
| `Remove` | `WorkbenchIcons.Remove` | `Wb.Icon.Remove` |
| `Copy` | `WorkbenchIcons.Copy` | `Wb.Icon.Copy` |
| `Edit` | `WorkbenchIcons.Edit` | `Wb.Icon.Edit` |
| `Search` | `WorkbenchIcons.Search` | `Wb.Icon.Search` |
| `Filter` | `WorkbenchIcons.Filter` | `Wb.Icon.Filter` |
| `Refresh` | `WorkbenchIcons.Refresh` | `Wb.Icon.Refresh` |

## View

| Name | Static Property | Resource Key |
| --- | --- | --- |
| `ZoomIn` | `WorkbenchIcons.ZoomIn` | `Wb.Icon.ZoomIn` |
| `ZoomOut` | `WorkbenchIcons.ZoomOut` | `Wb.Icon.ZoomOut` |
| `ZoomToFit` | `WorkbenchIcons.ZoomToFit` | `Wb.Icon.ZoomToFit` |
| `Eye` | `WorkbenchIcons.Eye` | `Wb.Icon.Eye` |
| `EyeOff` | `WorkbenchIcons.EyeOff` | `Wb.Icon.EyeOff` |
| `Layout` | `WorkbenchIcons.Layout` | `Wb.Icon.Layout` |
| `Sun` | `WorkbenchIcons.Sun` | `Wb.Icon.Sun` |
| `Moon` | `WorkbenchIcons.Moon` | `Wb.Icon.Moon` |
| `Grid` | `WorkbenchIcons.Grid` | `Wb.Icon.Grid` |
| `List` | `WorkbenchIcons.List` | `Wb.Icon.List` |
| `Home` | `WorkbenchIcons.Home` | `Wb.Icon.Home` |

## Playback

| Name | Static Property | Resource Key |
| --- | --- | --- |
| `Play` | `WorkbenchIcons.Play` | `Wb.Icon.Play` |
| `Pause` | `WorkbenchIcons.Pause` | `Wb.Icon.Pause` |
| `Stop` | `WorkbenchIcons.Stop` | `Wb.Icon.Stop` |

## Engineering / CAE / 3D

| Name | Static Property | Resource Key |
| --- | --- | --- |
| `Mesh` | `WorkbenchIcons.Mesh` | `Wb.Icon.Mesh` |
| `Cube` | `WorkbenchIcons.Cube` | `Wb.Icon.Cube` |
| `Section` | `WorkbenchIcons.Section` | `Wb.Icon.Section` |
| `Probe` | `WorkbenchIcons.Probe` | `Wb.Icon.Probe` |
| `Annotation` | `WorkbenchIcons.Annotation` | `Wb.Icon.Annotation` |
| `Vector` | `WorkbenchIcons.Vector` | `Wb.Icon.Vector` |
| `Contour` | `WorkbenchIcons.Contour` | `Wb.Icon.Contour` |
| `ChartLine` | `WorkbenchIcons.ChartLine` | `Wb.Icon.ChartLine` |
| `ChartBar` | `WorkbenchIcons.ChartBar` | `Wb.Icon.ChartBar` |
| `Orbit` | `WorkbenchIcons.Orbit` | `Wb.Icon.Orbit` |
| `Pan` | `WorkbenchIcons.Pan` | `Wb.Icon.Pan` |
| `Camera` | `WorkbenchIcons.Camera` | `Wb.Icon.Camera` |
| `Lock` | `WorkbenchIcons.Lock` | `Wb.Icon.Lock` |
| `Support` | `WorkbenchIcons.Support` | `Wb.Icon.Support` |
| `Layers` | `WorkbenchIcons.Layers` | `Wb.Icon.Layers` |

## Status

| Name | Static Property | Resource Key |
| --- | --- | --- |
| `Info` | `WorkbenchIcons.Info` | `Wb.Icon.Info` |
| `Warning` | `WorkbenchIcons.Warning` | `Wb.Icon.Warning` |
| `Error` | `WorkbenchIcons.Error` | `Wb.Icon.Error` |
| `Success` | `WorkbenchIcons.Success` | `Wb.Icon.Success` |
| `Check` | `WorkbenchIcons.Check` | `Wb.Icon.Check` |
| `Help` | `WorkbenchIcons.Help` | `Wb.Icon.Help` |

## Remote / Jobs

| Name | Static Property | Resource Key |
| --- | --- | --- |
| `Server` | `WorkbenchIcons.Server` | `Wb.Icon.Server` |
| `Cloud` | `WorkbenchIcons.Cloud` | `Wb.Icon.Cloud` |
| `Hourglass` | `WorkbenchIcons.Hourglass` | `Wb.Icon.Hourglass` |

## Navigation

| Name | Static Property | Resource Key |
| --- | --- | --- |
| `ChevronDown` | `WorkbenchIcons.ChevronDown` | `Wb.Icon.ChevronDown` |
| `ChevronUp` | `WorkbenchIcons.ChevronUp` | `Wb.Icon.ChevronUp` |
| `ChevronLeft` | `WorkbenchIcons.ChevronLeft` | `Wb.Icon.ChevronLeft` |
| `ChevronRight` | `WorkbenchIcons.ChevronRight` | `Wb.Icon.ChevronRight` |
| `More` | `WorkbenchIcons.More` | `Wb.Icon.More` |
| `Menu` | `WorkbenchIcons.Menu` | `Wb.Icon.Menu` |
| `ArrowRight` | `WorkbenchIcons.ArrowRight` | `Wb.Icon.ArrowRight` |
| `ArrowLeft` | `WorkbenchIcons.ArrowLeft` | `Wb.Icon.ArrowLeft` |
