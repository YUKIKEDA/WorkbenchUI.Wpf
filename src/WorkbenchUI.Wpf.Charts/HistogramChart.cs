using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Charts;

/// <summary>
/// Histogram chart for displaying frequency or probability distributions (such as stress distributions).
/// Automatically bins and plots numeric items provided via <see cref="Values"/> using <see cref="BinCount"/>.
/// </summary>
[TemplatePart(Name = PartPlot, Type = typeof(WorkbenchPlot))]
public class HistogramChart : Control
{
    private const string PartPlot = "PART_Plot";

    /// <summary>Identifies the <see cref="Values"/> dependency property.</summary>
    public static readonly DependencyProperty ValuesProperty =
        DependencyProperty.Register(
            nameof(Values), typeof(IEnumerable), typeof(HistogramChart),
            new PropertyMetadata(null, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="BinCount"/> dependency property.</summary>
    public static readonly DependencyProperty BinCountProperty =
        DependencyProperty.Register(
            nameof(BinCount), typeof(int), typeof(HistogramChart),
            new PropertyMetadata(20, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="Normalize"/> dependency property.</summary>
    public static readonly DependencyProperty NormalizeProperty =
        DependencyProperty.Register(
            nameof(Normalize), typeof(bool), typeof(HistogramChart),
            new PropertyMetadata(false, OnPlotPropertyChanged));

    /// <summary>Identifies the <see cref="XLabel"/> dependency property.</summary>
    public static readonly DependencyProperty XLabelProperty =
        DependencyProperty.Register(
            nameof(XLabel), typeof(string), typeof(HistogramChart),
            new PropertyMetadata("", OnPlotPropertyChanged));

    private WorkbenchPlot? _plot;

    static HistogramChart()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(HistogramChart), new FrameworkPropertyMetadata(typeof(HistogramChart)));
    }

    /// <summary>Sequence of numeric values (elements convertible to <see cref="double"/>) to bin.</summary>
    public IEnumerable? Values
    {
        get => (IEnumerable?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    /// <summary>Number of histogram bins (default is 20).</summary>
    public int BinCount
    {
        get => (int)GetValue(BinCountProperty);
        set => SetValue(BinCountProperty, value);
    }

    /// <summary>When <see langword="true"/>, normalizes bar heights to probability density (default is <see langword="false"/> for raw counts).</summary>
    public bool Normalize
    {
        get => (bool)GetValue(NormalizeProperty);
        set => SetValue(NormalizeProperty, value);
    }

    /// <summary>Horizontal axis label.</summary>
    public string XLabel
    {
        get => (string)GetValue(XLabelProperty);
        set => SetValue(XLabelProperty, value);
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

    private static void OnPlotPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((HistogramChart)d).Rebuild();

    private void OnThemeApplied(object? sender, EventArgs e) => Rebuild();

    private void Rebuild()
    {
        if (_plot is null)
        {
            return;
        }

        var plot = _plot.Plot;
        plot.Clear();

        var values = CollectValues();
        var binCount = Math.Max(1, BinCount);

        if (values.Length > 0)
        {
            var min = values.Min();
            var max = values.Max();

            // Ensure a non-zero span when all values are identical so a single bar can still be rendered.
            var span = max - min;
            if (span <= 0)
            {
                span = Math.Abs(min) > 0 ? Math.Abs(min) * 0.1 : 1;
                min -= span / 2;
                max += span / 2;
            }

            var binWidth = span / binCount;
            var counts = new double[binCount];

            foreach (var value in values)
            {
                var index = (int)((value - min) / binWidth);
                if (index >= binCount)
                {
                    index = binCount - 1; // Include values equal to max in the last bin.
                }
                else if (index < 0)
                {
                    index = 0;
                }

                counts[index]++;
            }

            if (Normalize)
            {
                var total = values.Length * binWidth;
                for (var i = 0; i < counts.Length; i++)
                {
                    counts[i] /= total;
                }
            }

            var centers = new double[binCount];
            for (var i = 0; i < binCount; i++)
            {
                centers[i] = min + binWidth * (i + 0.5);
            }

            var bars = plot.Add.Bars(centers, counts);
            var accent = ChartHelpers.GetTokenColor("Wb.Color.Accent.Default", "#007ACC");

            foreach (var bar in bars.Bars)
            {
                bar.Size = binWidth;
                bar.FillColor = accent.WithOpacity(.75);
                bar.LineColor = accent;
                bar.LineWidth = 1;
            }
        }

        plot.XLabel(XLabel);
        plot.YLabel(Normalize ? "Density" : "Count");
        plot.Axes.AutoScale();
        plot.Axes.Margins(bottom: 0);
        _plot.Refresh();
    }

    private double[] CollectValues()
    {
        if (Values is null)
        {
            return [];
        }

        var list = new List<double>();
        foreach (var item in Values)
        {
            if (item is null)
            {
                continue;
            }

            var value = Convert.ToDouble(item, System.Globalization.CultureInfo.InvariantCulture);
            if (!double.IsNaN(value) && !double.IsInfinity(value))
            {
                list.Add(value);
            }
        }

        return [.. list];
    }
}
