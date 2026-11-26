using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Vertical color map legend that renders a <see cref="ColorScale"/> model.
/// Draws the color bar with a gradient brush (or discrete bands when level count is set)
/// and places value labels at evenly spaced normalized tick positions (even on a logarithmic scale,
/// ticks remain evenly spaced while the displayed values follow a logarithmic progression).
/// </summary>
[TemplatePart(Name = PartTickHost, Type = typeof(Canvas))]
public class ColorMapLegend : Control
{
    private const string PartTickHost = "PART_TickHost";

    private Canvas? _tickHost;

    static ColorMapLegend()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ColorMapLegend), new FrameworkPropertyMetadata(typeof(ColorMapLegend)));
    }

    /// <summary>Identifies the <see cref="Scale"/> dependency property.</summary>
    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
        nameof(Scale), typeof(ColorScale), typeof(ColorMapLegend),
        new PropertyMetadata(null, OnScaleChanged));

    /// <summary>Gets or sets the value-to-color conversion model to render. Redraws automatically when properties change.</summary>
    public ColorScale? Scale
    {
        get => (ColorScale?)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    /// <summary>Identifies the <see cref="TickCount"/> dependency property.</summary>
    public static readonly DependencyProperty TickCountProperty = DependencyProperty.Register(
        nameof(TickCount), typeof(int), typeof(ColorMapLegend),
        new PropertyMetadata(5, OnAppearanceChanged), v => (int)v >= 2);

    /// <summary>Gets or sets the number of tick labels (minimum 2, including both endpoints).</summary>
    public int TickCount
    {
        get => (int)GetValue(TickCountProperty);
        set => SetValue(TickCountProperty, value);
    }

    /// <summary>Identifies the <see cref="LabelFormat"/> dependency property.</summary>
    public static readonly DependencyProperty LabelFormatProperty = DependencyProperty.Register(
        nameof(LabelFormat), typeof(string), typeof(ColorMapLegend),
        new PropertyMetadata("G4", OnAppearanceChanged));

    /// <summary>Gets or sets the numeric format string for tick labels.</summary>
    public string LabelFormat
    {
        get => (string)GetValue(LabelFormatProperty);
        set => SetValue(LabelFormatProperty, value);
    }

    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(ColorMapLegend), new PropertyMetadata(null));

    /// <summary>Gets or sets the legend title (e.g., "Stress [MPa]"). Hidden when <see langword="null"/>.</summary>
    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    private static readonly DependencyPropertyKey LegendBrushPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(LegendBrush), typeof(Brush), typeof(ColorMapLegend), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="LegendBrush"/> dependency property.</summary>
    public static readonly DependencyProperty LegendBrushProperty = LegendBrushPropertyKey.DependencyProperty;

    /// <summary>Gets the brush used by the template to draw the color bar (read-only).</summary>
    public Brush? LegendBrush => (Brush?)GetValue(LegendBrushProperty);

    private static void OnScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var legend = (ColorMapLegend)d;
        if (e.OldValue is ColorScale oldScale)
        {
            oldScale.PropertyChanged -= legend.OnScalePropertyChanged;
        }

        if (e.NewValue is ColorScale newScale)
        {
            newScale.PropertyChanged += legend.OnScalePropertyChanged;
        }

        legend.Refresh();
    }

    private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((ColorMapLegend)d).Refresh();

    private void OnScalePropertyChanged(object? sender, PropertyChangedEventArgs e) => Refresh();

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_tickHost is not null)
        {
            _tickHost.SizeChanged -= OnTickHostSizeChanged;
        }

        base.OnApplyTemplate();
        _tickHost = GetTemplateChild(PartTickHost) as Canvas;
        if (_tickHost is not null)
        {
            _tickHost.SizeChanged += OnTickHostSizeChanged;
        }

        Refresh();
    }

    private void OnTickHostSizeChanged(object sender, SizeChangedEventArgs e) => RebuildTicks();

    private void Refresh()
    {
        RebuildBrush();
        RebuildTicks();
    }

    private void RebuildBrush()
    {
        var scale = Scale;
        if (scale is null)
        {
            SetValue(LegendBrushPropertyKey, null);
            return;
        }

        // Vertical gradient with t = 0 at the bottom
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 1),
            EndPoint = new Point(0, 0),
        };

        if (scale.LevelCount is int levels and > 0)
        {
            for (var i = 0; i < levels; i++)
            {
                var color = scale.Sample((i + 0.5) / levels);
                brush.GradientStops.Add(new GradientStop(color, (double)i / levels));
                brush.GradientStops.Add(new GradientStop(color, (double)(i + 1) / levels));
            }
        }
        else
        {
            foreach (var (offset, color) in scale.ColorMap.Stops)
            {
                brush.GradientStops.Add(new GradientStop(color, offset));
            }
        }

        brush.Freeze();
        SetValue(LegendBrushPropertyKey, brush);
    }

    private void RebuildTicks()
    {
        if (_tickHost is null)
        {
            return;
        }

        _tickHost.Children.Clear();

        var scale = Scale;
        var height = _tickHost.ActualHeight;
        if (scale is null || height <= 0)
        {
            return;
        }

        var count = TickCount;
        for (var i = 0; i < count; i++)
        {
            var t = 1.0 - (double)i / (count - 1); // t = 1 at the top
            var y = (1.0 - t) * height;
            var value = scale.Denormalize(t);

            var tickLine = new Rectangle { Width = 5, Height = 1 };
            tickLine.SetResourceReference(Shape.FillProperty, "Wb.Brush.Border.Strong");
            Canvas.SetLeft(tickLine, 0);
            Canvas.SetTop(tickLine, Math.Clamp(y - 0.5, 0, height - 1));
            _tickHost.Children.Add(tickLine);

            var label = new TextBlock
            {
                Text = value.ToString(LabelFormat, CultureInfo.CurrentCulture),
            };
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(label, 8);
            Canvas.SetTop(label, Math.Clamp(
                y - label.DesiredSize.Height / 2, 0, Math.Max(0, height - label.DesiredSize.Height)));
            _tickHost.Children.Add(label);
        }
    }
}
