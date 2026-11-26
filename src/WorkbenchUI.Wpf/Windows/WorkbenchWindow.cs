using System.Windows;
using System.Windows.Input;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Custom-chrome <see cref="Window"/> built on <see cref="System.Windows.Shell.WindowChrome"/>.
/// Preserves native OS behaviors (resizing, Aero snap, maximize) while theming the title bar
/// and allowing arbitrary content in <see cref="TitleBarContent"/> (such as a menu bar or search box).
/// </summary>
public class WorkbenchWindow : Window
{
    static WorkbenchWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(WorkbenchWindow),
            new FrameworkPropertyMetadata(typeof(WorkbenchWindow)));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkbenchWindow"/> class.
    /// </summary>
    public WorkbenchWindow()
    {
        // Wire caption buttons through SystemCommands.
        CommandBindings.Add(new CommandBinding(
            SystemCommands.MinimizeWindowCommand, (_, _) => SystemCommands.MinimizeWindow(this)));
        CommandBindings.Add(new CommandBinding(
            SystemCommands.MaximizeWindowCommand, (_, _) => SystemCommands.MaximizeWindow(this)));
        CommandBindings.Add(new CommandBinding(
            SystemCommands.RestoreWindowCommand, (_, _) => SystemCommands.RestoreWindow(this)));
        CommandBindings.Add(new CommandBinding(
            SystemCommands.CloseWindowCommand, (_, _) => SystemCommands.CloseWindow(this)));
    }

    /// <inheritdoc />
    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        // Work around a known WPF WindowChrome + SizeToContent issue where initial layout includes
        // standard frame margins and leaves unpainted regions along the right and bottom edges.
        if (SizeToContent != SizeToContent.Manual)
        {
            InvalidateMeasure();
        }
    }

    /// <summary>Identifies the <see cref="TitleBarContent"/> dependency property.</summary>
    public static readonly DependencyProperty TitleBarContentProperty =
        DependencyProperty.Register(
            nameof(TitleBarContent), typeof(object), typeof(WorkbenchWindow),
            new PropertyMetadata(null, OnTitleBarContentChanged));

    /// <summary>
    /// Gets or sets arbitrary content placed between the window title and caption buttons
    /// (for example, a menu bar or search box). Interaction is prioritized over window dragging in this region.
    /// </summary>
    public object? TitleBarContent
    {
        get => GetValue(TitleBarContentProperty);
        set => SetValue(TitleBarContentProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == DataContextProperty)
        {
            SyncTitleBarDataContext();
        }
    }

    private static void OnTitleBarContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((WorkbenchWindow)d).SyncTitleBarDataContext();

    /// <summary>
    /// Explicitly propagates <see cref="FrameworkElement.DataContext"/> to <see cref="TitleBarContent"/>,
    /// because content hosted inside the chrome template's <see cref="System.Windows.Controls.ContentPresenter"/> may not inherit it automatically.
    /// </summary>
    private void SyncTitleBarDataContext()
    {
        if (TitleBarContent is FrameworkElement element)
        {
            element.DataContext = DataContext;
        }
    }
}
