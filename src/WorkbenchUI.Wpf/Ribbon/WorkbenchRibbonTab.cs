using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Markup;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Tab item in a <see cref="WorkbenchRibbon"/>. Hosts <see cref="WorkbenchRibbonGroup"/> instances
/// (or arbitrary elements) as direct XAML content.
/// </summary>
[ContentProperty(nameof(Groups))]
public class WorkbenchRibbonTab : TabItem
{
    static WorkbenchRibbonTab()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(WorkbenchRibbonTab), new FrameworkPropertyMetadata(typeof(WorkbenchRibbonTab)));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkbenchRibbonTab"/> class.
    /// </summary>
    public WorkbenchRibbonTab()
    {
        // Tab content is a horizontal host for groups.
        // Placing an ItemsControl bound to Groups inside Content reuses TabControl's SelectedContent mechanism.
        Content = CreateGroupsHost();
    }

    /// <summary>Gets the collection of groups displayed in this tab.</summary>
    public ObservableCollection<object> Groups { get; } = [];

    private ItemsControl CreateGroupsHost()
    {
        var panelFactory = new FrameworkElementFactory(typeof(StackPanel));
        panelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

        var host = new GroupsHost
        {
            ItemsSource = Groups,
            ItemsPanel = new ItemsPanelTemplate(panelFactory),
            Focusable = false,
        };
        // Keep group heights uniform across tabs (row height is enforced by the ribbon's MinHeight).
        host.VerticalAlignment = VerticalAlignment.Stretch;
        return host;
    }

    /// <summary>
    /// Host for ribbon groups. Uses a plain <see cref="FrameworkElementAutomationPeer"/> instead of the default
    /// <see cref="ItemsControl"/> peer (which wraps children in DataItem wrappers and blocks Invoke patterns)
    /// so child automation peers are exposed directly.
    /// </summary>
    private sealed class GroupsHost : ItemsControl
    {
        protected override AutomationPeer OnCreateAutomationPeer() =>
            new FrameworkElementAutomationPeer(this);
    }
}
