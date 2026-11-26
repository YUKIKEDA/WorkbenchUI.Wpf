namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Hit information for a single probe pick.
/// Barycentric value interpolation is handled by the library, while physical units and formatting
/// are supplied by the application via <see cref="WorkbenchViewport.ProbeLabelFormatter"/>.
/// </summary>
/// <param name="Mesh">Hit mesh.</param>
/// <param name="TriangleIndex">Hit triangle index (in units of TriangleIndices / 3).</param>
/// <param name="NodeIndex">Node with the largest barycentric weight at the hit point, used as the annotation anchor.</param>
/// <param name="X">Model-space X coordinate of the hit point (on the undeformed geometry).</param>
/// <param name="Y">Model-space Y coordinate of the hit point.</param>
/// <param name="Z">Model-space Z coordinate of the hit point.</param>
/// <param name="ScalarValue">Barycentric-interpolated scalar value at the hit point, or null if the mesh has no ScalarValues.</param>
/// <param name="NodeScalarValue">Scalar value at <see cref="NodeIndex"/>, or null if the mesh has no ScalarValues.</param>
public sealed record ProbeResult(
    ViewportMesh Mesh,
    int TriangleIndex,
    int NodeIndex,
    double X,
    double Y,
    double Z,
    double? ScalarValue,
    double? NodeScalarValue);

/// <summary>
/// Event arguments for <see cref="WorkbenchViewport.ProbePicked"/>.
/// Setting <see cref="Handled"/> to true suppresses the default automatic annotation creation
/// (useful when the application manages or displays annotations itself).
/// </summary>
public sealed class ProbePickedEventArgs(ProbeResult result) : EventArgs
{
    /// <summary>Probe hit information.</summary>
    public ProbeResult Result { get; } = result;

    /// <summary>When true, prevents the viewport from automatically adding an annotation. Default is false.</summary>
    public bool Handled { get; set; }
}
