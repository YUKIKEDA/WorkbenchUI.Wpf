using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Search input box with search icon, placeholder text, and clear button.
/// <see cref="Text"/> updates immediately on every keystroke, while <see cref="SearchText"/>
/// updates after <see cref="SearchDelay"/> (200 ms by default) so expensive list filtering
/// can bind to <see cref="SearchText"/> without running on every keystroke.
/// Press Enter to commit immediately, or Esc to clear.
/// </summary>
public class SearchBox : Control
{
    /// <summary>Name of the clear button inside the control template (used for click detection).</summary>
    internal const string ClearButtonPartName = "PART_ClearButton";

    private readonly DispatcherTimer _timer;
    private bool _syncingFromSearchText;

    static SearchBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SearchBox), new FrameworkPropertyMetadata(typeof(SearchBox)));
    }

    /// <summary>Initializes a new instance of the <see cref="SearchBox"/> class.</summary>
    public SearchBox()
    {
        _timer = new DispatcherTimer();
        _timer.Tick += (_, _) => Commit();
        AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnClearButtonClick));
    }

    // Control has no AutomationPeer by default; expose AutomationId and the inner TextBox
    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new FrameworkElementAutomationPeer(this);

    /// <summary>Identifies the <see cref="Text"/> dependency property.</summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(SearchBox),
        new FrameworkPropertyMetadata(string.Empty,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

    /// <summary>Gets or sets the current input text (updated immediately on each keystroke).</summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Identifies the <see cref="SearchText"/> dependency property.</summary>
    public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register(
        nameof(SearchText), typeof(string), typeof(SearchBox),
        new FrameworkPropertyMetadata(string.Empty,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSearchTextChanged));

    /// <summary>Gets or sets the debounced search text. Bind filtering logic to this property.</summary>
    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    /// <summary>Identifies the <see cref="SearchDelay"/> dependency property.</summary>
    public static readonly DependencyProperty SearchDelayProperty = DependencyProperty.Register(
        nameof(SearchDelay), typeof(TimeSpan), typeof(SearchBox),
        new PropertyMetadata(TimeSpan.FromMilliseconds(200)));

    /// <summary>Gets or sets the debounce delay before <see cref="Text"/> propagates to <see cref="SearchText"/>. Zero commits immediately.</summary>
    public TimeSpan SearchDelay
    {
        get => (TimeSpan)GetValue(SearchDelayProperty);
        set => SetValue(SearchDelayProperty, value);
    }

    // Keep built-in strings customizable via dependency properties
    /// <summary>Identifies the <see cref="Placeholder"/> dependency property.</summary>
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(SearchBox), new PropertyMetadata("Search..."));

    /// <summary>Gets or sets the placeholder text displayed when empty (customizable).</summary>
    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var box = (SearchBox)d;
        if (box._syncingFromSearchText)
        {
            return;
        }

        var delay = box.SearchDelay;
        if (delay <= TimeSpan.Zero)
        {
            box.Commit();
        }
        else
        {
            box._timer.Stop();
            box._timer.Interval = delay;
            box._timer.Start();
        }
    }

    private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Synchronize Text if SearchText is updated directly by the application
        var box = (SearchBox)d;
        if (!box._timer.IsEnabled && !Equals(box.Text, e.NewValue))
        {
            box._syncingFromSearchText = true;
            try
            {
                box.SetCurrentValue(TextProperty, (string?)e.NewValue ?? string.Empty);
            }
            finally
            {
                box._syncingFromSearchText = false;
            }
        }
    }

    /// <summary>Cancels any pending debounce timer and immediately copies <see cref="Text"/> to <see cref="SearchText"/>.</summary>
    public void Commit()
    {
        _timer.Stop();
        SetCurrentValue(SearchTextProperty, Text);
    }

    /// <summary>Clears the text and commits immediately.</summary>
    public void Clear()
    {
        SetCurrentValue(TextProperty, string.Empty);
        Commit();
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Handled)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            Commit();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && !string.IsNullOrEmpty(Text))
        {
            Clear();
            e.Handled = true;
        }
    }

    private void OnClearButtonClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { Name: ClearButtonPartName })
        {
            Clear();
            e.Handled = true;
        }
    }
}
