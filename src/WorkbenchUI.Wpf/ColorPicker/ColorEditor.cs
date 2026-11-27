using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WorkbenchUI.Wpf.Theming;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Color selection editor consisting of an HSV saturation/value plane, hue slider, alpha slider,
/// Hex/RGB inputs, and a color palette. Can be embedded in dialogs or used inside the <see cref="ColorPicker"/> popup.
/// </summary>
[TemplatePart(Name = PartSvCanvas, Type = typeof(Canvas))]
[TemplatePart(Name = PartSvThumb, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartHexBox, Type = typeof(TextBox))]
public class ColorEditor : Control
{
    private const string PartSvCanvas = "PART_SvCanvas";
    private const string PartSvThumb = "PART_SvThumb";
    private const string PartHexBox = "PART_HexBox";

    /// <summary>Gets the default 16-color palette.</summary>
    public static IReadOnlyList<Color> DefaultPalette { get; } =
    [
        Color.FromRgb(0xFF, 0xFF, 0xFF), Color.FromRgb(0xC8, 0xC8, 0xC8),
        Color.FromRgb(0x80, 0x80, 0x80), Color.FromRgb(0x40, 0x40, 0x40),
        Color.FromRgb(0x00, 0x00, 0x00), Color.FromRgb(0xE8, 0x11, 0x23),
        Color.FromRgb(0xF7, 0x63, 0x0C), Color.FromRgb(0xFF, 0xD8, 0x00),
        Color.FromRgb(0x16, 0xC6, 0x0C), Color.FromRgb(0x10, 0x7C, 0x10),
        Color.FromRgb(0x00, 0xB7, 0xC3), Color.FromRgb(0x00, 0x78, 0xD7),
        Color.FromRgb(0x00, 0x3E, 0x92), Color.FromRgb(0x88, 0x6C, 0xE4),
        Color.FromRgb(0xE3, 0x00, 0x8C), Color.FromRgb(0x8E, 0x56, 0x2E),
    ];

    private Canvas? _svCanvas;
    private FrameworkElement? _svThumb;
    private TextBox? _hexBox;

    // Canonical HSV state kept separately so byte RGB quantization does not lose hue or saturation
    // (for example, hue cannot be recovered from RGB when approaching black).
    private double _hue;
    private double _saturation;
    private double _value = 1.0;
    private bool _updating;

