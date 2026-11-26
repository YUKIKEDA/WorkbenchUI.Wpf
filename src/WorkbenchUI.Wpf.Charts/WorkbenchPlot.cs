using System.Windows;
using System.Windows.Input;
using WorkbenchUI.Wpf.Theming;

namespace WorkbenchUI.Wpf.Charts;

/// <summary>
/// A <see cref="ScottPlot.WPF.WpfPlot"/> preconfigured with WorkbenchUI.Wpf design token colors.
/// <para>
/// Calls <see cref="WorkbenchChartTheme.Apply"/> on creation and subscribes to
/// <see cref="ThemeManager.ThemeChanged"/> while loaded to automatically follow theme and accent changes.
/// (Existing plottable colors are not reapplied automatically, so composite controls that need
/// series colors to update should rebuild their plots on <see cref="ThemeApplied"/>.)
/// </para>
/// <para>
/// Mouse wheel zooming defaults to <b>Ctrl+Wheel</b> (<see cref="WheelZoomRequiresCtrl"/>).
/// Unmodified wheel events are forwarded to outer scroll containers so the plot does not
/// interfere with page scrolling when embedded in a <see cref="System.Windows.Controls.ScrollViewer"/>.
/// </para>
/// </summary>
public class WorkbenchPlot : ScottPlot.WPF.WpfPlot
{
    /// <summary>Identifies the <see cref="WheelZoomRequiresCtrl"/> dependency property.</summary>
    public static readonly DependencyProperty WheelZoomRequiresCtrlProperty =
        DependencyProperty.Register(
            nameof(WheelZoomRequiresCtrl), typeof(bool), typeof(WorkbenchPlot),
            new PropertyMetadata(true));

    /// <summary>
    /// Initializes a new instance of <see cref="WorkbenchPlot"/>.
    /// </summary>
    public WorkbenchPlot()
    {
        WorkbenchChartTheme.Apply(Plot);

        // ScottPlot's default MouseWheelZoom uses Ctrl to lock the vertical axis, which conflicts
        // with the Ctrl+Wheel zoom gate. Reassign axis lock keys to Shift (horizontal) and Alt (vertical).
        UserInputProcessor.RemoveAll<ScottPlot.Interactivity.UserActionResponses.MouseWheelZoom>();
        UserInputProcessor.UserActionResponses.Add(
            new ScottPlot.Interactivity.UserActionResponses.MouseWheelZoom(
                ScottPlot.Interactivity.StandardKeys.Shift,
                ScottPlot.Interactivity.StandardKeys.Alt));

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>
    /// Raised after the chart theme has been reapplied, allowing composite controls to rebuild their series.
    /// </summary>
    public event EventHandler? ThemeApplied;

    /// <summary>
    /// When <see langword="true"/> (default), mouse wheel zooming requires holding the Ctrl key,
    /// and unmodified wheel events bubble to outer scroll containers.
    /// Set to <see langword="false"/> for full-bleed plots in non-scrolling layouts to allow direct wheel zooming.
    /// </summary>
    public bool WheelZoomRequiresCtrl
    {
        get => (bool)GetValue(WheelZoomRequiresCtrlProperty);
        set => SetValue(WheelZoomRequiresCtrlProperty, value);
    }

    private bool IsWheelZoomGesture =>
        !WheelZoomRequiresCtrl || (Keyboard.Modifiers & ModifierKeys.Control) != 0;

    /// <inheritdoc />
    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        base.OnPreviewMouseWheel(e);

        if (IsWheelZoomGesture)
        {
            return; // Allow tunneling down to the inner SKElement so ScottPlot performs the zoom.
        }

        // Unmodified wheel: suppress zoom before it reaches SKElement and re-raise a bubbling
        // MouseWheelEvent from this control so an outer ScrollViewer can scroll the page.
        e.Handled = true;
        var args = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = MouseWheelEvent,
            Source = this,
        };
        RaiseEvent(args);
    }

    /// <inheritdoc />
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (IsWheelZoomGesture)
        {
            // Stop bubbling during zoom so outer scroll containers do not scroll simultaneously.
            e.Handled = true;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Subscribe only while loaded to prevent leaks from the static ThemeChanged event.
        ThemeManager.ThemeChanged -= OnThemeChanged;
        ThemeManager.ThemeChanged += OnThemeChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ThemeManager.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        WorkbenchChartTheme.Apply(Plot);
        ThemeApplied?.Invoke(this, EventArgs.Empty);
        Refresh();
    }
}
