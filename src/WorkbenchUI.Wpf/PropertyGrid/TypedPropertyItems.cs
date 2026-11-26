using System.Collections;
using System.Windows.Media;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>Text property item edited with a <c>TextBox</c>.</summary>
public class TextPropertyItem : PropertyItem
{
    private string? _value;

    /// <summary>Gets or sets the string value.</summary>
    public string? Value
    {
        get => _value;
        set => SetField(ref _value, value);
    }
}

/// <summary>Boolean property item edited with a <c>CheckBox</c>.</summary>
public class BoolPropertyItem : PropertyItem
{
    private bool _value;

    /// <summary>Gets or sets the boolean value.</summary>
    public bool Value
    {
        get => _value;
        set => SetField(ref _value, value);
    }
}

/// <summary>
/// Numeric property item edited with a <see cref="NumericBox"/>, supporting unit conversion (<see cref="IUnitProvider"/>), bounds, and step increment.
/// </summary>
public class NumericPropertyItem : PropertyItem
{
    private double? _value;
    private double _minimum = double.NegativeInfinity;
    private double _maximum = double.PositiveInfinity;
    private double _increment = 1.0;
    private string? _unit;
    private IUnitProvider? _unitProvider;
    private string _format = "G";

    /// <summary>Gets or sets the internal value in base units, or <see langword="null"/> when empty.</summary>
    public double? Value
    {
        get => _value;
        set => SetField(ref _value, value);
    }

    /// <summary>Gets or sets the minimum allowed value.</summary>
    public double Minimum
    {
        get => _minimum;
        set => SetField(ref _minimum, value);
    }

    /// <summary>Gets or sets the maximum allowed value.</summary>
    public double Maximum
    {
        get => _maximum;
        set => SetField(ref _maximum, value);
    }

    /// <summary>Gets or sets the step increment used when stepping the value.</summary>
    public double Increment
    {
        get => _increment;
        set => SetField(ref _increment, value);
    }

    /// <summary>Gets or sets the display-only unit label (no conversion). <see cref="UnitProvider"/> takes precedence if set.</summary>
    public string? Unit
    {
        get => _unit;
        set => SetField(ref _unit, value);
    }

    /// <summary>Gets or sets the unit conversion provider.</summary>
    public IUnitProvider? UnitProvider
    {
        get => _unitProvider;
        set => SetField(ref _unitProvider, value);
    }

    /// <summary>Gets or sets the numeric format string.</summary>
    public string Format
    {
        get => _format;
        set => SetField(ref _format, value);
    }
}

/// <summary>Choice property item edited with a <c>ComboBox</c>.</summary>
public class ChoicePropertyItem : PropertyItem
{
    private IEnumerable? _choices;
    private object? _value;

    /// <summary>Gets or sets the collection of selectable items.</summary>
    public IEnumerable? Choices
    {
        get => _choices;
        set => SetField(ref _choices, value);
    }

    /// <summary>Gets or sets the currently selected value.</summary>
    public object? Value
    {
        get => _value;
        set => SetField(ref _value, value);
    }
}

/// <summary>File or folder path property item edited with a <see cref="PathBox"/>.</summary>
public class PathPropertyItem : PropertyItem
{
    private string? _value;
    private PathBoxMode _mode = PathBoxMode.OpenFile;
    private string? _filter;
    private string? _dialogTitle;

    /// <summary>Gets or sets the selected path.</summary>
    public string? Value
    {
        get => _value;
        set => SetField(ref _value, value);
    }

    /// <summary>Gets or sets the dialog mode opened by the browse button.</summary>
    public PathBoxMode Mode
    {
        get => _mode;
        set => SetField(ref _mode, value);
    }

    /// <summary>Gets or sets the file dialog filter string.</summary>
    public string? Filter
    {
        get => _filter;
        set => SetField(ref _filter, value);
    }

    /// <summary>Gets or sets the browse dialog title.</summary>
    public string? DialogTitle
    {
        get => _dialogTitle;
        set => SetField(ref _dialogTitle, value);
    }
}

/// <summary>Color property item edited with a <see cref="ColorPicker"/>.</summary>
public class ColorPropertyItem : PropertyItem
{
    private Color _value = Colors.White;
    private bool _isAlphaEnabled = true;

    /// <summary>Gets or sets the selected color.</summary>
    public Color Value
    {
        get => _value;
        set => SetField(ref _value, value);
    }

    /// <summary>Gets or sets a value indicating whether alpha channel editing is enabled.</summary>
    public bool IsAlphaEnabled
    {
        get => _isAlphaEnabled;
        set => SetField(ref _isAlphaEnabled, value);
    }
}

/// <summary>XYZ vector property item edited with a <see cref="Vector3Box"/>.</summary>
public class Vector3PropertyItem : PropertyItem
{
    private double? _x;
    private double? _y;
    private double? _z;
    private double _minimum = double.NegativeInfinity;
    private double _maximum = double.PositiveInfinity;
    private double _increment = 1.0;
    private string? _unit;
    private IUnitProvider? _unitProvider;
    private string _format = "G";

    /// <summary>Gets or sets the X component in base units, or <see langword="null"/> when empty.</summary>
    public double? X
    {
        get => _x;
        set => SetField(ref _x, value);
    }

    /// <summary>Gets or sets the Y component in base units, or <see langword="null"/> when empty.</summary>
    public double? Y
    {
        get => _y;
        set => SetField(ref _y, value);
    }

    /// <summary>Gets or sets the Z component in base units, or <see langword="null"/> when empty.</summary>
    public double? Z
    {
        get => _z;
        set => SetField(ref _z, value);
    }

    /// <summary>Gets or sets the minimum allowed value for each component.</summary>
    public double Minimum
    {
        get => _minimum;
        set => SetField(ref _minimum, value);
    }

    /// <summary>Gets or sets the maximum allowed value for each component.</summary>
    public double Maximum
    {
        get => _maximum;
        set => SetField(ref _maximum, value);
    }

    /// <summary>Gets or sets the step increment used when stepping component values.</summary>
    public double Increment
    {
        get => _increment;
        set => SetField(ref _increment, value);
    }

    /// <summary>Gets or sets the shared unit label for all three axes. <see cref="UnitProvider"/> takes precedence if set.</summary>
    public string? Unit
    {
        get => _unit;
        set => SetField(ref _unit, value);
    }

    /// <summary>Gets or sets the unit conversion provider.</summary>
    public IUnitProvider? UnitProvider
    {
        get => _unitProvider;
        set => SetField(ref _unitProvider, value);
    }

    /// <summary>Gets or sets the numeric format string.</summary>
    public string Format
    {
        get => _format;
        set => SetField(ref _format, value);
    }
}
