using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Infrastructure;

/// <summary>Allows Gallery pages to interact with the host shell window.</summary>
public interface IGalleryHost
{
    /// <summary>Navigates to the page with the specified identifier.</summary>
    void NavigateTo(string pageId);

    /// <summary>Opens the multi-pane docking shell sample window.</summary>
    void LaunchDockingShell();

    /// <summary>Gets or sets the pane display mode of the shell navigation view.</summary>
    NavigationViewPaneDisplayMode PaneDisplayMode { get; set; }

    /// <summary>Gets or sets whether the shell navigation pane is expanded.</summary>
    bool IsPaneOpen { get; set; }

    /// <summary>Raised when shell layout properties such as <see cref="PaneDisplayMode"/> or <see cref="IsPaneOpen"/> change.</summary>
    event EventHandler? ShellStateChanged;
}
