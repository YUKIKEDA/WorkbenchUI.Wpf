using System.Windows;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Base window for modal and in-app dialogs.
/// Extends <see cref="WorkbenchWindow"/> with a bottom <see cref="Footer"/> button-bar region,
/// defaulting to owner-centered startup, hidden from the taskbar, and non-resizable.
/// </summary>
public class WorkbenchDialogWindow : WorkbenchWindow
{
    static WorkbenchDialogWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(WorkbenchDialogWindow),
            new FrameworkPropertyMetadata(typeof(WorkbenchDialogWindow)));
        ResizeModeProperty.OverrideMetadata(
            typeof(WorkbenchDialogWindow),
            new FrameworkPropertyMetadata(ResizeMode.NoResize));
        ShowInTaskbarProperty.OverrideMetadata(
            typeof(WorkbenchDialogWindow),
            new FrameworkPropertyMetadata(false));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkbenchDialogWindow"/> class.
    /// </summary>
    public WorkbenchDialogWindow()
    {
        // Falls back to CenterScreen on ShowDialog if Owner is not set.
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }

    /// <summary>Identifies the <see cref="Footer"/> dependency property.</summary>
    public static readonly DependencyProperty FooterProperty =
        DependencyProperty.Register(
            nameof(Footer), typeof(object), typeof(WorkbenchDialogWindow),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the content displayed in the dialog's bottom button bar.
    /// When <see langword="null"/>, the footer bar is hidden.
    /// </summary>
    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }
}
