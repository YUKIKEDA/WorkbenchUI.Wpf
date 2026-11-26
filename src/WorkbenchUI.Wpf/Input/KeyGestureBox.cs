using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Keyboard shortcut input box for settings dialogs.
/// Captures key presses while focused and assigns them to <see cref="Gesture"/> (<see cref="KeyGesture"/>).
/// <list type="bullet">
/// <item>Alphanumeric keys require a modifier key (per WPF <see cref="KeyGesture"/> rules); function keys (F1–F12), Delete, etc. are allowed unmodified.</item>
/// <item>Clicking the trailing clear button resets <see cref="Gesture"/> to <see langword="null"/>; pressing Esc restores the value from when focus was gained.</item>
/// <item>Can be serialized to and from strings using WPF's built-in <see cref="KeyGestureConverter"/>.</item>
/// </list>
/// </summary>
public class KeyGestureBox : Control
{
    /// <summary>Name of the clear button inside the control template.</summary>
    internal const string ClearButtonPartName = "PART_ClearButton";

    private KeyGesture? _valueOnFocus;

    static KeyGestureBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(KeyGestureBox), new FrameworkPropertyMetadata(typeof(KeyGestureBox)));

        EventManager.RegisterClassHandler(
            typeof(KeyGestureBox), ButtonBase.ClickEvent, new RoutedEventHandler(OnButtonClick));
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new FrameworkElementAutomationPeer(this);

    /// <summary>Identifies the <see cref="Gesture"/> dependency property.</summary>
    public static readonly DependencyProperty GestureProperty = DependencyProperty.Register(
        nameof(Gesture), typeof(KeyGesture), typeof(KeyGestureBox),
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnGestureChanged));

    /// <summary>Gets or sets the captured keyboard shortcut, or <see langword="null"/> if unassigned.</summary>
    public KeyGesture? Gesture
    {
        get => (KeyGesture?)GetValue(GestureProperty);
        set => SetValue(GestureProperty, value);
    }

    /// <summary>Identifies the <see cref="PlaceholderText"/> dependency property.</summary>
    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(
        nameof(PlaceholderText), typeof(string), typeof(KeyGestureBox), new PropertyMetadata(null));

    /// <summary>Gets or sets the muted placeholder text shown when no shortcut is assigned (e.g., "Press shortcut keys").</summary>
    public string? PlaceholderText
    {
        get => (string?)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    private static readonly DependencyPropertyKey DisplayTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(DisplayText), typeof(string), typeof(KeyGestureBox), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="DisplayText"/> dependency property.</summary>
    public static readonly DependencyProperty DisplayTextProperty = DisplayTextPropertyKey.DependencyProperty;

    /// <summary>Gets the formatted display string for the current gesture (e.g., "Ctrl+Shift+S"; read-only).</summary>
    public string DisplayText => (string)GetValue(DisplayTextProperty);

    private static void OnGestureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var box = (KeyGestureBox)d;
        box.SetValue(DisplayTextPropertyKey,
            (e.NewValue as KeyGesture)?.GetDisplayStringForCulture(CultureInfo.CurrentCulture) ?? string.Empty);
    }

    /// <inheritdoc />
    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);
        if (e.NewFocus == this)
        {
            _valueOnFocus = Gesture;
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Handled)
        {
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Allow Tab to move focus normally
        if (key is Key.Tab)
        {
            return;
        }

        // Ignore standalone modifier key presses while waiting for the main key
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin)
        {
            e.Handled = true;
            return;
        }

        e.Handled = true;

        // Esc restores the gesture captured when focus entered the control
        if (key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None)
        {
            SetCurrentValue(GestureProperty, _valueOnFocus);
            return;
        }

        try
        {
            SetCurrentValue(GestureProperty, new KeyGesture(key, Keyboard.Modifiers));
        }
        catch (NotSupportedException)
        {
            // Ignore combinations invalid as a KeyGesture (such as unmodified letter keys)
        }
    }

    private static void OnButtonClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { Name: ClearButtonPartName } &&
            sender is KeyGestureBox box)
        {
            e.Handled = true;
            box.SetCurrentValue(GestureProperty, null);
            box.Focus();
        }
    }
}
