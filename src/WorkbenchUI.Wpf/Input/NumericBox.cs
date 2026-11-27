using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Unit-aware numeric input control.
/// <list type="bullet">
/// <item>Stores its value in <see cref="Value"/> (<c>double?</c>), where <see langword="null"/> represents an empty/unset state.</item>
/// <item>Commits on Enter or focus loss, and reverts to the prior value on Esc.</item>
/// <item>Supports scientific notation (e.g., "1e-3"), without evaluating arithmetic expressions.</item>
/// <item>Delegates unit conversion to <see cref="UnitProvider"/> (<see cref="IUnitProvider"/>).</item>
/// <item>Displays validation errors with an error border and tooltip.</item>
/// </list>
/// </summary>
[TemplatePart(Name = PartTextBox, Type = typeof(TextBox))]
[TemplatePart(Name = PartIncreaseButton, Type = typeof(ButtonBase))]
[TemplatePart(Name = PartDecreaseButton, Type = typeof(ButtonBase))]
public class NumericBox : Control
{
    private const string PartTextBox = "PART_TextBox";
    private const string PartIncreaseButton = "PART_IncreaseButton";
    private const string PartDecreaseButton = "PART_DecreaseButton";

    private TextBox? _textBox;
    private ButtonBase? _increaseButton;
    private ButtonBase? _decreaseButton;
    private bool _isTextDirty;
    private bool _suppressTextChanged;

