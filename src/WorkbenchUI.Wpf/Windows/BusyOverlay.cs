using System.Windows;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Wraps arbitrary UI content and overlays a semi-transparent veil with a spinner and status message
/// to block user interaction while <see cref="IsBusy"/> is <see langword="true"/>.
/// Set <see cref="BusyContent"/> to display custom content such as a <see cref="ProgressDisplay"/>.
/// </summary>
public class BusyOverlay : ContentControl
{
    static BusyOverlay()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(BusyOverlay),
            new FrameworkPropertyMetadata(typeof(BusyOverlay)));
    }

    /// <summary>Identifies the <see cref="IsBusy"/> dependency property.</summary>
    public static readonly DependencyProperty IsBusyProperty =
        DependencyProperty.Register(
            nameof(IsBusy), typeof(bool), typeof(BusyOverlay),
            new PropertyMetadata(false));

    /// <summary>Gets or sets a value indicating whether the busy overlay is visible and blocks interaction with the underlying content.</summary>
    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    /// <summary>Identifies the <see cref="BusyText"/> dependency property.</summary>
    public static readonly DependencyProperty BusyTextProperty =
        DependencyProperty.Register(
            nameof(BusyText), typeof(string), typeof(BusyOverlay),
            new PropertyMetadata(string.Empty));

    /// <summary>Gets or sets the status message displayed beneath the default spinner.</summary>
    public string BusyText
    {
        get => (string)GetValue(BusyTextProperty);
        set => SetValue(BusyTextProperty, value);
    }

    /// <summary>Identifies the <see cref="BusyContent"/> dependency property.</summary>
    public static readonly DependencyProperty BusyContentProperty =
        DependencyProperty.Register(
            nameof(BusyContent), typeof(object), typeof(BusyOverlay),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets custom content centered in the overlay (such as a <see cref="ProgressDisplay"/>).
    /// When <see langword="null"/>, the default spinner and <see cref="BusyText"/> are shown.
    /// </summary>
    public object? BusyContent
    {
        get => GetValue(BusyContentProperty);
        set => SetValue(BusyContentProperty, value);
    }
}
