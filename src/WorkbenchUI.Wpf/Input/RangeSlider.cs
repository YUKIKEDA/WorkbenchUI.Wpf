using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Dual-thumb slider for selecting a value range (<see cref="LowerValue"/> to <see cref="UpperValue"/>).
/// In addition to dragging the endpoint thumbs, dragging the center range bar shifts both endpoints
/// together while preserving the selected span (useful for sliding contour ranges).
/// </summary>
[TemplatePart(Name = PartCanvas, Type = typeof(Canvas))]
[TemplatePart(Name = PartLowerThumb, Type = typeof(Thumb))]
[TemplatePart(Name = PartUpperThumb, Type = typeof(Thumb))]
[TemplatePart(Name = PartRangeThumb, Type = typeof(Thumb))]
public class RangeSlider : Control
{
    private const string PartCanvas = "PART_Canvas";
    private const string PartLowerThumb = "PART_LowerThumb";
    private const string PartUpperThumb = "PART_UpperThumb";
    private const string PartRangeThumb = "PART_RangeThumb";

    private Canvas? _canvas;
    private Thumb? _lowerThumb;
    private Thumb? _upperThumb;
    private Thumb? _rangeThumb;

    static RangeSlider()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RangeSlider), new FrameworkPropertyMetadata(typeof(RangeSlider)));
    }

    /// <summary>Initializes a new instance of the <see cref="RangeSlider"/> class.</summary>
    public RangeSlider()
    {
        // Thumb ActualWidth is 0 before initial layout completes; reposition after Loaded
        Loaded += (_, _) => UpdateThumbPositions();
    }

    #region Dependency properties

    /// <summary>Identifies the <see cref="Minimum"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(RangeSlider),
        new FrameworkPropertyMetadata(0.0, OnRangeChanged));

    /// <summary>Gets or sets the minimum possible value of the slider.</summary>
    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>Identifies the <see cref="Maximum"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(RangeSlider),
        new FrameworkPropertyMetadata(1.0, OnRangeChanged));

    /// <summary>Gets or sets the maximum possible value of the slider.</summary>
    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>Identifies the <see cref="LowerValue"/> dependency property.</summary>
    public static readonly DependencyProperty LowerValueProperty = DependencyProperty.Register(
        nameof(LowerValue), typeof(double), typeof(RangeSlider),
        new FrameworkPropertyMetadata(0.0,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceLowerValue));

    /// <summary>Gets or sets the lower bound of the selected range.</summary>
    public double LowerValue
    {
        get => (double)GetValue(LowerValueProperty);
        set => SetValue(LowerValueProperty, value);
    }

    /// <summary>Identifies the <see cref="UpperValue"/> dependency property.</summary>
    public static readonly DependencyProperty UpperValueProperty = DependencyProperty.Register(
        nameof(UpperValue), typeof(double), typeof(RangeSlider),
        new FrameworkPropertyMetadata(1.0,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceUpperValue));

    /// <summary>Gets or sets the upper bound of the selected range.</summary>
    public double UpperValue
    {
        get => (double)GetValue(UpperValueProperty);
        set => SetValue(UpperValueProperty, value);
    }

    /// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(RangeSlider),
        new FrameworkPropertyMetadata(Orientation.Horizontal));

    /// <summary>Gets or sets the orientation of the slider.</summary>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    #endregion

    private static object CoerceLowerValue(DependencyObject d, object baseValue)
    {
        var slider = (RangeSlider)d;
        var value = (double)baseValue;
        return Math.Clamp(value, slider.Minimum, Math.Min(slider.UpperValue, slider.Maximum));
    }

    private static object CoerceUpperValue(DependencyObject d, object baseValue)
    {
        var slider = (RangeSlider)d;
        var value = (double)baseValue;
        return Math.Clamp(value, Math.Max(slider.LowerValue, slider.Minimum), slider.Maximum);
    }

    private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var slider = (RangeSlider)d;
        slider.CoerceValue(LowerValueProperty);
        slider.CoerceValue(UpperValueProperty);
        slider.UpdateThumbPositions();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var slider = (RangeSlider)d;

        // Re-coerce the opposite bound when one bound moves past the other
        if (e.Property == UpperValueProperty)
        {
            slider.CoerceValue(LowerValueProperty);
        }
        else
        {
            slider.CoerceValue(UpperValueProperty);
        }

        slider.UpdateThumbPositions();
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_lowerThumb is not null)
        {
            _lowerThumb.DragDelta -= OnLowerDragDelta;
        }

        if (_upperThumb is not null)
        {
            _upperThumb.DragDelta -= OnUpperDragDelta;
        }

        if (_rangeThumb is not null)
        {
            _rangeThumb.DragDelta -= OnRangeDragDelta;
        }

        if (_canvas is not null)
        {
            _canvas.SizeChanged -= OnCanvasSizeChanged;
        }

        base.OnApplyTemplate();

        _canvas = GetTemplateChild(PartCanvas) as Canvas;
        _lowerThumb = GetTemplateChild(PartLowerThumb) as Thumb;
        _upperThumb = GetTemplateChild(PartUpperThumb) as Thumb;
        _rangeThumb = GetTemplateChild(PartRangeThumb) as Thumb;

        if (_lowerThumb is not null)
        {
            _lowerThumb.DragDelta += OnLowerDragDelta;
        }

        if (_upperThumb is not null)
        {
            _upperThumb.DragDelta += OnUpperDragDelta;
        }

        if (_rangeThumb is not null)
        {
            _rangeThumb.DragDelta += OnRangeDragDelta;
        }

        if (_canvas is not null)
        {
            _canvas.SizeChanged += OnCanvasSizeChanged;
        }

        UpdateThumbPositions();
    }

    private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e) => UpdateThumbPositions();

    private void OnLowerDragDelta(object sender, DragDeltaEventArgs e) =>
        SetCurrentValue(LowerValueProperty, LowerValue + DragToValueDelta(e));

    private void OnUpperDragDelta(object sender, DragDeltaEventArgs e) =>
        SetCurrentValue(UpperValueProperty, UpperValue + DragToValueDelta(e));

    private void OnRangeDragDelta(object sender, DragDeltaEventArgs e)
    {
        // Shift both endpoints together preserving span, stopping at the track bounds.
        var delta = Math.Clamp(DragToValueDelta(e), Minimum - LowerValue, Maximum - UpperValue);
        if (delta == 0)
        {
            return;
        }

        // Update the leading edge first so coercion does not clamp against the trailing edge
        if (delta > 0)
        {
            SetCurrentValue(UpperValueProperty, UpperValue + delta);
            SetCurrentValue(LowerValueProperty, LowerValue + delta);
        }
        else
        {
            SetCurrentValue(LowerValueProperty, LowerValue + delta);
            SetCurrentValue(UpperValueProperty, UpperValue + delta);
        }
    }

    /// <summary>Converts a drag delta in pixels to a value delta (bottom is minimum when vertical).</summary>
    private double DragToValueDelta(DragDeltaEventArgs e)
    {
        var usable = UsableLength();
        if (usable <= 0)
        {
            return 0;
        }

        var pixels = Orientation == Orientation.Horizontal ? e.HorizontalChange : -e.VerticalChange;
        return pixels / usable * (Maximum - Minimum);
    }

    /// <summary>Returns the travel distance of a thumb center (track length minus one thumb size).</summary>
    private double UsableLength()
    {
        if (_canvas is null || _lowerThumb is null)
        {
            return 0;
        }

        var thumbSize = Orientation == Orientation.Horizontal
            ? _lowerThumb.ActualWidth
            : _lowerThumb.ActualHeight;
        var length = Orientation == Orientation.Horizontal
            ? _canvas.ActualWidth
            : _canvas.ActualHeight;
        return Math.Max(0, length - thumbSize);
    }

    private void UpdateThumbPositions()
    {
        if (_canvas is null || _lowerThumb is null || _upperThumb is null || _rangeThumb is null)
        {
            return;
        }

        var usable = UsableLength();
        var range = Maximum - Minimum;
        var tLower = range <= 0 ? 0 : (LowerValue - Minimum) / range;
        var tUpper = range <= 0 ? 0 : (UpperValue - Minimum) / range;

        if (Orientation == Orientation.Horizontal)
        {
            var yCenter = (_canvas.ActualHeight - _lowerThumb.ActualHeight) / 2;
            var xLower = tLower * usable;
            var xUpper = tUpper * usable;

            Canvas.SetLeft(_lowerThumb, xLower);
            Canvas.SetTop(_lowerThumb, yCenter);
            Canvas.SetLeft(_upperThumb, xUpper);
            Canvas.SetTop(_upperThumb, yCenter);

            // Range bar spans between thumb centers across the full track height for easier grabbing
            var halfThumb = _lowerThumb.ActualWidth / 2;
            Canvas.SetLeft(_rangeThumb, xLower + halfThumb);
            Canvas.SetTop(_rangeThumb, 0);
            _rangeThumb.Width = Math.Max(0, xUpper - xLower);
            _rangeThumb.Height = _canvas.ActualHeight;
        }
        else
        {
            var xCenter = (_canvas.ActualWidth - _lowerThumb.ActualWidth) / 2;

            // Vertical orientation places minimum at the bottom (matching standard Slider)
            var yLower = (1 - tLower) * usable;
            var yUpper = (1 - tUpper) * usable;

            Canvas.SetLeft(_lowerThumb, xCenter);
            Canvas.SetTop(_lowerThumb, yLower);
            Canvas.SetLeft(_upperThumb, xCenter);
            Canvas.SetTop(_upperThumb, yUpper);

            var halfThumb = _lowerThumb.ActualHeight / 2;
            Canvas.SetLeft(_rangeThumb, 0);
            Canvas.SetTop(_rangeThumb, yUpper + halfThumb);
            _rangeThumb.Width = _canvas.ActualWidth;
            _rangeThumb.Height = Math.Max(0, yLower - yUpper);
        }
    }
}
