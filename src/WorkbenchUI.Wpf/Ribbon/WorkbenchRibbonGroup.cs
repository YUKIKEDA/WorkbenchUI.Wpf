using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Labeled group within a <see cref="WorkbenchRibbonTab"/>. Arranges child controls horizontally,
/// displays the group header at the bottom, and draws a separator along the trailing edge.
/// Accepts arbitrary child controls such as buttons styled with <c>Wb.Ribbon.Button.*</c>, <see cref="ComboBox"/>, or vertical <see cref="StackPanel"/> instances.
/// </summary>
public class WorkbenchRibbonGroup : HeaderedItemsControl
{
    static WorkbenchRibbonGroup()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(WorkbenchRibbonGroup), new FrameworkPropertyMetadata(typeof(WorkbenchRibbonGroup)));
    }

    /// <inheritdoc />
    /// <remarks>
    /// The default <see cref="ItemsControl"/> automation peer wraps children in <see cref="ItemAutomationPeer"/> data-item wrappers
    /// that do not forward patterns such as Invoke, preventing UIA clients from activating buttons.
    /// Returning a plain <see cref="FrameworkElementAutomationPeer"/> exposes child peers directly.
    /// </remarks>
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new FrameworkElementAutomationPeer(this);
}
