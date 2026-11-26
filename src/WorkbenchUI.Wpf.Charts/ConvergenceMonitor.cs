using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WorkbenchUI.Wpf.Charts;

/// <summary>
/// Streaming residual chart for monitoring solver convergence.
/// <para>
/// Bind a collection of <see cref="ConvergenceSeries"/> instances to <see cref="SeriesSource"/>
/// and call <see cref="ConvergenceSeries.Append"/> from a worker thread.
/// Redrawing is throttled to approximately 100 ms by an internal timer.
/// </para>
/// </summary>
[TemplatePart(Name = PartPlot, Type = typeof(WorkbenchPlot))]
public class ConvergenceMonitor : Control
{
    private const string PartPlot = "PART_Plot";

    /// <summary>Identifies the <see cref="SeriesSource"/> dependency property.</summary>
    public static readonly DependencyProperty SeriesSourceProperty =
        DependencyProperty.Register(
            nameof(SeriesSource), typeof(IEnumerable), typeof(ConvergenceMonitor),
            new PropertyMetadata(null, OnSeriesSourceChanged));

    /// <summary>Identifies the <see cref="Threshold"/> dependency property.</summary>
    public static readonly DependencyProperty ThresholdProperty =
        DependencyProperty.Register(
            nameof(Threshold), typeof(double), typeof(ConvergenceMonitor),
            new PropertyMetadata(double.NaN, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="IsLogScale"/> dependency property.</summary>
    public static readonly DependencyProperty IsLogScaleProperty =
        DependencyProperty.Register(
            nameof(IsLogScale), typeof(bool), typeof(ConvergenceMonitor),
            new PropertyMetadata(true, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="XLabel"/> dependency property.</summary>
    public static readonly DependencyProperty XLabelProperty =
        DependencyProperty.Register(
            nameof(XLabel), typeof(string), typeof(ConvergenceMonitor),
            new PropertyMetadata("Iteration", OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="YLabel"/> dependency property.</summary>
    public static readonly DependencyProperty YLabelProperty =
        DependencyProperty.Register(
            nameof(YLabel), typeof(string), typeof(ConvergenceMonitor),
            new PropertyMetadata("Residual", OnPlotPropertyChanged));

    private readonly DispatcherTimer _timer;
    private WorkbenchPlot? _plot;
    private volatile bool _dirty;

    static ConvergenceMonitor()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ConvergenceMonitor), new FrameworkPropertyMetadata(typeof(ConvergenceMonitor)));
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ConvergenceMonitor"/>.
    /// </summary>
    public ConvergenceMonitor()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(100),
        };
        _timer.Tick += OnTimerTick;

        Loaded += (_, _) => _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
    }

    /// <summary>Collection of <see cref="ConvergenceSeries"/> instances to display.</summary>
    public IEnumerable? SeriesSource
    {
        get => (IEnumerable?)GetValue(SeriesSourceProperty);
        set => SetValue(SeriesSourceProperty, value);
    }

    /// <summary>Convergence threshold value. When <see cref="double.NaN"/> (default), no reference line is displayed.</summary>
    public double Threshold
    {
        get => (double)GetValue(ThresholdProperty);
        set => SetValue(ThresholdProperty, value);
    }

    /// <summary>Whether to use a logarithmic scale on the vertical axis (default is <see langword="true"/>).</summary>
    public bool IsLogScale
    {
        get => (bool)GetValue(IsLogScaleProperty);
        set => SetValue(IsLogScaleProperty, value);
    }

    /// <summary>Horizontal axis label (default is <c>"Iteration"</c>).</summary>
    public string XLabel
    {
        get => (string)GetValue(XLabelProperty);
        set => SetValue(XLabelProperty, value);
    }

    /// <summary>Vertical axis label (default is <c>"Residual"</c>).</summary>
    public string YLabel
    {
        get => (string)GetValue(YLabelProperty);
        set => SetValue(YLabelProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_plot is not null)
        {
            _plot.ThemeApplied -= OnThemeApplied;
        }

        _plot = GetTemplateChild(PartPlot) as WorkbenchPlot;

        if (_plot is not null)
        {
            _plot.ThemeApplied += OnThemeApplied;
            Rebuild();
        }
    }

    private static void OnSeriesSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var monitor = (ConvergenceMonitor)d;

        if (e.OldValue is INotifyCollectionChanged oldIncc)
        {
            oldIncc.CollectionChanged -= monitor.OnCollectionChanged;
        }

        monitor.DetachSeriesHandlers(e.OldValue as IEnumerable);

        if (e.NewValue is INotifyCollectionChanged newIncc)
        {
            newIncc.CollectionChanged += monitor.OnCollectionChanged;
        }

        monitor.AttachSeriesHandlers(e.NewValue as IEnumerable);
        monitor.Rebuild();
    }

    private static void OnPlotPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((ConvergenceMonitor)d).Rebuild();

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        DetachSeriesHandlers(SeriesSource);
        AttachSeriesHandlers(SeriesSource);
        Rebuild();
    }

