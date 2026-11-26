namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Progress notification for asynchronous geometry builds, passed to
/// <see cref="WorkbenchViewport.GeometryBuildProgressChanged"/> on the UI thread.
/// </summary>
public sealed class ViewportBuildProgressEventArgs(string stage, double progress) : EventArgs
{
    /// <summary>Name of the current build stage (e.g., "Building geometry (1/2)").</summary>
    public string Stage { get; } = stage;

    /// <summary>Overall progress in the range [0, 1].</summary>
    public double Progress { get; } = progress;
}
