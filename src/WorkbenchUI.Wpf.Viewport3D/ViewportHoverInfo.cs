namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Current hover pre-highlight target, accessible via <see cref="WorkbenchViewport.HoverInfo"/>.
/// Granularity follows <see cref="WorkbenchViewport.PickMode"/>:
/// Part targets the whole mesh (<see cref="TriangleIndex"/> and <see cref="NodeIndex"/> are -1),
/// Face targets a triangle, and Node/Probe targets the nearest node.
/// </summary>
/// <param name="Mesh">Currently hovered mesh.</param>
/// <param name="TriangleIndex">Currently hovered triangle index, or -1 if not applicable.</param>
/// <param name="NodeIndex">Currently hovered node index, or -1 if not applicable.</param>
/// <param name="IsPart">Whether the entire part is hovered.</param>
public sealed record ViewportHoverInfo(ViewportMesh Mesh, int TriangleIndex, int NodeIndex, bool IsPart);
