using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>Severity level of a toast notification, controlling its accent stripe color.</summary>
public enum ToastLevel
{
    /// <summary>Informational toast.</summary>
    Info,

    /// <summary>Success toast.</summary>
    Success,

    /// <summary>Warning toast.</summary>
    Warning,

    /// <summary>Error toast.</summary>
    Error,
}

/// <summary>Represents a single active toast notification.</summary>
public sealed class ToastItem
{
    internal ToastItem(string message, ToastLevel level)
    {
        Message = message;
        Level = level;
    }

    /// <summary>Gets the notification message text.</summary>
    public string Message { get; }

    /// <summary>Gets the severity level of the toast.</summary>
    public ToastLevel Level { get; }
}

/// <summary>
/// Container control that displays transient toast notifications.
/// Place explicitly in the application layout (typically bottom-right of the root grid) and enqueue notifications via <see cref="Show(string, ToastLevel, TimeSpan?)"/>.
/// Notifications dismiss automatically after <see cref="DefaultDuration"/> or when their close button is clicked.
/// </summary>
public class ToastHost : Control
{
    /// <summary>Template part name used to identify each toast's close button.</summary>
    internal const string ClosePartName = "PART_ToastClose";

    private readonly ObservableCollection<ToastItem> _items = [];
    private readonly Dictionary<ToastItem, DispatcherTimer> _timers = [];

    static ToastHost()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ToastHost),
            new FrameworkPropertyMetadata(typeof(ToastHost)));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToastHost"/> class.
    /// </summary>
    public ToastHost()
    {
        SetValue(ItemsPropertyKey, new ReadOnlyObservableCollection<ToastItem>(_items));

        // Handle clicks from close buttons inside item templates centrally.
        AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnCloseButtonClick));
        Unloaded += OnUnloaded;
    }

    private static readonly DependencyPropertyKey ItemsPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(Items), typeof(ReadOnlyObservableCollection<ToastItem>), typeof(ToastHost),
            new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty = ItemsPropertyKey.DependencyProperty;

    /// <summary>Gets the read-only collection of currently visible toast notifications.</summary>
    public ReadOnlyObservableCollection<ToastItem> Items =>
        (ReadOnlyObservableCollection<ToastItem>)GetValue(ItemsProperty);

    /// <summary>Identifies the <see cref="DefaultDuration"/> dependency property.</summary>
    public static readonly DependencyProperty DefaultDurationProperty =
        DependencyProperty.Register(
            nameof(DefaultDuration), typeof(TimeSpan), typeof(ToastHost),
            new PropertyMetadata(TimeSpan.FromSeconds(4)));

    /// <summary>Gets or sets the display duration used when <see cref="Show(string, ToastLevel, TimeSpan?)"/> omits an explicit duration. Defaults to 4 seconds.</summary>
    public TimeSpan DefaultDuration
    {
        get => (TimeSpan)GetValue(DefaultDurationProperty);
        set => SetValue(DefaultDurationProperty, value);
    }

    /// <summary>Displays a toast notification. Must be called on the UI thread.</summary>
    public void Show(string message, ToastLevel level = ToastLevel.Info, TimeSpan? duration = null)
    {
        var item = new ToastItem(message, level);
        _items.Add(item);

        var timer = new DispatcherTimer { Interval = duration ?? DefaultDuration };
        void OnTick(object? sender, EventArgs e)
        {
            timer.Tick -= OnTick;
            RemoveToast(item);
        }

        timer.Tick += OnTick;
        _timers[item] = timer;
        timer.Start();
    }

    /// <summary>Dismisses all currently visible toast notifications.</summary>
    public void Clear()
    {
        foreach (var timer in _timers.Values)
        {
            timer.Stop();
        }

        _timers.Clear();
        _items.Clear();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        foreach (var timer in _timers.Values)
        {
            timer.Stop();
        }

        _timers.Clear();
    }

    private void RemoveToast(ToastItem item)
    {
        if (_timers.Remove(item, out var timer))
        {
            timer.Stop();
        }

        _items.Remove(item);
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { Name: ClosePartName, DataContext: ToastItem item })
        {
            RemoveToast(item);
            e.Handled = true;
        }
    }
}
