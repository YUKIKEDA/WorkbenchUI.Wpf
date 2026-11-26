using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Composite control for displaying the progress of a single job.
/// Consists of a progress bar, status text, elapsed time, and an optional cancel button (<see cref="ICommand"/>).
/// Elapsed time is tracked automatically with an internal stopwatch while <see cref="IsRunning"/> is <see langword="true"/>.
/// To display multiple jobs, host this control inside an <see cref="ItemsControl"/>.
/// </summary>
public class ProgressDisplay : Control
{
    private readonly Stopwatch _stopwatch = new();
    private readonly DispatcherTimer _timer;

    static ProgressDisplay()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ProgressDisplay), new FrameworkPropertyMetadata(typeof(ProgressDisplay)));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressDisplay"/> class.
    /// </summary>
    public ProgressDisplay()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => UpdateElapsedText();
        Loaded += (_, _) =>
        {
            if (IsRunning)
            {
                _timer.Start();
            }
        };
        Unloaded += (_, _) => _timer.Stop();
    }

    /// <summary>Identifies the <see cref="Text"/> dependency property.</summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(ProgressDisplay), new PropertyMetadata(string.Empty));

    /// <summary>Gets or sets the status text (for example, "Generating mesh...").</summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Identifies the <see cref="Minimum"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(ProgressDisplay),
        new PropertyMetadata(0.0, OnProgressChanged));

    /// <summary>Gets or sets the minimum progress value.</summary>
    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>Identifies the <see cref="Maximum"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(ProgressDisplay),
        new PropertyMetadata(100.0, OnProgressChanged));

    /// <summary>Gets or sets the maximum progress value.</summary>
    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(ProgressDisplay),
        new PropertyMetadata(0.0, OnProgressChanged));

    /// <summary>Gets or sets the current progress value.</summary>
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Identifies the <see cref="IsIndeterminate"/> dependency property.</summary>
    public static readonly DependencyProperty IsIndeterminateProperty = DependencyProperty.Register(
        nameof(IsIndeterminate), typeof(bool), typeof(ProgressDisplay),
        new PropertyMetadata(false, OnProgressChanged));

    /// <summary>Gets or sets a value indicating whether progress is indeterminate (hides percentage text).</summary>
    public bool IsIndeterminate
    {
        get => (bool)GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    /// <summary>Identifies the <see cref="IsRunning"/> dependency property.</summary>
    public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
        nameof(IsRunning), typeof(bool), typeof(ProgressDisplay),
        new PropertyMetadata(false, OnIsRunningChanged));

    /// <summary>
    /// Gets or sets a value indicating whether the elapsed timer is running.
    /// Setting this to <see langword="true"/> starts the stopwatch; setting it to <see langword="false"/> stops it while keeping the final elapsed text visible.
    /// </summary>
    public bool IsRunning
    {
        get => (bool)GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    /// <summary>Identifies the <see cref="CancelCommand"/> dependency property.</summary>
    public static readonly DependencyProperty CancelCommandProperty = DependencyProperty.Register(
        nameof(CancelCommand), typeof(ICommand), typeof(ProgressDisplay), new PropertyMetadata(null));

    /// <summary>Gets or sets the command invoked by the cancel button. When <see langword="null"/>, the button is hidden.</summary>
    public ICommand? CancelCommand
    {
        get => (ICommand?)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    /// <summary>Identifies the <see cref="CancelCommandParameter"/> dependency property.</summary>
    public static readonly DependencyProperty CancelCommandParameterProperty = DependencyProperty.Register(
        nameof(CancelCommandParameter), typeof(object), typeof(ProgressDisplay), new PropertyMetadata(null));

    /// <summary>Gets or sets the parameter passed to <see cref="CancelCommand"/>.</summary>
    public object? CancelCommandParameter
    {
        get => GetValue(CancelCommandParameterProperty);
        set => SetValue(CancelCommandParameterProperty, value);
    }

    // Built-in strings are exposed as dependency properties so callers can replace or localize them.
    /// <summary>Identifies the <see cref="CancelText"/> dependency property.</summary>
    public static readonly DependencyProperty CancelTextProperty = DependencyProperty.Register(
        nameof(CancelText), typeof(string), typeof(ProgressDisplay), new PropertyMetadata("Cancel"));

    /// <summary>Gets or sets the label displayed on the cancel button.</summary>
    public string CancelText
    {
        get => (string)GetValue(CancelTextProperty);
        set => SetValue(CancelTextProperty, value);
    }

    private static readonly DependencyPropertyKey PercentTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(PercentText), typeof(string), typeof(ProgressDisplay), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="PercentText"/> dependency property.</summary>
    public static readonly DependencyProperty PercentTextProperty = PercentTextPropertyKey.DependencyProperty;

    /// <summary>Gets the formatted completion percentage text (empty when <see cref="IsIndeterminate"/> is <see langword="true"/>).</summary>
    public string PercentText => (string)GetValue(PercentTextProperty);

    private static readonly DependencyPropertyKey ElapsedTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ElapsedText), typeof(string), typeof(ProgressDisplay), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="ElapsedText"/> dependency property.</summary>
    public static readonly DependencyProperty ElapsedTextProperty = ElapsedTextPropertyKey.DependencyProperty;

    /// <summary>Gets the formatted elapsed time text (empty before timing starts).</summary>
    public string ElapsedText => (string)GetValue(ElapsedTextProperty);

    /// <summary>Gets the current elapsed duration.</summary>
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((ProgressDisplay)d).UpdatePercentText();

    private static void OnIsRunningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var display = (ProgressDisplay)d;
        if ((bool)e.NewValue)
        {
            display._stopwatch.Restart();
            display._timer.Start();
        }
        else
        {
            display._stopwatch.Stop();
            display._timer.Stop();
        }

        display.UpdateElapsedText();
    }

    private void UpdatePercentText()
    {
        var range = Maximum - Minimum;
        var text = IsIndeterminate || range <= 0
            ? string.Empty
            : Math.Clamp((Value - Minimum) / range, 0.0, 1.0).ToString("P0", CultureInfo.CurrentCulture);
        SetValue(PercentTextPropertyKey, text);
    }

    private void UpdateElapsedText()
    {
        var elapsed = _stopwatch.Elapsed;
        var text = elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
            : $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        SetValue(ElapsedTextPropertyKey, text);
    }
}
