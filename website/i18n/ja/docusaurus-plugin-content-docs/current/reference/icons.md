---
title: 内蔵アイコン一覧
description: WorkbenchIcons が提供する 67 種の 16x16 ベクタ Geometry アイコン完全一覧。
sidebar_position: 3
---

# 内蔵アイコン一覧 (`WorkbenchIcons`)

`WorkbenchIcons` は `16×16` グリッド（`WorkbenchIcons.CanvasSize`）に描かれた **67 種類** のストローク方式ベクタアイコンを提供します。すべて Frozen 済みで、静的プロパティ（`WorkbenchIcons.<Name>`）およびリソースキー（`Wb.Icon.<Name>`）の両方から利用できます。

## 使い方

```xml
<!-- WorkbenchIcon で表示 -->
<wb:WorkbenchIcon Icon="{x:Static wb:WorkbenchIcons.Save}" Size="16" />

<!-- ButtonAssist でボタンにアイコンを付与 -->
<Button Content="Run" wb:ButtonAssist.Icon="{x:Static wb:WorkbenchIcons.Play}" />

<!-- リソースキーによるアイコンのみボタン -->
<Button Style="{StaticResource Wb.Button.Icon}"
        wb:ButtonAssist.Icon="{StaticResource Wb.Icon.ZoomToFit}"
        ToolTip="Fit to View" />
```

## ファイル・プロジェクト

| 名前 | 静的プロパティ | リソースキー |
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

## 編集

| 名前 | 静的プロパティ | リソースキー |
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

## 表示

| 名前 | 静的プロパティ | リソースキー |
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

## 再生

| 名前 | 静的プロパティ | リソースキー |
| --- | --- | --- |
| `Play` | `WorkbenchIcons.Play` | `Wb.Icon.Play` |
| `Pause` | `WorkbenchIcons.Pause` | `Wb.Icon.Pause` |
| `Stop` | `WorkbenchIcons.Stop` | `Wb.Icon.Stop` |

## エンジニアリング / CAE / 3D

| 名前 | 静的プロパティ | リソースキー |
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

## ステータス

| 名前 | 静的プロパティ | リソースキー |
| --- | --- | --- |
| `Info` | `WorkbenchIcons.Info` | `Wb.Icon.Info` |
| `Warning` | `WorkbenchIcons.Warning` | `Wb.Icon.Warning` |
| `Error` | `WorkbenchIcons.Error` | `Wb.Icon.Error` |
| `Success` | `WorkbenchIcons.Success` | `Wb.Icon.Success` |
| `Check` | `WorkbenchIcons.Check` | `Wb.Icon.Check` |
| `Help` | `WorkbenchIcons.Help` | `Wb.Icon.Help` |

## リモート / ジョブ

| 名前 | 静的プロパティ | リソースキー |
| --- | --- | --- |
| `Server` | `WorkbenchIcons.Server` | `Wb.Icon.Server` |
| `Cloud` | `WorkbenchIcons.Cloud` | `Wb.Icon.Cloud` |
| `Hourglass` | `WorkbenchIcons.Hourglass` | `Wb.Icon.Hourglass` |

## ナビゲーション

| 名前 | 静的プロパティ | リソースキー |
| --- | --- | --- |
| `ChevronDown` | `WorkbenchIcons.ChevronDown` | `Wb.Icon.ChevronDown` |
| `ChevronUp` | `WorkbenchIcons.ChevronUp` | `Wb.Icon.ChevronUp` |
| `ChevronLeft` | `WorkbenchIcons.ChevronLeft` | `Wb.Icon.ChevronLeft` |
| `ChevronRight` | `WorkbenchIcons.ChevronRight` | `Wb.Icon.ChevronRight` |
| `More` | `WorkbenchIcons.More` | `Wb.Icon.More` |
| `Menu` | `WorkbenchIcons.Menu` | `Wb.Icon.Menu` |
| `ArrowRight` | `WorkbenchIcons.ArrowRight` | `Wb.Icon.ArrowRight` |
| `ArrowLeft` | `WorkbenchIcons.ArrowLeft` | `Wb.Icon.ArrowLeft` |
