using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WorkbenchUI.Wpf.Charts;

/// <summary>
/// General-purpose line chart for time-history and load-displacement curves.
/// Bind a collection of <see cref="ChartSeries"/> instances to <see cref="SeriesSource"/>.
/// Displays a crosshair and coordinate readout on mouse hover.
/// </summary>
[TemplatePart(Name = PartPlot, Type = typeof(WorkbenchPlot))]
public class HistoryChart : Control
{
    private const string PartPlot = "PART_Plot";

    /// <summary>Identifies the <see cref="SeriesSource"/> dependency property.</summary>
    public static readonly DependencyProperty SeriesSourceProperty =
        DependencyProperty.Register(
            nameof(SeriesSource), typeof(IEnumerable), typeof(HistoryChart),
            new PropertyMetadata(null, OnSeriesSourceChanged));

    /// <summary>Identifies the <see cref="XLabel"/> dependency property.</summary>
    public static readonly DependencyProperty XLabelProperty =
        DependencyProperty.Register(
            nameof(XLabel), typeof(string), typeof(HistoryChart),
            new PropertyMetadata("", OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="YLabel"/> dependency property.</summary>
    public static readonly DependencyProperty YLabelProperty =
        DependencyProperty.Register(
            nameof(YLabel), typeof(string), typeof(HistoryChart),
            new PropertyMetadata("", OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="ShowLegend"/> dependency property.</summary>
    public static readonly DependencyProperty ShowLegendProperty =
        DependencyProperty.Register(
            nameof(ShowLegend), typeof(bool), typeof(HistoryChart),
            new PropertyMetadata(true, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="ShowCrosshair"/> dependency property.</summary>
    public static readonly DependencyProperty ShowCrosshairProperty =
        DependencyProperty.Register(
            nameof(ShowCrosshair), typeof(bool), typeof(HistoryChart),
            new PropertyMetadata(true));

    private readonly List<INotifyPropertyChanged> _subscribedSeries = [];
    private WorkbenchPlot? _plot;
    private ScottPlot.Plottables.Crosshair? _crosshair;

    static HistoryChart()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(HistoryChart), new FrameworkPropertyMetadata(typeof(HistoryChart)));
    }

    /// <summary>Initializes a new instance of <see cref="HistoryChart"/>.</summary>
    public HistoryChart()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>Collection of <see cref="ChartSeries"/> instances to display.</summary>
    public IEnumerable? SeriesSource
    {
        get => (IEnumerable?)GetValue(SeriesSourceProperty);
        set => SetValue(SeriesSourceProperty, value);
    }

    /// <summary>Horizontal axis label.</summary>
    public string XLabel
    {
        get => (string)GetValue(XLabelProperty);
        set => SetValue(XLabelProperty, value);
    }

    /// <summary>Vertical axis label.</summary>
    public string YLabel
    {
        get => (string)GetValue(YLabelProperty);
        set => SetValue(YLabelProperty, value);
    }

    /// <summary>Whether to display the legend (default is <see langword="true"/>; shown only when at least one series has a name).</summary>
    public bool ShowLegend
    {
        get => (bool)GetValue(ShowLegendProperty);
        set => SetValue(ShowLegendProperty, value);
    }

    /// <summary>Whether to display the crosshair and coordinate readout on hover (default is <see langword="true"/>).</summary>
    public bool ShowCrosshair
    {
        get => (bool)GetValue(ShowCrosshairProperty);
        set => SetValue(ShowCrosshairProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_plot is not null)
        {
            _plot.ThemeApplied -= OnThemeApplied;
            _plot.MouseMove -= OnPlotMouseMove;
            _plot.MouseLeave -= OnPlotMouseLeave;
        }

        _plot = GetTemplateChild(PartPlot) as WorkbenchPlot;
        _crosshair = null;

        if (_plot is not null)
        {
            _plot.ThemeApplied += OnThemeApplied;
            _plot.MouseMove += OnPlotMouseMove;
            _plot.MouseLeave += OnPlotMouseLeave;
            Rebuild();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachSeriesHandlers(SeriesSource);
        Rebuild();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => DetachSeriesHandlers();

    private static void OnSeriesSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (HistoryChart)d;

        if (e.OldValue is INotifyCollectionChanged oldIncc)
        {
            oldIncc.CollectionChanged -= chart.OnCollectionChanged;
        }

        chart.DetachSeriesHandlers();

        if (e.NewValue is INotifyCollectionChanged newIncc)
        {
            newIncc.CollectionChanged += chart.OnCollectionChanged;
        }

        chart.AttachSeriesHandlers(e.NewValue as IEnumerable);
        chart.Rebuild();
    }

    private static void OnPlotPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((HistoryChart)d).Rebuild();

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Detach all tracked series before reattaching so removed/cleared items never keep handlers.
        AttachSeriesHandlers(SeriesSource);
        Rebuild();
    }

    private void AttachSeriesHandlers(IEnumerable? source)
    {
        DetachSeriesHandlers();
        if (source is null)
        {
            return;
        }

        foreach (var item in source)
        {
            if (item is INotifyPropertyChanged inpc && !_subscribedSeries.Contains(inpc))
            {
                inpc.PropertyChanged += OnSeriesPropertyChanged;
                _subscribedSeries.Add(inpc);
            }
        }
    }

    private void DetachSeriesHandlers()
    {
        foreach (var inpc in _subscribedSeries)
        {
            inpc.PropertyChanged -= OnSeriesPropertyChanged;
        }

        _subscribedSeries.Clear();
    }

    private void OnSeriesPropertyChanged(object? sender, PropertyChangedEventArgs e) => Rebuild();

    private void OnThemeApplied(object? sender, EventArgs e) => Rebuild();

    private void Rebuild()
    {
        if (_plot is null)
        {
            return;
        }

        var plot = _plot.Plot;
        plot.Clear();
        _crosshair = null;

        var hasNamedSeries = false;

        if (SeriesSource is not null)
        {
            foreach (var item in SeriesSource)
            {
                if (item is not ChartSeries series ||
                    series.X is null || series.Y is null)
                {
                    continue;
                }

                var count = Math.Min(series.X.Length, series.Y.Length);
                if (count == 0)
                {
                    continue;
                }

                var xs = series.X.Length == count ? series.X : series.X[..count];
                var ys = series.Y.Length == count ? series.Y : series.Y[..count];
                var line = plot.Add.ScatterLine(xs, ys);
                line.LineWidth = (float)series.LineWidth;

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

        plot.Legend.IsVisible = ShowLegend && hasNamedSeries;
        plot.XLabel(XLabel);
        plot.YLabel(YLabel);
        plot.Axes.AutoScale();
        _plot.Refresh();
    }

    private void OnPlotMouseMove(object sender, MouseEventArgs e)
    {
        if (_plot is null || !ShowCrosshair)
        {
            return;
        }

        var plot = _plot.Plot;

        if (_crosshair is null)
        {
            _crosshair = plot.Add.Crosshair(0, 0);
            var accent = ChartHelpers.GetTokenColor("Wb.Color.Accent.Default", "#007ACC");
            _crosshair.LineColor = accent.WithOpacity(.7);
            _crosshair.TextColor = ChartHelpers.GetTokenColor("Wb.Color.Text.OnAccent", "#FFFFFF");
            _crosshair.TextBackgroundColor = accent;
        }

        var position = e.GetPosition(_plot);
        var pixel = new ScottPlot.Pixel(
            position.X * _plot.DisplayScale,
            position.Y * _plot.DisplayScale);
        var coordinates = plot.GetCoordinates(pixel);

        _crosshair.IsVisible = true;
        _crosshair.Position = coordinates;
        _crosshair.VerticalLine.Text = $"{coordinates.X:G5}";
        _crosshair.HorizontalLine.Text = $"{coordinates.Y:G5}";
        _plot.Refresh();
    }

    private void OnPlotMouseLeave(object sender, MouseEventArgs e)
    {
        if (_plot is null || _crosshair is null)
        {
            return;
        }

        _crosshair.IsVisible = false;
        _plot.Refresh();
    }
}
