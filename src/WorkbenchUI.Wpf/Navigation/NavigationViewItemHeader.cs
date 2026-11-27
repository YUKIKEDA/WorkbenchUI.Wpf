using System.Windows;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Section header displayed between groups of items in a <see cref="NavigationView"/>.
/// Automatically collapses when the host pane is in compact icon-rail mode.
/// </summary>
public class NavigationViewItemHeader : ContentControl
{
    static NavigationViewItemHeader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NavigationViewItemHeader),
            new FrameworkPropertyMetadata(typeof(NavigationViewItemHeader)));
        FocusableProperty.OverrideMetadata(
            typeof(NavigationViewItemHeader), new FrameworkPropertyMetadata(false));
        IsTabStopProperty.OverrideMetadata(
            typeof(NavigationViewItemHeader), new FrameworkPropertyMetadata(false));
    }

    private static readonly DependencyPropertyKey IsCompactPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsCompact), typeof(bool), typeof(NavigationViewItemHeader), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsCompact"/> dependency property.</summary>
    public static readonly DependencyProperty IsCompactProperty = IsCompactPropertyKey.DependencyProperty;

    /// <summary>Gets a value indicating whether the parent <see cref="NavigationView"/> pane is currently compact (read-only).</summary>
    public bool IsCompact => (bool)GetValue(IsCompactProperty);

    internal void SetIsCompact(bool value) => SetValue(IsCompactPropertyKey, value);
}
