---
title: Built-in Icons
description: Complete list of all 68 built-in 16x16 vector Geometry icons in WorkbenchIcons.
sidebar_position: 3
---

# Built-in Icons (`WorkbenchIcons`)

`WorkbenchIcons` provides **68** stroke-based `Geometry` icons drawn on a `16×16` grid (`WorkbenchIcons.CanvasSize`). Every icon is frozen for cross-thread safety and available both as a static property (`WorkbenchIcons.<Name>`) and as a resource key (`Wb.Icon.<Name>`).

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

## Complete Icon List

### File & Project
`File`, `NewFile`, `Folder`, `FolderOpen`, `Save`, `SaveAs`, `History`, `Settings`, `Download`, `Upload`, `Close`

### Editing
`Undo`, `Redo`, `Delete`, `Add`, `Remove`, `Copy`, `Edit`, `Search`, `Filter`, `Refresh`

### View
`ZoomIn`, `ZoomOut`, `ZoomToFit`, `Eye`, `EyeOff`, `Layout`, `Sun`, `Moon`, `Grid`, `List`, `Home`

### Playback
`Play`, `Pause`, `Stop`

### Engineering / CAE / 3D
`Mesh`, `Cube`, `Section`, `Probe`, `Annotation`, `Vector`, `Contour`, `ChartLine`, `ChartBar`, `Orbit`, `Pan`, `Camera`, `Lock`, `Support`, `Layers`

### Status
`Info`, `Warning`, `Error`, `Success`, `Check`, `Help`

### Remote / Jobs
`Server`, `Cloud`, `Hourglass`

### Navigation
`ChevronDown`, `ChevronUp`, `ChevronLeft`, `ChevronRight`, `More`, `Menu`, `ArrowRight`, `ArrowLeft`
