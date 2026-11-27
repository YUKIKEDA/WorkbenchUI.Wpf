namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Specifies how the navigation pane of a <see cref="NavigationView"/> behaves and displays.
/// </summary>
public enum NavigationViewPaneDisplayMode
{
    /// <summary>
    /// Automatically expands above <see cref="NavigationView.AutoCollapseWidth"/> and collapses to a compact icon rail below it.
    /// </summary>
    Auto,

    /// <summary>
    /// Always keeps the navigation pane open on the left side.
    /// </summary>
    Left,

    /// <summary>
    /// Displays the navigation pane as a narrow icon-only rail on the left side by default.
    /// </summary>
    LeftCompact,
}