    private void AttachSeriesHandlers(IEnumerable? source)
    {
        if (source is null)
        {
            return;
        }

        foreach (var item in source)
        {
            if (item is ConvergenceSeries series)
            {
                series.Changed += OnSeriesChanged;
            }
        }
    }

    private void DetachSeriesHandlers(IEnumerable? source)
    {
        if (source is null)
        {
            return;
        }

        foreach (var item in source)
        {
            if (item is ConvergenceSeries series)
            {
                series.Changed -= OnSeriesChanged;
            }
        }
    }

    // May be invoked from a worker thread, so only set the dirty flag here.
    private void OnSeriesChanged(object? sender, EventArgs e) => _dirty = true;

    private void OnThemeApplied(object? sender, EventArgs e) => Rebuild();

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_dirty)
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        _dirty = false;

        if (_plot is null)
        {
            return;
        }

        var plot = _plot.Plot;
        plot.Clear();

        var hasNamedSeries = false;

        if (SeriesSource is not null)
        {
            foreach (var item in SeriesSource)
            {
                if (item is not ConvergenceSeries series)
                {
                    continue;
                }

                var values = series.Snapshot();
                if (values.Length == 0)
                {
                    continue;
                }

                var xs = new double[values.Length];
                var ys = new double[values.Length];
                for (var i = 0; i < values.Length; i++)
                {
                    xs[i] = i + 1;
                    ys[i] = IsLogScale ? ChartHelpers.SafeLog10(values[i]) : values[i];
                }

                var line = plot.Add.ScatterLine(xs, ys);
                line.LineWidth = 2;

                if (series.Color is { } color)
                {
                    line.Color = WorkbenchChartTheme.ToScottPlotColor(color);
                }

                if (!string.IsNullOrEmpty(series.Name))
                {
                    line.LegendText = series.Name;
                    hasNamedSeries = true;
                }
            }
        }

        if (!double.IsNaN(Threshold))
        {
            var y = IsLogScale ? ChartHelpers.SafeLog10(Threshold) : Threshold;
            var line = plot.Add.HorizontalLine(y);
            line.LinePattern = ScottPlot.LinePattern.Dashed;
            line.LineWidth = 1.5f;
            line.Color = ChartHelpers.GetTokenColor("Wb.Color.Warning", "#CCA700");
        }

        plot.Axes.Left.TickGenerator = IsLogScale
            ? ChartHelpers.CreateLogTickGenerator()
            : new ScottPlot.TickGenerators.NumericAutomatic();

        plot.Legend.IsVisible = hasNamedSeries;
        plot.XLabel(XLabel);
        plot.YLabel(YLabel);
        plot.Axes.AutoScale();
        _plot.Refresh();
    }
}
