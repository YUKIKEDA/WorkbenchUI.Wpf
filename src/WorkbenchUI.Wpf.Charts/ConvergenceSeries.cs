namespace WorkbenchUI.Wpf.Charts;

/// <summary>
/// Thread-safe residual series streamed into <see cref="ConvergenceMonitor"/>.
/// <para>
/// <see cref="Append"/> and <see cref="Clear"/> are thread-safe and can be called directly
/// from solver worker threads. The chart control retrieves a <see cref="Snapshot"/> on a
/// throttled timer to redraw.
/// </para>
/// </summary>
public class ConvergenceSeries
{
    private readonly Lock _sync = new();
    private readonly List<double> _values = [];

    /// <summary>
    /// Initializes a new instance of <see cref="ConvergenceSeries"/>.
    /// </summary>
    /// <param name="name">Optional series name displayed in the legend.</param>
    public ConvergenceSeries(string name = "")
    {
        Name = name;
    }

    /// <summary>Series name displayed in the legend.</summary>
    public string Name { get; set; }

    /// <summary>Line color. When <see langword="null"/>, a color is assigned automatically from the theme palette.</summary>
    public System.Windows.Media.Color? Color { get; set; }

    /// <summary>Current number of recorded iterations.</summary>
    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _values.Count;
            }
        }
    }

    /// <summary>Raised on the caller's thread when values are appended or cleared.</summary>
    public event EventHandler? Changed;

    /// <summary>Appends a residual value to the end of the series. Safe to call from any thread.</summary>
    /// <param name="residual">Residual value to append.</param>
    public void Append(double residual)
    {
        lock (_sync)
        {
            _values.Add(residual);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Clears all values from the series. Safe to call from any thread.</summary>
    public void Clear()
    {
        lock (_sync)
        {
            _values.Clear();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Returns a thread-safe copy of the current residual values.</summary>
    public double[] Snapshot()
    {
        lock (_sync)
        {
            return [.. _values];
        }
    }
}
