---
title: Render 3D Meshes & Voxels
description: How to display meshes, sparse voxel grids, scalar contours, deformations, section cuts, and probes in WorkbenchViewport.
sidebar_position: 6
---

# How to Render 3D Meshes & Voxels

`WorkbenchViewport` (`WorkbenchUI.Wpf.Viewport3D`) renders triangle meshes and sparse voxel grids on Direct3D 11 (`D3DImage` with automatic `WriteableBitmap` fallback).

## Displaying a Surface Mesh with Contours

Bind `MeshSource` to an `ObservableCollection<ViewportMesh>` and optionally attach a `ColorScale` shared with `ColorMapLegend`:

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

> **Array Replacement Convention**: Always replace `Positions`, `TriangleIndices`, `ScalarValues`, `Displacements`, or `VectorValues` with a new array instance rather than mutating elements in place. Replacing `ScalarValues` or `Displacements` alone performs a fast partial GPU upload without rebuilding geometry.

## Displaying a Sparse Voxel Grid

For structured Cartesian domains where only active cells are populated, use `ViewportVoxelGrid` and `ViewportVoxelIndexing.Encode`:

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

## GPU Deformation & Section Clipping

- **Deformation**: Assign `mesh.Displacements` (`double[3 * N]`), then adjust `Viewport.DeformationScale` (or call `Viewport.GetSuggestedDeformationScale()`), `IsDeformationAnimated`, and `ShowUndeformedWireframe`.
- **Section Plane**: Assign a `SectionPlane` (`OriginX/Y/Z`, `NormalX/Y/Z`) to `Viewport.SectionPlane` and toggle `ShowSectionPlaneIndicator`. Individual meshes or voxel grids can opt out with `IsClippable = false`.

## Probing, Selection & Annotations

Set `PickMode` (`None`, `Part`, `Element`, `Node`, `Probe`, `Cell`), handle `ProbePicked` or `VoxelProbePicked`, and attach callouts to `Viewport.Annotations`:

```csharp
Viewport.PickMode = ViewportPickMode.Probe;
Viewport.ProbePicked += (_, e) =>
{
    var r = e.Result;
    Viewport.Annotations.Add(new ViewportAnnotation
    {
        Mesh = r.Mesh,
        NodeIndex = r.NearestNodeIndex,
        X = r.X, Y = r.Y, Z = r.Z,
        Text = $"{r.ScalarValue:F2}",
    });
};
```
