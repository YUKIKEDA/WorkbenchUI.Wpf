using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Conversion model mapping physical scalar values to colors.
/// Combines a <see cref="Controls.ColorMap"/> with a value range (Minimum/Maximum), optional logarithmic scaling,
/// discrete level quantization, and out-of-range/NaN color handling.
/// UI-independent for unit testing; <see cref="ColorMapLegend"/> simply renders this model.
/// </summary>
public sealed class ColorScale : INotifyPropertyChanged
{
    private ColorMap _colorMap = ColorMap.Jet;
    private double _minimum;
    private double _maximum = 1.0;
    private bool _isLogarithmic;
    private int? _levelCount;
    private Color? _belowRangeColor;
    private Color? _aboveRangeColor;
    private Color _naNColor = Colors.Transparent;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets or sets the color map used for sampling colors.</summary>
    public ColorMap ColorMap
    {
        get => _colorMap;
        set => SetField(ref _colorMap, value);
    }

    /// <summary>Gets or sets the lower bound of the value range.</summary>
    public double Minimum
    {
        get => _minimum;
        set => SetField(ref _minimum, value);
    }

    /// <summary>Gets or sets the upper bound of the value range.</summary>
    public double Maximum
    {
        get => _maximum;
        set => SetField(ref _maximum, value);
    }

    /// <summary>Gets or sets whether logarithmic scaling is used. Effective only when both Minimum and Maximum are positive (otherwise falls back to linear).</summary>
    public bool IsLogarithmic
    {
        get => _isLogarithmic;
        set => SetField(ref _isLogarithmic, value);
    }

    /// <summary>Gets or sets the number of discrete color bands. <see langword="null"/> produces a continuous gradient.</summary>
    public int? LevelCount
    {
        get => _levelCount;
        set => SetField(ref _levelCount, value);
    }

    /// <summary>Gets or sets the color used for values below Minimum. When <see langword="null"/>, values clamp to the lower end of the color map.</summary>
    public Color? BelowRangeColor
    {
        get => _belowRangeColor;
        set => SetField(ref _belowRangeColor, value);
    }

    /// <summary>Gets or sets the color used for values above Maximum. When <see langword="null"/>, values clamp to the upper end of the color map.</summary>
    public Color? AboveRangeColor
    {
        get => _aboveRangeColor;
        set => SetField(ref _aboveRangeColor, value);
    }

    /// <summary>Gets or sets the color used for <see cref="double.NaN"/> values (defaults to transparent).</summary>
    public Color NaNColor
    {
        get => _naNColor;
        set => SetField(ref _naNColor, value);
    }

    /// <summary>
    /// Creates a copy of all settings to support dialog Apply/Cancel workflows
    /// (edit the clone and write changes back via <see cref="CopyFrom"/> on OK).
    /// </summary>
    public ColorScale Clone() => new()
    {
        ColorMap = _colorMap,
        Minimum = _minimum,
        Maximum = _maximum,
        IsLogarithmic = _isLogarithmic,
        LevelCount = _levelCount,
        BelowRangeColor = _belowRangeColor,
        AboveRangeColor = _aboveRangeColor,
        NaNColor = _naNColor,
    };

    /// <summary>Copies all settings from another instance (raising notifications only for changed properties).</summary>
    public void CopyFrom(ColorScale other)
    {
        ArgumentNullException.ThrowIfNull(other);
        ColorMap = other._colorMap;
        Minimum = other._minimum;
        Maximum = other._maximum;
        IsLogarithmic = other._isLogarithmic;
        LevelCount = other._levelCount;
        BelowRangeColor = other._belowRangeColor;
        AboveRangeColor = other._aboveRangeColor;
        NaNColor = other._naNColor;
    }

    private bool UseLog => _isLogarithmic && _minimum > 0 && _maximum > 0;

    /// <summary>Normalizes a value into the [0, 1] range without clamping out-of-range values.</summary>
    public double Normalize(double value)
    {
        if (UseLog)
        {
            var logMin = Math.Log10(_minimum);
            var logMax = Math.Log10(_maximum);
            return logMax == logMin ? 0.0 : (Math.Log10(value) - logMin) / (logMax - logMin);
        }

        var range = _maximum - _minimum;
        return range == 0.0 ? 0.0 : (value - _minimum) / range;
    }

    /// <summary>Converts a normalized position <paramref name="t"/> (0–1) back into the value range (used for legend tick labels).</summary>
    public double Denormalize(double t)
    {
        if (UseLog)
        {
            var logMin = Math.Log10(_minimum);
            var logMax = Math.Log10(_maximum);
            return Math.Pow(10.0, logMin + t * (logMax - logMin));
        }

        return _minimum + t * (_maximum - _minimum);
    }

    /// <summary>Returns the color corresponding to <paramref name="value"/>, accounting for discrete levels, logarithmic scaling, out-of-range values, and NaN.</summary>
    public Color GetColor(double value)
    {
        if (double.IsNaN(value))
        {
            return _naNColor;
        }

        var t = Normalize(value);
        if (t < 0.0)
        {
            return _belowRangeColor ?? Sample(0.0);
        }

        if (t > 1.0)
        {
            return _aboveRangeColor ?? Sample(1.0);
        }

        return Sample(t);
    }

    /// <summary>Samples a color at normalized position <paramref name="t"/>, applying discrete level quantization if configured.</summary>
    public Color Sample(double t)
    {
        if (_levelCount is int levels and > 0)
        {
            var band = Math.Min((int)(t * levels), levels - 1);
            return _colorMap.GetColor((band + 0.5) / levels);
        }

        return _colorMap.GetColor(t);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
