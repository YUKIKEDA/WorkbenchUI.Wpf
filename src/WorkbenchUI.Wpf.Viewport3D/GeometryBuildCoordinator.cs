namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Generation tracking for asynchronous geometry builds.
/// <para>
/// Each build request advances the generation counter and cancels the previous generation's
/// <see cref="CancellationToken"/>. A completed build may only be committed when
/// <see cref="IsCurrent"/> returns true (stale results are discarded). UI-thread only.
/// </para>
/// </summary>
internal sealed class GeometryBuildCoordinator
{
    private CancellationTokenSource? _cts;

    /// <summary>Current build generation (0 = not started).</summary>
    public int CurrentGeneration { get; private set; }

    /// <summary>
    /// Starts a new build generation, canceling any in-progress build.
    /// Pass the returned generation number to <see cref="IsCurrent"/> upon completion.
    /// </summary>
    public (int Generation, CancellationToken Token) Begin()
    {
        var prev = _cts;
        _cts = new CancellationTokenSource();
        if (prev is not null)
        {
            prev.Cancel();
            prev.Dispose();
        }

        return (++CurrentGeneration, _cts.Token);
    }

    /// <summary>Returns whether the specified generation is still current (if false, discard its results).</summary>
    public bool IsCurrent(int generation) => generation == CurrentGeneration;

    /// <summary>
    /// Cancels any in-progress build and invalidates all outstanding generations
    /// (called on unload or renderer disposal).
    /// </summary>
    public void CancelAll()
    {
        var prev = _cts;
        _cts = null;
        if (prev is not null)
        {
            prev.Cancel();
            prev.Dispose();
        }

        CurrentGeneration++;
    }
}
