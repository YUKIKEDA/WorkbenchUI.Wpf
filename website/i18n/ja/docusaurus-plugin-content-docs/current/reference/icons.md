---
title: 内蔵アイコン一覧
description: WorkbenchIcons が提供する 68 種の 16x16 ベクタ Geometry アイコン完全一覧。
sidebar_position: 3
---

# 内蔵アイコン一覧 (`WorkbenchIcons`)

`WorkbenchIcons` は `16×16` グリッド（`WorkbenchIcons.CanvasSize`）に描かれた **68 種類** のストローク方式ベクタアイコンを提供します。すべて Frozen 済みで、静的プロパティ（`WorkbenchIcons.<Name>`）およびリソースキー（`Wb.Icon.<Name>`）の両方から利用できます。

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

## アイコン一覧

### ファイル・プロジェクト
`File`, `NewFile`, `Folder`, `FolderOpen`, `Save`, `SaveAs`, `History`, `Settings`, `Download`, `Upload`, `Close`

### 編集
`Undo`, `Redo`, `Delete`, `Add`, `Remove`, `Copy`, `Edit`, `Search`, `Filter`, `Refresh`

### 表示
`ZoomIn`, `ZoomOut`, `ZoomToFit`, `Eye`, `EyeOff`, `Layout`, `Sun`, `Moon`, `Grid`, `List`, `Home`

### 再生
`Play`, `Pause`, `Stop`

### エンジニアリング / CAE / 3D
`Mesh`, `Cube`, `Section`, `Probe`, `Annotation`, `Vector`, `Contour`, `ChartLine`, `ChartBar`, `Orbit`, `Pan`, `Camera`, `Lock`, `Support`, `Layers`

### ステータス
`Info`, `Warning`, `Error`, `Success`, `Check`, `Help`

### リモート / ジョブ
`Server`, `Cloud`, `Hourglass`

### ナビゲーション
`ChevronDown`, `ChevronUp`, `ChevronLeft`, `ChevronRight`, `More`, `Menu`, `ArrowRight`, `ArrowLeft`
