using System.Windows;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Composite control for entering X, Y, and Z vector components using three <see cref="NumericBox"/> inputs.
/// Exposes <see cref="X"/>, <see cref="Y"/>, and <see cref="Z"/> as individual nullable <see cref="double"/> dependency properties
/// (where <see langword="null"/> means blank/unset), while sharing <see cref="UnitProvider"/>, range bounds, and <see cref="Increment"/> across all three axes.
/// </summary>
public class Vector3Box : Control
{
    static Vector3Box()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(Vector3Box), new FrameworkPropertyMetadata(typeof(Vector3Box)));
    }

    /// <summary>Identifies the <see cref="X"/> dependency property.</summary>
    public static readonly DependencyProperty XProperty = DependencyProperty.Register(
        nameof(X), typeof(double?), typeof(Vector3Box),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Gets or sets the X component in base units, or <see langword="null"/> when empty.</summary>
    public double? X
    {
        get => (double?)GetValue(XProperty);
        set => SetValue(XProperty, value);
    }

    /// <summary>Identifies the <see cref="Y"/> dependency property.</summary>
    public static readonly DependencyProperty YProperty = DependencyProperty.Register(
        nameof(Y), typeof(double?), typeof(Vector3Box),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Gets or sets the Y component in base units, or <see langword="null"/> when empty.</summary>
    public double? Y
    {
        get => (double?)GetValue(YProperty);
        set => SetValue(YProperty, value);
    }

    /// <summary>Identifies the <see cref="Z"/> dependency property.</summary>
    public static readonly DependencyProperty ZProperty = DependencyProperty.Register(
        nameof(Z), typeof(double?), typeof(Vector3Box),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Gets or sets the Z component in base units, or <see langword="null"/> when empty.</summary>
    public double? Z
    {
        get => (double?)GetValue(ZProperty);
        set => SetValue(ZProperty, value);
    }

    /// <summary>Identifies the <see cref="Minimum"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(Vector3Box), new PropertyMetadata(double.NegativeInfinity));

    /// <summary>Gets or sets the shared minimum value (in base units) for all three axes.</summary>
    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>Identifies the <see cref="Maximum"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(Vector3Box), new PropertyMetadata(double.PositiveInfinity));

    /// <summary>Gets or sets the shared maximum value (in base units) for all three axes.</summary>
    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>Identifies the <see cref="Increment"/> dependency property.</summary>
    public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register(
        nameof(Increment), typeof(double), typeof(Vector3Box), new PropertyMetadata(1.0));

    /// <summary>Gets or sets the step size (in display units) for spin buttons and keyboard stepping.</summary>
    public double Increment
    {
        get => (double)GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    /// <summary>Identifies the <see cref="Unit"/> dependency property.</summary>
    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
        nameof(Unit), typeof(string), typeof(Vector3Box), new PropertyMetadata(null));

    /// <summary>Gets or sets the display-only unit label (no conversion). <see cref="UnitProvider"/> takes precedence when set.</summary>
    public string? Unit
    {
        get => (string?)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    /// <summary>Identifies the <see cref="UnitProvider"/> dependency property.</summary>
    public static readonly DependencyProperty UnitProviderProperty = DependencyProperty.Register(
        nameof(UnitProvider), typeof(IUnitProvider), typeof(Vector3Box), new PropertyMetadata(null));

    /// <summary>Gets or sets the shared unit provider for converting between display and base units on all three axes.</summary>
    public IUnitProvider? UnitProvider
    {
        get => (IUnitProvider?)GetValue(UnitProviderProperty);
        set => SetValue(UnitProviderProperty, value);
    }

    /// <summary>Identifies the <see cref="Format"/> dependency property.</summary>
    public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(
        nameof(Format), typeof(string), typeof(Vector3Box), new PropertyMetadata("G"));

    /// <summary>Gets or sets the numeric format string used for display.</summary>
    public string Format
    {
        get => (string)GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    /// <summary>Identifies the <see cref="IsReadOnly"/> dependency property.</summary>
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly), typeof(bool), typeof(Vector3Box), new PropertyMetadata(false));

    /// <summary>Gets or sets whether the control is read-only.</summary>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    // Keep axis labels customizable via dependency properties
    /// <summary>Identifies the <see cref="XLabel"/> dependency property.</summary>
    public static readonly DependencyProperty XLabelProperty = DependencyProperty.Register(
        nameof(XLabel), typeof(string), typeof(Vector3Box), new PropertyMetadata("X"));

    /// <summary>Gets or sets the label displayed for the X axis.</summary>
    public string XLabel
    {
        get => (string)GetValue(XLabelProperty);
        set => SetValue(XLabelProperty, value);
    }

    /// <summary>Identifies the <see cref="YLabel"/> dependency property.</summary>
    public static readonly DependencyProperty YLabelProperty = DependencyProperty.Register(
        nameof(YLabel), typeof(string), typeof(Vector3Box), new PropertyMetadata("Y"));

    /// <summary>Gets or sets the label displayed for the Y axis.</summary>
    public string YLabel
    {
        get => (string)GetValue(YLabelProperty);
        set => SetValue(YLabelProperty, value);
    }

    /// <summary>Identifies the <see cref="ZLabel"/> dependency property.</summary>
    public static readonly DependencyProperty ZLabelProperty = DependencyProperty.Register(
        nameof(ZLabel), typeof(string), typeof(Vector3Box), new PropertyMetadata("Z"));

    /// <summary>Gets or sets the label displayed for the Z axis.</summary>
    public string ZLabel
    {
        get => (string)GetValue(ZLabelProperty);
        set => SetValue(ZLabelProperty, value);
    }
}