    static NumericBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NumericBox), new FrameworkPropertyMetadata(typeof(NumericBox)));
    }

    #region Dependency properties

    /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double?), typeof(NumericBox),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    /// <summary>Gets or sets the internal value in base units, or <see langword="null"/> when empty.</summary>
    public double? Value
    {
        get => (double?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Identifies the <see cref="Minimum"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(NumericBox), new PropertyMetadata(double.NegativeInfinity));

    /// <summary>Gets or sets the minimum allowed value in base units.</summary>
    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>Identifies the <see cref="Maximum"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(NumericBox), new PropertyMetadata(double.PositiveInfinity));

    /// <summary>Gets or sets the maximum allowed value in base units.</summary>
    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>Identifies the <see cref="Increment"/> dependency property.</summary>
    public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register(
        nameof(Increment), typeof(double), typeof(NumericBox), new PropertyMetadata(1.0));

    /// <summary>Gets or sets the step size (in display units) for spin buttons, Up/Down keys, and mouse wheel scrolling.</summary>
    public double Increment
    {
        get => (double)GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    /// <summary>Identifies the <see cref="Unit"/> dependency property.</summary>
    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
        nameof(Unit), typeof(string), typeof(NumericBox), new PropertyMetadata(null, OnUnitChanged));

    /// <summary>Gets or sets the display-only unit label (no conversion). <see cref="UnitProvider"/> takes precedence when set.</summary>
    public string? Unit
    {
        get => (string?)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    /// <summary>Identifies the <see cref="UnitProvider"/> dependency property.</summary>
    public static readonly DependencyProperty UnitProviderProperty = DependencyProperty.Register(
        nameof(UnitProvider), typeof(IUnitProvider), typeof(NumericBox), new PropertyMetadata(null, OnUnitChanged));

    /// <summary>Gets or sets the provider that converts between display units and internal base units.</summary>
    public IUnitProvider? UnitProvider
    {
        get => (IUnitProvider?)GetValue(UnitProviderProperty);
        set => SetValue(UnitProviderProperty, value);
    }

    /// <summary>Identifies the <see cref="Format"/> dependency property.</summary>
    public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(
        nameof(Format), typeof(string), typeof(NumericBox), new PropertyMetadata("G", OnFormatChanged));

    /// <summary>Gets or sets the numeric format string used for display (e.g., "G6", "F2").</summary>
    public string Format
    {
        get => (string)GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    /// <summary>Identifies the <see cref="IsReadOnly"/> dependency property.</summary>
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly), typeof(bool), typeof(NumericBox), new PropertyMetadata(false));

    /// <summary>Gets or sets whether the control is read-only.</summary>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    // Keep built-in strings customizable via dependency properties so callers can localize them.
    /// <summary>Identifies the <see cref="ParseErrorText"/> dependency property.</summary>
    public static readonly DependencyProperty ParseErrorTextProperty = DependencyProperty.Register(
        nameof(ParseErrorText), typeof(string), typeof(NumericBox),
        new PropertyMetadata("Invalid numeric value."));

    /// <summary>Gets or sets the error message shown when parsing fails (customizable).</summary>
    public string ParseErrorText
    {
        get => (string)GetValue(ParseErrorTextProperty);
        set => SetValue(ParseErrorTextProperty, value);
    }

    /// <summary>Identifies the <see cref="OutOfRangeErrorFormat"/> dependency property.</summary>
    public static readonly DependencyProperty OutOfRangeErrorFormatProperty = DependencyProperty.Register(
        nameof(OutOfRangeErrorFormat), typeof(string), typeof(NumericBox),
        new PropertyMetadata("Value must be between {0} and {1}."));

    /// <summary>Gets or sets the format string for out-of-range errors, where <c>{0}</c> is minimum and <c>{1}</c> is maximum (customizable).</summary>
    public string OutOfRangeErrorFormat
    {
        get => (string)GetValue(OutOfRangeErrorFormatProperty);
        set => SetValue(OutOfRangeErrorFormatProperty, value);
    }

    private static readonly DependencyPropertyKey HasErrorPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasError), typeof(bool), typeof(NumericBox), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="HasError"/> dependency property.</summary>
    public static readonly DependencyProperty HasErrorProperty = HasErrorPropertyKey.DependencyProperty;

    /// <summary>Gets whether the current input text has a validation error (read-only).</summary>
    public bool HasError => (bool)GetValue(HasErrorProperty);

    private static readonly DependencyPropertyKey ErrorTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ErrorText), typeof(string), typeof(NumericBox), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="ErrorText"/> dependency property.</summary>
    public static readonly DependencyProperty ErrorTextProperty = ErrorTextPropertyKey.DependencyProperty;

    /// <summary>Gets the validation error message displayed in the tooltip (read-only).</summary>
    public string? ErrorText => (string?)GetValue(ErrorTextProperty);

    private static readonly DependencyPropertyKey UnitTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(UnitText), typeof(string), typeof(NumericBox), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="UnitText"/> dependency property.</summary>
    public static readonly DependencyProperty UnitTextProperty = UnitTextPropertyKey.DependencyProperty;

    /// <summary>Gets the string displayed in the unit label (<see cref="IUnitProvider.DisplayUnit"/> or <see cref="Unit"/>).</summary>
    public string? UnitText => (string?)GetValue(UnitTextProperty);

    #endregion

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var box = (NumericBox)d;

        // While the user is actively editing (focused), do not overwrite text from external value changes.
        // Committed edits refresh display text explicitly in Commit.
        if (box._textBox is { IsKeyboardFocusWithin: false })
        {
            box.UpdateDisplayText();
            box.ClearError();
        }
    }

    private static void OnUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var box = (NumericBox)d;
        box.UpdateUnitText();
        if (box._textBox is { IsKeyboardFocusWithin: false })
        {
            box.UpdateDisplayText();
        }
    }

    private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var box = (NumericBox)d;
        if (box._textBox is { IsKeyboardFocusWithin: false })
        {
            box.UpdateDisplayText();
        }
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_textBox != null)
        {
            _textBox.TextChanged -= OnTextBoxTextChanged;
            _textBox.PreviewKeyDown -= OnTextBoxPreviewKeyDown;
            _textBox.LostKeyboardFocus -= OnTextBoxLostKeyboardFocus;
        }

        if (_increaseButton != null)
        {
            _increaseButton.Click -= OnIncreaseClick;
        }

        if (_decreaseButton != null)
        {
            _decreaseButton.Click -= OnDecreaseClick;
        }

        base.OnApplyTemplate();

        _textBox = GetTemplateChild(PartTextBox) as TextBox;
        _increaseButton = GetTemplateChild(PartIncreaseButton) as ButtonBase;
        _decreaseButton = GetTemplateChild(PartDecreaseButton) as ButtonBase;

        if (_textBox != null)
        {
            _textBox.TextChanged += OnTextBoxTextChanged;
            _textBox.PreviewKeyDown += OnTextBoxPreviewKeyDown;
            _textBox.LostKeyboardFocus += OnTextBoxLostKeyboardFocus;
        }

        if (_increaseButton != null)
        {
            _increaseButton.Click += OnIncreaseClick;
        }

        if (_decreaseButton != null)
        {
            _decreaseButton.Click += OnDecreaseClick;
        }

        UpdateUnitText();
        UpdateDisplayText();
    }

    /// <inheritdoc />
    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        base.OnPreviewMouseWheel(e);

        if (!IsReadOnly && _textBox is { IsKeyboardFocusWithin: true })
        {
            Spin(e.Delta > 0 ? +1 : -1);
            e.Handled = true;
        }
    }

    private void OnTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                Commit();
                _textBox?.SelectAll();
                e.Handled = true;
                break;
            case Key.Escape:
                RevertText();
                e.Handled = true;
                break;
            case Key.Up when !IsReadOnly:
                Spin(+1);
                e.Handled = true;
                break;
            case Key.Down when !IsReadOnly:
                Spin(-1);
                e.Handled = true;
                break;
        }
    }

    private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_suppressTextChanged)
        {
            _isTextDirty = true;
        }
    }

    private void OnTextBoxLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => Commit();

    private void OnIncreaseClick(object sender, RoutedEventArgs e) => Spin(+1);

    private void OnDecreaseClick(object sender, RoutedEventArgs e) => Spin(-1);

    /// <summary>Parses, validates, and commits the current text to <see cref="Value"/>.</summary>
    private void Commit()
    {
        if (_textBox is null || IsReadOnly || !_isTextDirty)
        {
            return;
        }

        var text = _textBox.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            ClearError();
            SetCurrentValue(ValueProperty, null);
            UpdateDisplayText();
            return;
        }

        if (!NumericText.TryParse(text, CultureInfo.CurrentCulture, out var number, out var unitSymbol))
        {
            SetError(ParseErrorText);
            return;
        }

        double baseValue;
        if (unitSymbol is not null)
        {
            // Explicit unit input (e.g., "500 mm") is valid only if UnitProvider can convert it
            if (UnitProvider is null || !UnitProvider.TryConvertFrom(number, unitSymbol, out baseValue))
            {
                SetError(ParseErrorText);
                return;
            }
        }
        else
        {
            baseValue = UnitProvider?.FromDisplay(number) ?? number;
        }

        if (baseValue < Minimum || baseValue > Maximum)
        {
            // Show range limits in the user's current display unit
            var displayMin = UnitProvider?.ToDisplay(Minimum) ?? Minimum;
            var displayMax = UnitProvider?.ToDisplay(Maximum) ?? Maximum;
            SetError(string.Format(CultureInfo.CurrentCulture, OutOfRangeErrorFormat, FormatNumber(displayMin), FormatNumber(displayMax)));
            return;
        }

        ClearError();
        SetCurrentValue(ValueProperty, baseValue);
        UpdateDisplayText();
    }

    /// <summary>Discards edits and restores the display text for the current <see cref="Value"/> (Esc).</summary>
    private void RevertText()
    {
        ClearError();
        UpdateDisplayText();
        _textBox?.SelectAll();
    }

    /// <summary>Adjusts the value by <see cref="Increment"/> * <paramref name="direction"/> in display units.</summary>
    private void Spin(int direction)
    {
        if (IsReadOnly)
        {
            return;
        }

        var provider = UnitProvider;
        var currentBase = Value ?? 0;
        var display = provider?.ToDisplay(currentBase) ?? currentBase;
        var newDisplay = display + Increment * direction;
        var newBase = provider?.FromDisplay(newDisplay) ?? newDisplay;

        newBase = Math.Clamp(newBase, Minimum, Maximum);

        ClearError();
        SetCurrentValue(ValueProperty, newBase);
        UpdateDisplayText();
        _textBox?.SelectAll();
    }

    private void UpdateDisplayText()
    {
        if (_textBox is null)
        {
            return;
        }

        _suppressTextChanged = true;
        try
        {
            var value = Value;
            if (value is null)
            {
                _textBox.Text = string.Empty;
            }
            else
            {
                var display = UnitProvider?.ToDisplay(value.Value) ?? value.Value;
                _textBox.Text = FormatNumber(display);
            }

            _isTextDirty = false;
        }
        finally
        {
            _suppressTextChanged = false;
        }
    }

    private void UpdateUnitText() =>
        SetValue(UnitTextPropertyKey, UnitProvider?.DisplayUnit ?? Unit);

    private string FormatNumber(double value) =>
        double.IsNegativeInfinity(value) ? "-∞"
        : double.IsPositiveInfinity(value) ? "+∞"
        : value.ToString(Format, CultureInfo.CurrentCulture);

    private void SetError(string message)
    {
        SetValue(HasErrorPropertyKey, true);
        SetValue(ErrorTextPropertyKey, message);
    }

    private void ClearError()
    {
        SetValue(HasErrorPropertyKey, false);
        SetValue(ErrorTextPropertyKey, null);
    }
}
