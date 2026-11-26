using System.Windows;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Standalone indeterminate spinning ring extracted from <see cref="BusyOverlay"/>.
/// Specify dimensions via <see cref="FrameworkElement.Width"/> and <see cref="FrameworkElement.Height"/> (default is 24x24).
/// </summary>
public class ProgressRing : Control
{
    static ProgressRing()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ProgressRing), new FrameworkPropertyMetadata(typeof(ProgressRing)));
    }

    /// <summary>Identifies the <see cref="IsActive"/> dependency property.</summary>
    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive), typeof(bool), typeof(ProgressRing), new PropertyMetadata(true));

    /// <summary>Gets or sets a value indicating whether the ring is visible and spinning.</summary>
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }
}
