using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Drop-down color picker that displays a swatch of the current color and opens a <see cref="ColorEditor"/> popup on click.
/// </summary>
public class ColorPicker : Control
{
    static ColorPicker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ColorPicker), new FrameworkPropertyMetadata(typeof(ColorPicker)));
    }

    /// <summary>Identifies the <see cref="SelectedColor"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
        nameof(SelectedColor), typeof(Color), typeof(ColorPicker),
        new FrameworkPropertyMetadata(Colors.White,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));

    /// <summary>Gets or sets the currently selected color.</summary>
    public Color SelectedColor
    {
        get => (Color)GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    /// <summary>Identifies the <see cref="IsAlphaEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsAlphaEnabledProperty = DependencyProperty.Register(
        nameof(IsAlphaEnabled), typeof(bool), typeof(ColorPicker), new PropertyMetadata(true));

    /// <summary>Gets or sets whether alpha (opacity) editing is allowed.</summary>
    public bool IsAlphaEnabled
    {
        get => (bool)GetValue(IsAlphaEnabledProperty);
        set => SetValue(IsAlphaEnabledProperty, value);
    }

    /// <summary>Identifies the <see cref="Palette"/> dependency property.</summary>
    public static readonly DependencyProperty PaletteProperty = DependencyProperty.Register(
        nameof(Palette), typeof(IEnumerable<Color>), typeof(ColorPicker),
        new PropertyMetadata(ColorEditor.DefaultPalette));

    /// <summary>Gets or sets the colors displayed in the popup palette.</summary>
    public IEnumerable<Color> Palette
    {
        get => (IEnumerable<Color>)GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
        nameof(IsDropDownOpen), typeof(bool), typeof(ColorPicker),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Gets or sets whether the color editor popup is open.</summary>
    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    private static readonly DependencyPropertyKey PreviewBrushPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(PreviewBrush), typeof(Brush), typeof(ColorPicker), new PropertyMetadata(Brushes.White));

    /// <summary>Identifies the <see cref="PreviewBrush"/> dependency property.</summary>
    public static readonly DependencyProperty PreviewBrushProperty = PreviewBrushPropertyKey.DependencyProperty;

    /// <summary>Gets the brush displayed in the color swatch for the current color (read-only).</summary>
    public Brush PreviewBrush => (Brush)GetValue(PreviewBrushProperty);

    /// <summary>Identifies the <see cref="SelectedColorChanged"/> routed event.</summary>
    public static readonly RoutedEvent SelectedColorChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(SelectedColorChanged), RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventHandler<Color>), typeof(ColorPicker));

    /// <summary>Occurs when <see cref="SelectedColor"/> changes.</summary>
    public event RoutedPropertyChangedEventHandler<Color> SelectedColorChanged
    {
        add => AddHandler(SelectedColorChangedEvent, value);
        remove => RemoveHandler(SelectedColorChangedEvent, value);
    }

    private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (ColorPicker)d;

        var brush = new SolidColorBrush((Color)e.NewValue);
        brush.Freeze();
        picker.SetValue(PreviewBrushPropertyKey, brush);

        picker.RaiseEvent(new RoutedPropertyChangedEventArgs<Color>(
            (Color)e.OldValue, (Color)e.NewValue, SelectedColorChangedEvent));
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled)
        {
            return;
        }

        if (e.Key == Key.Escape && IsDropDownOpen)
        {
            SetCurrentValue(IsDropDownOpenProperty, false);
            e.Handled = true;
        }
        else if (e.Key == Key.F4 ||
                 (e.Key == Key.Down && e.KeyboardDevice.Modifiers == ModifierKeys.Alt))
        {
            SetCurrentValue(IsDropDownOpenProperty, !IsDropDownOpen);
            e.Handled = true;
        }
    }
}
