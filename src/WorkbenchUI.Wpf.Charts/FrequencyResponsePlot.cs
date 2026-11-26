using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Charts;

/// <summary>
/// Frequency response (Bode plot) chart displaying magnitude in the upper panel and phase in the lower panel,
/// with synchronized logarithmic frequency horizontal axes.
/// Bind a collection of <see cref="FrequencyResponseSeries"/> instances to <see cref="SeriesSource"/>.
/// </summary>
[TemplatePart(Name = PartMagnitudePlot, Type = typeof(WorkbenchPlot))]
[TemplatePart(Name = PartPhasePlot, Type = typeof(WorkbenchPlot))]
public class FrequencyResponsePlot : Control
{
    private const string PartMagnitudePlot = "PART_MagnitudePlot";
    private const string PartPhasePlot = "PART_PhasePlot";

    /// <summary>Identifies the <see cref="SeriesSource"/> dependency property.</summary>
    public static readonly DependencyProperty SeriesSourceProperty =
        DependencyProperty.Register(
            nameof(SeriesSource), typeof(IEnumerable), typeof(FrequencyResponsePlot),
            new PropertyMetadata(null, OnSeriesSourceChanged));

    /// <summary>Identifies the <see cref="ShowPhase"/> dependency property.</summary>
    public static readonly DependencyProperty ShowPhaseProperty =
        DependencyProperty.Register(
            nameof(ShowPhase), typeof(bool), typeof(FrequencyResponsePlot),
            new PropertyMetadata(true, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="MagnitudeInDecibels"/> dependency property.</summary>
    public static readonly DependencyProperty MagnitudeInDecibelsProperty =
        DependencyProperty.Register(
            nameof(MagnitudeInDecibels), typeof(bool), typeof(FrequencyResponsePlot),
            new PropertyMetadata(true, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="XLabel"/> dependency property.</summary>
    public static readonly DependencyProperty XLabelProperty =
        DependencyProperty.Register(
            nameof(XLabel), typeof(string), typeof(FrequencyResponsePlot),
            new PropertyMetadata("Frequency [Hz]", OnPlotPropertyChanged));

    private WorkbenchPlot? _magnitudePlot;
    private WorkbenchPlot? _phasePlot;
    private bool _syncingAxes;

    static FrequencyResponsePlot()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(FrequencyResponsePlot), new FrameworkPropertyMetadata(typeof(FrequencyResponsePlot)));
    }

    /// <summary>Collection of <see cref="FrequencyResponseSeries"/> instances to display.</summary>
    public IEnumerable? SeriesSource
    {
        get => (IEnumerable?)GetValue(SeriesSourceProperty);
        set => SetValue(SeriesSourceProperty, value);
    }

    /// <summary>Whether to display the phase panel (default is <see langword="true"/>).</summary>
    public bool ShowPhase
    {
        get => (bool)GetValue(ShowPhaseProperty);
        set => SetValue(ShowPhaseProperty, value);
    }

    /// <summary>Whether to display magnitude in dB (<c>20 * log10</c>) (default is <see langword="true"/>; <see langword="false"/> displays linear magnitude).</summary>
    public bool MagnitudeInDecibels
    {
        get => (bool)GetValue(MagnitudeInDecibelsProperty);
        set => SetValue(MagnitudeInDecibelsProperty, value);
    }

    /// <summary>Horizontal axis label (default is <c>"Frequency [Hz]"</c>).</summary>
    public string XLabel
    {
        get => (string)GetValue(XLabelProperty);
        set => SetValue(XLabelProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_magnitudePlot is not null)
        {
            _magnitudePlot.ThemeApplied -= OnThemeApplied;
        }

        _magnitudePlot = GetTemplateChild(PartMagnitudePlot) as WorkbenchPlot;
        _phasePlot = GetTemplateChild(PartPhasePlot) as WorkbenchPlot;

        if (_magnitudePlot is not null)
        {
            _magnitudePlot.ThemeApplied += OnThemeApplied;
        }

        if (_magnitudePlot is not null && _phasePlot is not null)
        {
            // Synchronize the X-axis range between the magnitude and phase plots during zoom and pan.
            _magnitudePlot.Plot.RenderManager.AxisLimitsChanged +=
                (_, _) => SyncAxes(_magnitudePlot, _phasePlot);
            _phasePlot.Plot.RenderManager.AxisLimitsChanged +=
                (_, _) => SyncAxes(_phasePlot, _magnitudePlot);
        }

        Rebuild();
    }

    private void SyncAxes(WorkbenchPlot source, WorkbenchPlot target)
    {
        if (_syncingAxes || !ShowPhase)
        {
            return;
        }

        var sourceLimits = source.Plot.Axes.GetLimits();
        var targetLimits = target.Plot.Axes.GetLimits();

        if (sourceLimits.Left == targetLimits.Left && sourceLimits.Right == targetLimits.Right)
        {
            return;
        }

        _syncingAxes = true;
        try
        {
            target.Plot.Axes.SetLimitsX(sourceLimits.Left, sourceLimits.Right);
            target.Refresh();
        }
        finally
        {
            _syncingAxes = false;
        }
    }

    private static void OnSeriesSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (FrequencyResponsePlot)d;

        if (e.OldValue is INotifyCollectionChanged oldIncc)
        {
            oldIncc.CollectionChanged -= chart.OnCollectionChanged;
        }

        chart.DetachSeriesHandlers(e.OldValue as IEnumerable);

        if (e.NewValue is INotifyCollectionChanged newIncc)
        {
            newIncc.CollectionChanged += chart.OnCollectionChanged;
        }

        chart.AttachSeriesHandlers(e.NewValue as IEnumerable);
        chart.Rebuild();
    }

    private static void OnPlotPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((FrequencyResponsePlot)d).Rebuild();

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
            if (item is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged += OnSeriesPropertyChanged;
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
            if (item is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged -= OnSeriesPropertyChanged;
            }
        }
    }

