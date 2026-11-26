namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>Scope of rubber-band (rectangle) selection.</summary>
public enum ViewportRubberBandSelectionMode
{
    /// <summary>Select visible elements only (via GPU ID buffer readback; default).</summary>
    VisibleOnly,

    /// <summary>
    /// Through-selection including occluded elements, evaluated via parallel CPU screen projection
    /// (Face: all 3 vertices inside rect; Node: projected vertex inside rect; Part: any triangle overlapping rect).
    /// Accounts for active deformation scale and excludes elements clipped by the section plane.
    /// </summary>
    Through,
}
