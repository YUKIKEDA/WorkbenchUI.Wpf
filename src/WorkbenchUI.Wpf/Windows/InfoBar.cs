using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>Severity level of an <see cref="InfoBar"/>, controlling its icon and accent color.</summary>
public enum InfoBarSeverity
{
    /// <summary>Informational message.</summary>
    Info,

    /// <summary>Success or confirmation message.</summary>
    Success,

    /// <summary>Warning message.</summary>
    Warning,

    /// <summary>Error message.</summary>
    Error,
}

/// <summary>
/// Inline notification bar control. Unlike transient <see cref="ToastHost"/> notifications,
/// an <see cref="InfoBar"/> remains visible until dismissed by the user or closed programmatically.
/// <see cref="IsOpen"/> binds two-way by default so view models can observe user dismissal.
/// </summary>
public class InfoBar : Control
{
    static InfoBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(InfoBar), new FrameworkPropertyMetadata(typeof(InfoBar)));

        // Handle clicks from the template's PART_CloseButton.
        EventManager.RegisterClassHandler(
            typeof(InfoBar), ButtonBase.ClickEvent, new RoutedEventHandler(OnButtonClick));
    }

    /// <summary>Identifies the <see cref="Severity"/> dependency property.</summary>
    public static readonly DependencyProperty SeverityProperty = DependencyProperty.Register(
        nameof(Severity), typeof(InfoBarSeverity), typeof(InfoBar),
        new PropertyMetadata(InfoBarSeverity.Info));

    /// <summary>Gets or sets the notification severity level.</summary>
    public InfoBarSeverity Severity
    {
        get => (InfoBarSeverity)GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(InfoBar), new PropertyMetadata(null));

    /// <summary>Gets or sets the bold heading text. Hidden when <see langword="null"/>.</summary>
    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Identifies the <see cref="Message"/> dependency property.</summary>
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(InfoBar), new PropertyMetadata(null));

    /// <summary>Gets or sets the notification body message.</summary>
    public string? Message
    {
        get => (string?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>Identifies the <see cref="IsOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(InfoBar),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Gets or sets a value indicating whether the bar is open. Set to <see langword="false"/> when dismissed via the close button.</summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Identifies the <see cref="IsClosable"/> dependency property.</summary>
    public static readonly DependencyProperty IsClosableProperty = DependencyProperty.Register(
        nameof(IsClosable), typeof(bool), typeof(InfoBar), new PropertyMetadata(true));

    /// <summary>Gets or sets a value indicating whether the user can dismiss the bar with the close button.</summary>
    public bool IsClosable
    {
        get => (bool)GetValue(IsClosableProperty);
        set => SetValue(IsClosableProperty, value);
    }

    /// <summary>Identifies the <see cref="ActionContent"/> dependency property.</summary>
    public static readonly DependencyProperty ActionContentProperty = DependencyProperty.Register(
        nameof(ActionContent), typeof(object), typeof(InfoBar), new PropertyMetadata(null));

    /// <summary>Gets or sets optional action content (such as a <see cref="Button"/> or hyperlink) displayed beside the message.</summary>
    public object? ActionContent
    {
        get => GetValue(ActionContentProperty);
        set => SetValue(ActionContentProperty, value);
    }

    /// <summary>Identifies the <see cref="Closed"/> routed event.</summary>
    public static readonly RoutedEvent ClosedEvent = EventManager.RegisterRoutedEvent(
        nameof(Closed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(InfoBar));

    /// <summary>Occurs when the user dismisses the <see cref="InfoBar"/> via its close button.</summary>
    public event RoutedEventHandler Closed
    {
        add => AddHandler(ClosedEvent, value);
        remove => RemoveHandler(ClosedEvent, value);
    }

    private static void OnButtonClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { Name: "PART_CloseButton" } &&
            sender is InfoBar bar)
        {
            e.Handled = true;
            bar.SetCurrentValue(IsOpenProperty, false);
            bar.RaiseEvent(new RoutedEventArgs(ClosedEvent, bar));
        }
    }
}
