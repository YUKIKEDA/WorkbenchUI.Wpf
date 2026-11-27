using System.Windows;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>Provides data for the <see cref="NavigationView.SelectionChanged"/> routed event.</summary>
public sealed class NavigationViewSelectionChangedEventArgs(
    RoutedEvent routedEvent,
    object source,
    object? selectedItem,
    NavigationViewItem? selectedItemContainer) : RoutedEventArgs(routedEvent, source)
{
    /// <summary>Gets the newly selected item, or <see langword="null"/> if selection was cleared.</summary>
    public object? SelectedItem { get; } = selectedItem;

    /// <summary>Gets the <see cref="NavigationViewItem"/> associated with the selection, if available.</summary>
    public NavigationViewItem? SelectedItemContainer { get; } = selectedItemContainer;
}

/// <summary>Represents the method that handles <see cref="NavigationView.SelectionChanged"/>.</summary>
public delegate void NavigationViewSelectionChangedEventHandler(
    object sender, NavigationViewSelectionChangedEventArgs e);
