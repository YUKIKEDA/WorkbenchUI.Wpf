using System.Windows;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Horizontal divider displayed between items in a <see cref="NavigationView"/>.
/// </summary>
public class NavigationViewItemSeparator : Control
{
    static NavigationViewItemSeparator()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NavigationViewItemSeparator),
            new FrameworkPropertyMetadata(typeof(NavigationViewItemSeparator)));
        FocusableProperty.OverrideMetadata(
            typeof(NavigationViewItemSeparator), new FrameworkPropertyMetadata(false));
        IsTabStopProperty.OverrideMetadata(
            typeof(NavigationViewItemSeparator), new FrameworkPropertyMetadata(false));
    }
}
