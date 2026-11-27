using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Editing UI for a <see cref="ColorScale"/>. <see cref="ColorMapLegend"/> handles display, while this control handles configuration.
/// <list type="bullet">
/// <item>Live-editing mode: directly mutates properties on the <see cref="Scale"/> instance so any legend or contour display bound to the same instance updates immediately.</item>
/// <item>For Apply/Cancel workflows, pass a copy created with <see cref="ColorScale.Clone"/> and write changes back on OK via <see cref="ColorScale.CopyFrom"/>.</item>
/// <item>Label strings can be customized via dependency properties (defaults are in English).</item>
/// </list>
/// </summary>
public class ColorScaleEditor : Control
{
    private bool _updating;

    static ColorScaleEditor()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ColorScaleEditor), new FrameworkPropertyMetadata(typeof(ColorScaleEditor)));
    }

    /// <summary>Initializes a new instance of the <see cref="ColorScaleEditor"/> class.</summary>
    public ColorScaleEditor()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Scale is { } scale)
        {
            scale.PropertyChanged -= OnScalePropertyChanged;
            scale.PropertyChanged += OnScalePropertyChanged;
        }

        SyncFromScale();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (Scale is { } scale)
        {
            scale.PropertyChanged -= OnScalePropertyChanged;
        }
    }

    #region Scale / ColorMaps / Units

    /// <summary>Identifies the <see cref="Scale"/> dependency property.</summary>
    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
        nameof(Scale), typeof(ColorScale), typeof(ColorScaleEditor),
        new PropertyMetadata(null, OnScaleChanged));

    /// <summary>Gets or sets the model being edited. Properties are updated directly for live feedback.</summary>
    public ColorScale? Scale
    {
        get => (ColorScale?)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    /// <summary>Identifies the <see cref="ColorMaps"/> dependency property.</summary>
    public static readonly DependencyProperty ColorMapsProperty = DependencyProperty.Register(
        nameof(ColorMaps), typeof(IEnumerable), typeof(ColorScaleEditor),
        new PropertyMetadata(ColorMap.BuiltIn));

    /// <summary>Gets or sets the selectable color map choices (defaults to the built-in presets).</summary>
    public IEnumerable ColorMaps
    {
        get => (IEnumerable)GetValue(ColorMapsProperty);
        set => SetValue(ColorMapsProperty, value);
    }

    /// <summary>Identifies the <see cref="UnitProvider"/> dependency property.</summary>
    public static readonly DependencyProperty UnitProviderProperty = DependencyProperty.Register(
        nameof(UnitProvider), typeof(IUnitProvider), typeof(ColorScaleEditor), new PropertyMetadata(null));

    /// <summary>Gets or sets the unit provider passed through to the Min/Max numeric inputs.</summary>
    public IUnitProvider? UnitProvider
    {
        get => (IUnitProvider?)GetValue(UnitProviderProperty);
        set => SetValue(UnitProviderProperty, value);
    }

    /// <summary>Identifies the <see cref="Format"/> dependency property.</summary>
    public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(
        nameof(Format), typeof(string), typeof(ColorScaleEditor), new PropertyMetadata("G4"));

    /// <summary>Gets or sets the numeric format string for the Min/Max inputs.</summary>
    public string Format
    {
        get => (string)GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    #endregion

    #region Intermediate editing dependency properties (bound by template)

    /// <summary>Identifies the <see cref="MinimumValue"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumValueProperty = DependencyProperty.Register(
        nameof(MinimumValue), typeof(double?), typeof(ColorScaleEditor),
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnEditValueChanged));

    /// <summary>Gets or sets the value in the Min input box. Committing <see langword="null"/> reverts to the model value.</summary>
    public double? MinimumValue
    {
        get => (double?)GetValue(MinimumValueProperty);
        set => SetValue(MinimumValueProperty, value);
    }

    /// <summary>Identifies the <see cref="MaximumValue"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumValueProperty = DependencyProperty.Register(
        nameof(MaximumValue), typeof(double?), typeof(ColorScaleEditor),
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnEditValueChanged));

    /// <summary>Gets or sets the value in the Max input box. Committing <see langword="null"/> reverts to the model value.</summary>
    public double? MaximumValue
    {
        get => (double?)GetValue(MaximumValueProperty);
        set => SetValue(MaximumValueProperty, value);
    }

    /// <summary>Identifies the <see cref="IsDiscrete"/> dependency property.</summary>
    public static readonly DependencyProperty IsDiscreteProperty = DependencyProperty.Register(
        nameof(IsDiscrete), typeof(bool), typeof(ColorScaleEditor),
        new FrameworkPropertyMetadata(false,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnEditValueChanged));

    /// <summary>Gets or sets whether discrete level quantization is enabled (maps to <see cref="ColorScale.LevelCount"/> being non-null).</summary>
    public bool IsDiscrete
    {
        get => (bool)GetValue(IsDiscreteProperty);
        set => SetValue(IsDiscreteProperty, value);
    }

    /// <summary>Identifies the <see cref="DiscreteLevels"/> dependency property.</summary>
    public static readonly DependencyProperty DiscreteLevelsProperty = DependencyProperty.Register(
        nameof(DiscreteLevels), typeof(double?), typeof(ColorScaleEditor),
        new FrameworkPropertyMetadata(10.0,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnEditValueChanged));

    /// <summary>Gets or sets the number of discrete levels (2–256).</summary>
    public double? DiscreteLevels
    {
        get => (double?)GetValue(DiscreteLevelsProperty);
        set => SetValue(DiscreteLevelsProperty, value);
    }

    /// <summary>Identifies the <see cref="ClampBelow"/> dependency property.</summary>
    public static readonly DependencyProperty ClampBelowProperty = DependencyProperty.Register(
        nameof(ClampBelow), typeof(bool), typeof(ColorScaleEditor),
        new FrameworkPropertyMetadata(true,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnEditValueChanged));

    /// <summary>Gets or sets whether values below Minimum clamp to the lower end of the color map (corresponds to <see cref="ColorScale.BelowRangeColor"/> being <see langword="null"/>).</summary>
    public bool ClampBelow
    {
        get => (bool)GetValue(ClampBelowProperty);
        set => SetValue(ClampBelowProperty, value);
    }

    /// <summary>Identifies the <see cref="BelowColor"/> dependency property.</summary>
    public static readonly DependencyProperty BelowColorProperty = DependencyProperty.Register(
        nameof(BelowColor), typeof(Color), typeof(ColorScaleEditor),
        new FrameworkPropertyMetadata(Colors.DarkGray,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnEditValueChanged));

    /// <summary>Gets or sets the color used for values below Minimum when clamping is disabled.</summary>
    public Color BelowColor
    {
        get => (Color)GetValue(BelowColorProperty);
        set => SetValue(BelowColorProperty, value);
    }

    /// <summary>Identifies the <see cref="ClampAbove"/> dependency property.</summary>
    public static readonly DependencyProperty ClampAboveProperty = DependencyProperty.Register(
        nameof(ClampAbove), typeof(bool), typeof(ColorScaleEditor),
        new FrameworkPropertyMetadata(true,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnEditValueChanged));

    /// <summary>Gets or sets whether values above Maximum clamp to the upper end of the color map (corresponds to <see cref="ColorScale.AboveRangeColor"/> being <see langword="null"/>).</summary>
    public bool ClampAbove
    {
        get => (bool)GetValue(ClampAboveProperty);
        set => SetValue(ClampAboveProperty, value);
    }

    /// <summary>Identifies the <see cref="AboveColor"/> dependency property.</summary>
    public static readonly DependencyProperty AboveColorProperty = DependencyProperty.Register(
        nameof(AboveColor), typeof(Color), typeof(ColorScaleEditor),
        new FrameworkPropertyMetadata(Colors.DarkGray,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnEditValueChanged));

    /// <summary>Gets or sets the color used for values above Maximum when clamping is disabled.</summary>
    public Color AboveColor
    {
        get => (Color)GetValue(AboveColorProperty);
        set => SetValue(AboveColorProperty, value);
    }

    private static readonly DependencyPropertyKey CanUseLogPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(CanUseLog), typeof(bool), typeof(ColorScaleEditor), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="CanUseLog"/> dependency property.</summary>
    public static readonly DependencyProperty CanUseLogProperty = CanUseLogPropertyKey.DependencyProperty;

    /// <summary>Gets whether logarithmic scaling is available (both Min and Max are positive; read-only).</summary>
    public bool CanUseLog => (bool)GetValue(CanUseLogProperty);

    #endregion

    #region Label strings (English defaults)

    /// <summary>Identifies the <see cref="ColorMapLabel"/> dependency property.</summary>
    public static readonly DependencyProperty ColorMapLabelProperty = DependencyProperty.Register(
        nameof(ColorMapLabel), typeof(string), typeof(ColorScaleEditor), new PropertyMetadata("Color map"));

    /// <summary>Gets or sets the label text for the color map selector.</summary>
    public string ColorMapLabel
    {
        get => (string)GetValue(ColorMapLabelProperty);
        set => SetValue(ColorMapLabelProperty, value);
    }

    /// <summary>Identifies the <see cref="MinimumLabel"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumLabelProperty = DependencyProperty.Register(
        nameof(MinimumLabel), typeof(string), typeof(ColorScaleEditor), new PropertyMetadata("Min"));

    /// <summary>Gets or sets the label text for the minimum value input.</summary>
    public string MinimumLabel
    {
        get => (string)GetValue(MinimumLabelProperty);
        set => SetValue(MinimumLabelProperty, value);
    }

    /// <summary>Identifies the <see cref="MaximumLabel"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumLabelProperty = DependencyProperty.Register(
        nameof(MaximumLabel), typeof(string), typeof(ColorScaleEditor), new PropertyMetadata("Max"));

    /// <summary>Gets or sets the label text for the maximum value input.</summary>
    public string MaximumLabel
    {
        get => (string)GetValue(MaximumLabelProperty);
        set => SetValue(MaximumLabelProperty, value);
    }

    /// <summary>Identifies the <see cref="DiscreteLevelsLabel"/> dependency property.</summary>
    public static readonly DependencyProperty DiscreteLevelsLabelProperty = DependencyProperty.Register(
        nameof(DiscreteLevelsLabel), typeof(string), typeof(ColorScaleEditor), new PropertyMetadata("Discrete levels"));

    /// <summary>Gets or sets the label text for the discrete levels toggle.</summary>
    public string DiscreteLevelsLabel
    {
        get => (string)GetValue(DiscreteLevelsLabelProperty);
        set => SetValue(DiscreteLevelsLabelProperty, value);
    }

    /// <summary>Identifies the <see cref="LogarithmicLabel"/> dependency property.</summary>
    public static readonly DependencyProperty LogarithmicLabelProperty = DependencyProperty.Register(
        nameof(LogarithmicLabel), typeof(string), typeof(ColorScaleEditor), new PropertyMetadata("Log scale"));

    /// <summary>Gets or sets the label text for the logarithmic scale toggle.</summary>
    public string LogarithmicLabel
    {
        get => (string)GetValue(LogarithmicLabelProperty);
        set => SetValue(LogarithmicLabelProperty, value);
    }

    /// <summary>Identifies the <see cref="LogDisabledHint"/> dependency property.</summary>
    public static readonly DependencyProperty LogDisabledHintProperty = DependencyProperty.Register(
        nameof(LogDisabledHint), typeof(string), typeof(ColorScaleEditor),
        new PropertyMetadata("Log scale requires positive Min and Max."));

    /// <summary>Gets or sets the tooltip/hint shown when logarithmic scaling is unavailable.</summary>
    public string LogDisabledHint
    {
        get => (string)GetValue(LogDisabledHintProperty);
        set => SetValue(LogDisabledHintProperty, value);
    }

    /// <summary>Identifies the <see cref="AdvancedLabel"/> dependency property.</summary>
    public static readonly DependencyProperty AdvancedLabelProperty = DependencyProperty.Register(
        nameof(AdvancedLabel), typeof(string), typeof(ColorScaleEditor), new PropertyMetadata("Advanced"));

    /// <summary>Gets or sets the header text for the advanced options expander.</summary>
    public string AdvancedLabel
    {
        get => (string)GetValue(AdvancedLabelProperty);
        set => SetValue(AdvancedLabelProperty, value);
    }

    /// <summary>Identifies the <see cref="BelowRangeLabel"/> dependency property.</summary>
    public static readonly DependencyProperty BelowRangeLabelProperty = DependencyProperty.Register(
        nameof(BelowRangeLabel), typeof(string), typeof(ColorScaleEditor), new PropertyMetadata("Below range"));

    /// <summary>Gets or sets the label text for the below-range color setting.</summary>
    public string BelowRangeLabel
    {
        get => (string)GetValue(BelowRangeLabelProperty);
        set => SetValue(BelowRangeLabelProperty, value);
    }

    /// <summary>Identifies the <see cref="AboveRangeLabel"/> dependency property.</summary>
    public static readonly DependencyProperty AboveRangeLabelProperty = DependencyProperty.Register(
        nameof(AboveRangeLabel), typeof(string), typeof(ColorScaleEditor), new PropertyMetadata("Above range"));

    /// <summary>Gets or sets the label text for the above-range color setting.</summary>
    public string AboveRangeLabel
    {
        get => (string)GetValue(AboveRangeLabelProperty);
        set => SetValue(AboveRangeLabelProperty, value);
    }

    /// <summary>Identifies the <see cref="NaNColorLabel"/> dependency property.</summary>
    public static readonly DependencyProperty NaNColorLabelProperty = DependencyProperty.Register(
        nameof(NaNColorLabel), typeof(string), typeof(ColorScaleEditor), new PropertyMetadata("NaN color"));

    /// <summary>Gets or sets the label text for the NaN color setting.</summary>
    public string NaNColorLabel
    {
        get => (string)GetValue(NaNColorLabelProperty);
        set => SetValue(NaNColorLabelProperty, value);
    }

    /// <summary>Identifies the <see cref="ClampLabel"/> dependency property.</summary>
    public static readonly DependencyProperty ClampLabelProperty = DependencyProperty.Register(
        nameof(ClampLabel), typeof(string), typeof(ColorScaleEditor), new PropertyMetadata("Clamp"));

    /// <summary>Gets or sets the label text for the clamp checkboxes.</summary>
    public string ClampLabel
    {
        get => (string)GetValue(ClampLabelProperty);
        set => SetValue(ClampLabelProperty, value);
    }

    #endregion

    #region Synchronization logic (Scale <-> intermediate DPs)

    private static void OnScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (ColorScaleEditor)d;
        if (e.OldValue is ColorScale oldScale)
        {
            oldScale.PropertyChanged -= editor.OnScalePropertyChanged;
        }

        if (e.NewValue is ColorScale newScale)
        {
            newScale.PropertyChanged += editor.OnScalePropertyChanged;
        }

        editor.SyncFromScale();
    }

    private void OnScalePropertyChanged(object? sender, PropertyChangedEventArgs e) => SyncFromScale();

    /// <summary>Syncs model values to intermediate dependency properties.</summary>
    private void SyncFromScale()
    {
        if (_updating)
        {
            return;
        }

        _updating = true;
        try
        {
            var scale = Scale;
            if (scale is null)
            {
                SetValue(CanUseLogPropertyKey, false);
                return;
            }

            SetCurrentValue(MinimumValueProperty, scale.Minimum);
            SetCurrentValue(MaximumValueProperty, scale.Maximum);
            SetCurrentValue(IsDiscreteProperty, scale.LevelCount is not null);
            if (scale.LevelCount is int levels)
            {
                SetCurrentValue(DiscreteLevelsProperty, (double)levels);
            }

            SetCurrentValue(ClampBelowProperty, scale.BelowRangeColor is null);
            if (scale.BelowRangeColor is Color below)
            {
                SetCurrentValue(BelowColorProperty, below);
            }

            SetCurrentValue(ClampAboveProperty, scale.AboveRangeColor is null);
            if (scale.AboveRangeColor is Color above)
            {
                SetCurrentValue(AboveColorProperty, above);
            }

            SetValue(CanUseLogPropertyKey, scale.Minimum > 0 && scale.Maximum > 0);
        }
        finally
        {
            _updating = false;
        }
    }

    /// <summary>Syncs intermediate dependency properties back to the model.</summary>
    private static void OnEditValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (ColorScaleEditor)d;
        if (editor._updating || editor.Scale is not ColorScale scale)
        {
            return;
        }

        editor._updating = true;
        try
        {
            if (e.Property == MinimumValueProperty || e.Property == MaximumValueProperty)
            {
                // Empty/null commit is treated as invalid input and reverted to the model value
                if (editor.MinimumValue is double min)
                {
                    scale.Minimum = min;
                }
                else
                {
                    editor.SetCurrentValue(MinimumValueProperty, scale.Minimum);
                }

                if (editor.MaximumValue is double max)
                {
                    scale.Maximum = max;
                }
                else
                {
                    editor.SetCurrentValue(MaximumValueProperty, scale.Maximum);
                }

                editor.SetValue(CanUseLogPropertyKey, scale.Minimum > 0 && scale.Maximum > 0);
            }
            else if (e.Property == IsDiscreteProperty || e.Property == DiscreteLevelsProperty)
            {
                if (editor.DiscreteLevels is not double levels)
                {
                    editor.SetCurrentValue(DiscreteLevelsProperty, (double)(scale.LevelCount ?? 10));
                    levels = (double)editor.DiscreteLevels!;
                }

                scale.LevelCount = editor.IsDiscrete ? (int)Math.Round(levels) : null;
            }
            else if (e.Property == ClampBelowProperty || e.Property == BelowColorProperty)
            {
                scale.BelowRangeColor = editor.ClampBelow ? null : editor.BelowColor;
            }
            else if (e.Property == ClampAboveProperty || e.Property == AboveColorProperty)
            {
                scale.AboveRangeColor = editor.ClampAbove ? null : editor.AboveColor;
            }
        }
        finally
        {
            editor._updating = false;
        }
    }

    #endregion
}
