using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// High-throughput log console bound to a <see cref="LogBuffer"/> via <see cref="Source"/>.
/// <list type="bullet">
/// <item>Batches UI updates on a timer (100 ms by default) to handle thousands of log lines per second.</item>
/// <item>Auto-scrolls only while already scrolled to the bottom, pausing when the user scrolls up.</item>
/// <item>Supports multi-line selection and Ctrl+C copying with timestamps and log levels.</item>
/// </list>
/// </summary>
[TemplatePart(Name = PartList, Type = typeof(ListBox))]
public class LogConsole : Control
{
    private const string PartList = "PART_List";

    private readonly DispatcherTimer _timer;
    private ListBox? _list;
    private ScrollViewer? _scrollViewer;

    static LogConsole()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(LogConsole), new FrameworkPropertyMetadata(typeof(LogConsole)));
    }

    /// <summary>Initializes a new instance of the <see cref="LogConsole"/> class.</summary>
    public LogConsole()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = FlushInterval };
        _timer.Tick += OnTimerTick;
        Loaded += (_, _) => _timer.Start();
        Unloaded += (_, _) => _timer.Stop();

        CommandBindings.Add(new CommandBinding(
            ApplicationCommands.Copy,
            (_, _) => CopySelection(),
            (_, e) => e.CanExecute = _list?.SelectedItems.Count > 0));
    }

    /// <summary>Identifies the <see cref="Source"/> dependency property.</summary>
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(LogBuffer), typeof(LogConsole), new PropertyMetadata(null));

    /// <summary>Gets or sets the log buffer to display.</summary>
    public LogBuffer? Source
    {
        get => (LogBuffer?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>Identifies the <see cref="FlushInterval"/> dependency property.</summary>
    public static readonly DependencyProperty FlushIntervalProperty = DependencyProperty.Register(
        nameof(FlushInterval), typeof(TimeSpan), typeof(LogConsole),
        new PropertyMetadata(TimeSpan.FromMilliseconds(100), OnFlushIntervalChanged));

    /// <summary>Gets or sets the interval at which pending log entries are flushed to the UI.</summary>
    public TimeSpan FlushInterval
    {
        get => (TimeSpan)GetValue(FlushIntervalProperty);
        set => SetValue(FlushIntervalProperty, value);
    }

    /// <summary>Identifies the <see cref="ShowTimestamps"/> dependency property.</summary>
    public static readonly DependencyProperty ShowTimestampsProperty = DependencyProperty.Register(
        nameof(ShowTimestamps), typeof(bool), typeof(LogConsole), new PropertyMetadata(true));

    /// <summary>Gets or sets whether the timestamp column is visible.</summary>
    public bool ShowTimestamps
    {
        get => (bool)GetValue(ShowTimestampsProperty);
        set => SetValue(ShowTimestampsProperty, value);
    }

    private static void OnFlushIntervalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((LogConsole)d)._timer.Interval = (TimeSpan)e.NewValue;

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _list = GetTemplateChild(PartList) as ListBox;
        _scrollViewer = null;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var source = Source;
        if (source is null)
        {
            return;
        }

        var follow = IsAtBottom();
        if (source.Flush() && follow)
        {
            _list?.UpdateLayout();
            _scrollViewer?.ScrollToEnd();
        }
    }

    private bool IsAtBottom()
    {
        _scrollViewer ??= _list is null ? null : FindDescendant<ScrollViewer>(_list);
        return _scrollViewer is null || _scrollViewer.ScrollableHeight - _scrollViewer.VerticalOffset < 1.0;
    }

    private void CopySelection()
    {
        if (_list is null)
        {
            return;
        }

        var builder = new StringBuilder();
        foreach (var entry in _list.SelectedItems.Cast<LogEntry>().OrderBy(e => e.Timestamp))
        {
            builder.AppendLine($"{entry.Timestamp:HH:mm:ss.fff} [{entry.Level}] {entry.Message}");
        }

        try
        {
            Clipboard.SetText(builder.ToString());
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // Ignore if the clipboard is currently locked by another process
        }
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T found)
            {
                return found;
            }

            if (FindDescendant<T>(child) is T descendant)
            {
                return descendant;
            }
        }

        return null;
    }
}
