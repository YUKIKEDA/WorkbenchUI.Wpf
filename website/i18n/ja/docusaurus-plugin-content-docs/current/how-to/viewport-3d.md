---
title: 3D ビューポートの描画
description: WorkbenchViewport でのメッシュ・疎ボクセル格子・コンター・変形・断面・プローブの利用方法。
sidebar_position: 6
---

# 3D メッシュとボクセルを描画する

`WorkbenchViewport`（`WorkbenchUI.Wpf.Viewport3D`）は Direct3D 11 ベースの 3D ビューポートコントロールです（`D3DImage` 経由でエアスペース問題を回避し、ソフトウェア環境では `WriteableBitmap` へ自動フォールバックします）。

## コンター付きサーフェスメッシュの表示

`MeshSource` に `ObservableCollection<ViewportMesh>` をバインドし、必要に応じて `ColorScale` を `ColorMapLegend` と共有します:

```xml
<Grid>
    <wb:WorkbenchViewport x:Name="Viewport"
                          MeshSource="{Binding Meshes}"
                          ColorScale="{Binding Scale}"
                          ShowContours="True"
                          ShowEdges="True"
                          ShowAxisTriad="True"
                          ShowViewCube="True"
                          PickMode="Node" />

    <wb:ColorMapLegend Scale="{Binding Scale}"
                       Title="Stress [MPa]"
                       HorizontalAlignment="Right"
                       VerticalAlignment="Bottom"
                       Margin="12" />
</Grid>
```

```csharp
var mesh = new ViewportMesh
{
    Name = "Bracket",
    Positions = positions,           // double[3 * N]: x0, y0, z0, ...
    TriangleIndices = triangles,     // int[3 * T]
    ScalarValues = nodalStresses,    // double[N]
};
Meshes.Add(mesh);
Viewport.FitToView();
```

> **配列差し替え規約**: `Positions`、`TriangleIndices`、`ScalarValues`、`Displacements`、`VectorValues` の更新は要素のインプレース書き換えではなく、新しい配列インスタンスを代入して通知してください。`ScalarValues` や `Displacements` のみの差し替え時は、ジオメトリ再構築を行わずに GPU バッファを部分更新します。

## 疎ボクセル格子の表示

活性セルのみを保持する直交格子には `ViewportVoxelGrid` と `ViewportVoxelIndexing.Encode` を使います:

```csharp
int nx = 100, ny = 100, nz = 100;
int[] active = [ViewportVoxelIndexing.Encode(10, 20, 30, nx, ny)];

var grid = new ViewportVoxelGrid
{
    Name = "Casting",
    ResolutionX = nx,
    ResolutionY = ny,
    ResolutionZ = nz,
    CellSizeX = 0.01,
    CellSizeY = 0.01,
    CellSizeZ = 0.01,
    ActiveCellIndices = active,
    ScalarValues = [650.0],
};
Voxels.Add(grid);
```

## GPU 変形表示と断面カット

- **変形表示**: `mesh.Displacements`（`double[3 * N]`）を設定し、`Viewport.DeformationScale`（推奨値は `Viewport.GetSuggestedDeformationScale()`）、`IsDeformationAnimated`、`ShowUndeformedWireframe` で制御します。
- **断面カット**: `Viewport.SectionPlane` に `SectionPlane`（`OriginX/Y/Z`, `NormalX/Y/Z`）を設定し、`ShowSectionPlaneIndicator` でインジケーター表示を切り替えます。個別のメッシュ／ボクセルは `IsClippable = false` でクリップ対象外にできます。

## プローブ・選択・注釈

`PickMode`（`None`, `Part`, `Face`, `Node`, `Probe`, `Cell`）を設定し、`ProbePicked` または `VoxelProbePicked` を購読して `Viewport.Annotations` へ注釈を追加できます:

```csharp
Viewport.PickMode = ViewportPickMode.Probe;
Viewport.ProbePicked += (_, e) =>
{
    var r = e.Result;
    Viewport.Annotations.Add(new ViewportAnnotation
    {
        Mesh = r.Mesh,
        NodeIndex = r.NodeIndex,
        X = r.X, Y = r.Y, Z = r.Z,
        Text = $"{r.ScalarValue:F2}",
    });
    e.Handled = true;
};
```
