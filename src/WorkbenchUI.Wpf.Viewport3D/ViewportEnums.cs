namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>Camera projection mode. Engineering/CAE workflows frequently use orthographic projection for dimensional inspection.</summary>
public enum ViewportProjection
{
    /// <summary>Perspective projection.</summary>
    Perspective,

    /// <summary>Orthographic (parallel) projection.</summary>
    Orthographic,
}

/// <summary>Upward axis for turntable orbit rotation. Defaults to Z-up to match CAE conventions.</summary>
public enum ViewportUpAxis
{
    /// <summary>Z axis points up (standard in CAE / CAD).</summary>
    ZUp,

    /// <summary>Y axis points up (standard in DCC / gaming).</summary>
    YUp,
}

/// <summary>
/// Selection granularity for left-button picking.
/// Faces (triangles) and nodes are reported using render-level indices; mapping back to solver entities is the application's responsibility.
/// </summary>
public enum ViewportPickMode
{
    /// <summary>Picking disabled (left button performs no pick).</summary>
    None,

    /// <summary>Select whole parts (<see cref="ViewportMesh"/>).</summary>
    Part,

    /// <summary>Select individual triangles (faces).</summary>
    Face,

    /// <summary>Select individual vertices (nodes).</summary>
    Node,

    /// <summary>
    /// Probe mode. Interpolates scalar values at the clicked location, raises <see cref="WorkbenchViewport.ProbePicked"/>,
    /// and automatically adds an annotation label by default. Rubber-band selection is disabled (click only).
    /// </summary>
    Probe,

    /// <summary>
    /// Voxel cell picking/probing. Returns the hit cell index in <see cref="WorkbenchViewport.VoxelSource"/>
    /// and raises <see cref="WorkbenchViewport.VoxelProbePicked"/>.
    /// </summary>
    Cell,
}

/// <summary>Standard camera viewpoints. Under Z-up, Front views from the -Y side following CAD conventions.</summary>
public enum ViewportStandardView
{
    /// <summary>Front view.</summary>
    Front,

    /// <summary>Back view.</summary>
    Back,

    /// <summary>Left side view.</summary>
    Left,

    /// <summary>Right side view.</summary>
    Right,

    /// <summary>Top view.</summary>
    Top,

    /// <summary>Bottom view.</summary>
    Bottom,

    /// <summary>Isometric view.</summary>
    Isometric,
}
