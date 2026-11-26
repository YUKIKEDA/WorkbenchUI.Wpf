# WorkbenchUI.Wpf.Viewport3D

Direct3D 11 (`Silk.NET`) 3D viewport control (`WorkbenchViewport`) for `WorkbenchUI.Wpf`, designed for engineering meshes, sparse voxel grids, scalar contour fields, and interactive post-processing.

## Capabilities

- **Rendering**: Direct3D 11 hardware rendering presented via WPF `D3DImage` (no airspace issues), with automatic `WriteableBitmap` fallback on WARP/software environments.
- **Data Models**: `ViewportMesh` (triangle surface meshes) and `ViewportVoxelGrid` (sparse voxel grids with brick LOD).
- **Post-Processing**: Scalar contour mapping (`ColorScale`), GPU vertex deformation and animation, GPU section-plane clipping (`SectionPlane`), 3D vector glyphs, and wireframe edge overlays.
- **Interaction**: Orbit/pan/zoom camera (`ViewportCamera`), standard views + clickable `ViewCube`, node/element/part/cell picking, hover pre-highlighting, rubber-band box selection, surface/voxel probing, and leader-line `ViewportAnnotation` overlays.

## License

MIT License. Depends on `Silk.NET` Direct3D / DXGI bindings (MIT).