    private void OnSeriesPropertyChanged(object? sender, PropertyChangedEventArgs e) => Rebuild();

    private void OnThemeApplied(object? sender, EventArgs e) => Rebuild();

    private void Rebuild()
    {
        if (_magnitudePlot is null)
        {
            return;
        }

        var magnitudePlot = _magnitudePlot.Plot;
        var phasePlot = _phasePlot?.Plot;

        magnitudePlot.Clear();
        phasePlot?.Clear();

        var hasNamedSeries = false;

        if (SeriesSource is not null)
        {
            var palette = WorkbenchChartTheme.GetSeriesPalette();
            var paletteIndex = 0;

            foreach (var item in SeriesSource)
            {
                if (item is not FrequencyResponseSeries series ||
                    series.Frequencies is null || series.Magnitudes is null ||
                    series.Frequencies.Length == 0)
                {
                    continue;
                }

                var color = series.Color is { } c
                    ? WorkbenchChartTheme.ToScottPlotColor(c)
                    : palette[paletteIndex % palette.Length];
                paletteIndex++;

                var logFreqs = new double[series.Frequencies.Length];
                for (var i = 0; i < logFreqs.Length; i++)
                {
                    logFreqs[i] = ChartHelpers.SafeLog10(series.Frequencies[i]);
                }

                var magnitudes = new double[series.Magnitudes.Length];
                for (var i = 0; i < magnitudes.Length; i++)
                {
                    magnitudes[i] = MagnitudeInDecibels
                        ? 20 * ChartHelpers.SafeLog10(series.Magnitudes[i])
                        : series.Magnitudes[i];
                }

                var magnitudeLine = magnitudePlot.Add.ScatterLine(logFreqs, magnitudes);
                magnitudeLine.LineWidth = 2;
                magnitudeLine.Color = color;

                if (!string.IsNullOrEmpty(series.Name))
                {
                    magnitudeLine.LegendText = series.Name;
                    hasNamedSeries = true;
                }

                if (phasePlot is not null && series.Phases is not null &&
                    series.Phases.Length == logFreqs.Length)
                {
                    var phaseLine = phasePlot.Add.ScatterLine(logFreqs, series.Phases);
                    phaseLine.LineWidth = 2;
                    phaseLine.Color = color;
                }
            }
        }

        var showPhase = ShowPhase && phasePlot is not null;

        ConfigureAxes(magnitudePlot, showXLabel: !showPhase);
        magnitudePlot.YLabel(MagnitudeInDecibels ? "Magnitude [dB]" : "Magnitude");
        magnitudePlot.Legend.IsVisible = hasNamedSeries;
        magnitudePlot.Axes.AutoScale();
        _magnitudePlot.Refresh();

        if (phasePlot is not null)
        {
            ConfigureAxes(phasePlot, showXLabel: true);
            phasePlot.YLabel("Phase [deg]");
            phasePlot.Axes.AutoScale();

            if (showPhase)
            {
                // Align the initial X-axis range with the magnitude plot.
                var limits = magnitudePlot.Axes.GetLimits();
                phasePlot.Axes.SetLimitsX(limits.Left, limits.Right);
            }

            _phasePlot!.Refresh();
        }
    }

    private void ConfigureAxes(ScottPlot.Plot plot, bool showXLabel)
    {
        plot.Axes.Bottom.TickGenerator = ChartHelpers.CreateLogTickGenerator();
        plot.XLabel(showXLabel ? XLabel : "");
    }
}