    static ColorEditor()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ColorEditor), new FrameworkPropertyMetadata(typeof(ColorEditor)));
    }

    /// <summary>Initializes a new instance of the <see cref="ColorEditor"/> class.</summary>
    public ColorEditor()
    {
        // Palette swatches are generated inside an ItemsControl in the template,
        // so clicks are handled via a class-level routed event handler (same pattern as SearchBox).
        AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnPaletteSwatchClick));
    }

    #region Dependency properties

    /// <summary>Identifies the <see cref="SelectedColor"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
        nameof(SelectedColor), typeof(Color), typeof(ColorEditor),
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
        nameof(IsAlphaEnabled), typeof(bool), typeof(ColorEditor),
        new PropertyMetadata(true, OnIsAlphaEnabledChanged));

    /// <summary>Gets or sets whether alpha (opacity) editing UI is shown. When <see langword="false"/>, the color is always opaque.</summary>
    public bool IsAlphaEnabled
    {
        get => (bool)GetValue(IsAlphaEnabledProperty);
        set => SetValue(IsAlphaEnabledProperty, value);
    }

    /// <summary>Identifies the <see cref="Palette"/> dependency property.</summary>
    public static readonly DependencyProperty PaletteProperty = DependencyProperty.Register(
        nameof(Palette), typeof(IEnumerable<Color>), typeof(ColorEditor),
        new PropertyMetadata(DefaultPalette));

    /// <summary>Gets or sets the colors displayed in the palette. Defaults to <see cref="DefaultPalette"/>.</summary>
    public IEnumerable<Color> Palette
    {
        get => (IEnumerable<Color>)GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    /// <summary>Identifies the <see cref="Hue"/> dependency property.</summary>
    public static readonly DependencyProperty HueProperty = DependencyProperty.Register(
        nameof(Hue), typeof(double), typeof(ColorEditor),
        new FrameworkPropertyMetadata(0.0,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHueChanged));

    /// <summary>Gets or sets the hue component (0–360), bound by the template's hue slider.</summary>
    public double Hue
    {
        get => (double)GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    /// <summary>Identifies the <see cref="Red"/> dependency property.</summary>
    public static readonly DependencyProperty RedProperty = DependencyProperty.Register(
        nameof(Red), typeof(double?), typeof(ColorEditor),
        new FrameworkPropertyMetadata(255.0d as double?,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRgbaComponentChanged));

    /// <summary>Gets or sets the red channel (0–255), bound by the template's <see cref="NumericBox"/>.</summary>
    public double? Red
    {
        get => (double?)GetValue(RedProperty);
        set => SetValue(RedProperty, value);
    }

    /// <summary>Identifies the <see cref="Green"/> dependency property.</summary>
    public static readonly DependencyProperty GreenProperty = DependencyProperty.Register(
        nameof(Green), typeof(double?), typeof(ColorEditor),
        new FrameworkPropertyMetadata(255.0d as double?,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRgbaComponentChanged));

    /// <summary>Gets or sets the green channel (0–255).</summary>
    public double? Green
    {
        get => (double?)GetValue(GreenProperty);
        set => SetValue(GreenProperty, value);
    }

    /// <summary>Identifies the <see cref="Blue"/> dependency property.</summary>
    public static readonly DependencyProperty BlueProperty = DependencyProperty.Register(
        nameof(Blue), typeof(double?), typeof(ColorEditor),
        new FrameworkPropertyMetadata(255.0d as double?,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRgbaComponentChanged));

    /// <summary>Gets or sets the blue channel (0–255).</summary>
    public double? Blue
    {
        get => (double?)GetValue(BlueProperty);
        set => SetValue(BlueProperty, value);
    }

    /// <summary>Identifies the <see cref="Alpha"/> dependency property.</summary>
    public static readonly DependencyProperty AlphaProperty = DependencyProperty.Register(
        nameof(Alpha), typeof(double), typeof(ColorEditor),
        new FrameworkPropertyMetadata(255.0,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRgbaComponentChanged));

    /// <summary>Gets or sets the alpha channel (0–255), bound by the template's alpha slider.</summary>
    public double Alpha
    {
        get => (double)GetValue(AlphaProperty);
        set => SetValue(AlphaProperty, value);
    }

    private static readonly DependencyPropertyKey HueBrushPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HueBrush), typeof(Brush), typeof(ColorEditor), new PropertyMetadata(Brushes.Red));

    /// <summary>Identifies the <see cref="HueBrush"/> dependency property.</summary>
    public static readonly DependencyProperty HueBrushProperty = HueBrushPropertyKey.DependencyProperty;

    /// <summary>Gets the pure-color brush for the current hue (used as the SV plane background; read-only).</summary>
    public Brush HueBrush => (Brush)GetValue(HueBrushProperty);

    private static readonly DependencyPropertyKey AlphaTrackBrushPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(AlphaTrackBrush), typeof(Brush), typeof(ColorEditor), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="AlphaTrackBrush"/> dependency property.</summary>
    public static readonly DependencyProperty AlphaTrackBrushProperty = AlphaTrackBrushPropertyKey.DependencyProperty;

    /// <summary>Gets the gradient brush for the alpha slider track (transparent to current color; read-only).</summary>
    public Brush? AlphaTrackBrush => (Brush?)GetValue(AlphaTrackBrushProperty);

    private static readonly DependencyPropertyKey PreviewBrushPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(PreviewBrush), typeof(Brush), typeof(ColorEditor), new PropertyMetadata(Brushes.White));

    /// <summary>Identifies the <see cref="PreviewBrush"/> dependency property.</summary>
    public static readonly DependencyProperty PreviewBrushProperty = PreviewBrushPropertyKey.DependencyProperty;

    /// <summary>Gets the preview brush for the current color including alpha (read-only).</summary>
    public Brush PreviewBrush => (Brush)GetValue(PreviewBrushProperty);

    /// <summary>Identifies the <see cref="SelectedColorChanged"/> routed event.</summary>
    public static readonly RoutedEvent SelectedColorChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(SelectedColorChanged), RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventHandler<Color>), typeof(ColorEditor));

    /// <summary>Occurs when <see cref="SelectedColor"/> changes.</summary>
    public event RoutedPropertyChangedEventHandler<Color> SelectedColorChanged
    {
        add => AddHandler(SelectedColorChangedEvent, value);
        remove => RemoveHandler(SelectedColorChangedEvent, value);
    }

    #endregion

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_svCanvas is not null)
        {
            _svCanvas.MouseLeftButtonDown -= OnSvMouseDown;
            _svCanvas.MouseMove -= OnSvMouseMove;
            _svCanvas.MouseLeftButtonUp -= OnSvMouseUp;
            _svCanvas.SizeChanged -= OnSvSizeChanged;
        }

        if (_hexBox is not null)
        {
            _hexBox.LostKeyboardFocus -= OnHexBoxLostFocus;
            _hexBox.PreviewKeyDown -= OnHexBoxKeyDown;
        }

        base.OnApplyTemplate();

        _svCanvas = GetTemplateChild(PartSvCanvas) as Canvas;
        _svThumb = GetTemplateChild(PartSvThumb) as FrameworkElement;
        _hexBox = GetTemplateChild(PartHexBox) as TextBox;

        if (_svCanvas is not null)
        {
            _svCanvas.MouseLeftButtonDown += OnSvMouseDown;
            _svCanvas.MouseMove += OnSvMouseMove;
            _svCanvas.MouseLeftButtonUp += OnSvMouseUp;
            _svCanvas.SizeChanged += OnSvSizeChanged;
        }

        if (_hexBox is not null)
        {
            _hexBox.LostKeyboardFocus += OnHexBoxLostFocus;
            _hexBox.PreviewKeyDown += OnHexBoxKeyDown;
        }

        SyncFromColor(SelectedColor, preserveHsv: false);
    }

    #region Color synchronization

    private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (ColorEditor)d;

        // During internal updates (originating from HSV interaction), preserve HSV state and only refresh UI.
        // External changes recompute HSV from the new color.
        editor.SyncFromColor((Color)e.NewValue, preserveHsv: editor._updating);

        editor.RaiseEvent(new RoutedPropertyChangedEventArgs<Color>(
            (Color)e.OldValue, (Color)e.NewValue, SelectedColorChangedEvent));
    }

    private static void OnIsAlphaEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (ColorEditor)d;
        if (!(bool)e.NewValue && editor.SelectedColor.A != 255)
        {
            var color = editor.SelectedColor;
            editor.SetCurrentValue(SelectedColorProperty, Color.FromArgb(255, color.R, color.G, color.B));
        }
        else
        {
            editor.UpdateHexText();
        }
    }

    private static void OnHueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (ColorEditor)d;
        if (editor._updating)
        {
            return;
        }

        editor._hue = Math.Clamp((double)e.NewValue, 0, 360);
        editor.ApplyHsv();
    }

    private static void OnRgbaComponentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (ColorEditor)d;
        if (editor._updating)
        {
            return;
        }

        var color = Color.FromArgb(
            editor.IsAlphaEnabled ? ToByte(editor.Alpha) : (byte)255,
            ToByte(editor.Red ?? 0),
            ToByte(editor.Green ?? 0),
            ToByte(editor.Blue ?? 0));
        editor.SetCurrentValue(SelectedColorProperty, color);
    }

    /// <summary>Updates <see cref="SelectedColor"/> from the current HSV and alpha state.</summary>
    private void ApplyHsv()
    {
        var rgb = ColorMath.FromHsv(_hue, _saturation, _value);
        var alpha = IsAlphaEnabled ? ToByte(Alpha) : (byte)255;

        _updating = true;
        try
        {
            SetCurrentValue(SelectedColorProperty, Color.FromArgb(alpha, rgb.R, rgb.G, rgb.B));
        }
        finally
        {
            _updating = false;
        }
    }

    /// <summary>Updates editor controls from <see cref="SelectedColor"/>.</summary>
    private void SyncFromColor(Color color, bool preserveHsv)
    {
        if (!preserveHsv)
        {
            var (h, s, v) = ColorMath.ToHsv(color);

            // Preserve saturation and hue when V=0 (black), and preserve hue when S=0 (achromatic)
            if (v > 0)
            {
                _saturation = s;
                if (s > 0)
                {
                    _hue = h;
                }
            }

            _value = v;
        }

        var wasUpdating = _updating;
        _updating = true;
        try
        {
            SetCurrentValue(HueProperty, _hue);
            SetCurrentValue(RedProperty, (double?)color.R);
            SetCurrentValue(GreenProperty, (double?)color.G);
            SetCurrentValue(BlueProperty, (double?)color.B);
            SetCurrentValue(AlphaProperty, (double)color.A);
        }
        finally
        {
            _updating = wasUpdating;
        }

        UpdateBrushes(color);
        UpdateHexText();
        UpdateSvThumbPosition();
    }

    private void UpdateBrushes(Color color)
    {
        var hueBrush = new SolidColorBrush(ColorMath.FromHsv(_hue, 1, 1));
        hueBrush.Freeze();
        SetValue(HueBrushPropertyKey, hueBrush);

        var opaque = Color.FromRgb(color.R, color.G, color.B);
        var alphaTrack = new LinearGradientBrush(
            Color.FromArgb(0, opaque.R, opaque.G, opaque.B), opaque, 0.0);
        alphaTrack.Freeze();
        SetValue(AlphaTrackBrushPropertyKey, alphaTrack);

        var preview = new SolidColorBrush(color);
        preview.Freeze();
        SetValue(PreviewBrushPropertyKey, preview);
    }

    private static byte ToByte(double value) => (byte)Math.Clamp(Math.Round(value), 0, 255);

    #endregion

    #region SV area (saturation / value square)

    private void OnSvMouseDown(object sender, MouseButtonEventArgs e)
    {
        _svCanvas?.CaptureMouse();
        UpdateSvFromPoint(e.GetPosition(_svCanvas));
        e.Handled = true;
    }

    private void OnSvMouseMove(object sender, MouseEventArgs e)
    {
        if (_svCanvas is { IsMouseCaptured: true })
        {
            UpdateSvFromPoint(e.GetPosition(_svCanvas));
        }
    }

    private void OnSvMouseUp(object sender, MouseButtonEventArgs e) => _svCanvas?.ReleaseMouseCapture();

    private void OnSvSizeChanged(object sender, SizeChangedEventArgs e) => UpdateSvThumbPosition();

    private void UpdateSvFromPoint(Point point)
    {
        if (_svCanvas is null || _svCanvas.ActualWidth <= 0 || _svCanvas.ActualHeight <= 0)
        {
            return;
        }

        _saturation = Math.Clamp(point.X / _svCanvas.ActualWidth, 0, 1);
        _value = Math.Clamp(1 - point.Y / _svCanvas.ActualHeight, 0, 1);
        ApplyHsv();
    }

    private void UpdateSvThumbPosition()
    {
        if (_svCanvas is null || _svThumb is null)
        {
            return;
        }

        Canvas.SetLeft(_svThumb, _saturation * _svCanvas.ActualWidth - _svThumb.ActualWidth / 2);
        Canvas.SetTop(_svThumb, (1 - _value) * _svCanvas.ActualHeight - _svThumb.ActualHeight / 2);
    }

    #endregion

    #region Hex input

    private void OnHexBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitHexText();
            _hexBox?.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            UpdateHexText();
            e.Handled = true;
        }
    }

    private void OnHexBoxLostFocus(object sender, KeyboardFocusChangedEventArgs e) => CommitHexText();

    private void CommitHexText()
    {
        if (_hexBox is null)
        {
            return;
        }

        if (TryParseHex(_hexBox.Text, out var color))
        {
            if (!IsAlphaEnabled)
            {
                color = Color.FromArgb(255, color.R, color.G, color.B);
            }

            SetCurrentValue(SelectedColorProperty, color);
        }
        else
        {
            // Revert invalid input to the formatted representation of the current color
            UpdateHexText();
        }
    }

    private void UpdateHexText()
    {
        if (_hexBox is not null)
        {
            var color = SelectedColor;
            _hexBox.Text = IsAlphaEnabled
                ? $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}"
                : $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }

    /// <summary>Parses a "#RRGGBB" or "#AARRGGBB" string (leading '#' is optional).</summary>
    internal static bool TryParseHex(string? text, out Color color)
    {
        color = default;
        var hex = text?.Trim().TrimStart('#');
        if (hex is null || (hex.Length != 6 && hex.Length != 8))
        {
            return false;
        }

        if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        if (hex.Length == 6)
        {
            value |= 0xFF000000;
        }

        color = Color.FromArgb(
            (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);
        return true;
    }

    #endregion

    private void OnPaletteSwatchClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { Name: "PaletteSwatch", DataContext: Color color })
        {
            // Palette swatches apply RGB only and preserve the current alpha
            var alpha = IsAlphaEnabled ? SelectedColor.A : (byte)255;
            SetCurrentValue(SelectedColorProperty, Color.FromArgb(alpha, color.R, color.G, color.B));
            e.Handled = true;
        }
    }
}
