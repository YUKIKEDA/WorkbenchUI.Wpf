using System.Windows;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Workflow-oriented ribbon control organizing commands by task stage into tabs and labeled groups.
/// Supports tabs, groups, large and small button style keys, arbitrary hosted controls,
/// an application menu button, and an auxiliary area at the trailing edge of the tab strip,
/// with horizontal scrolling when content overflows.
/// </summary>
public class WorkbenchRibbon : TabControl
{
    static WorkbenchRibbon()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(WorkbenchRibbon), new FrameworkPropertyMetadata(typeof(WorkbenchRibbon)));
    }

    /// <summary>Identifies the <see cref="ApplicationMenu"/> dependency property.</summary>
    public static readonly DependencyProperty ApplicationMenuProperty = DependencyProperty.Register(
        nameof(ApplicationMenu), typeof(ContextMenu), typeof(WorkbenchRibbon),
        new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the menu opened by the application button at the leading edge of the tab strip,
    /// typically used to consolidate file operations and other global commands.
    /// </summary>
    public ContextMenu? ApplicationMenu
    {
        get => (ContextMenu?)GetValue(ApplicationMenuProperty);
        set => SetValue(ApplicationMenuProperty, value);
    }

    /// <summary>Identifies the <see cref="ApplicationLabel"/> dependency property.</summary>
    public static readonly DependencyProperty ApplicationLabelProperty = DependencyProperty.Register(
        nameof(ApplicationLabel), typeof(object), typeof(WorkbenchRibbon),
        new PropertyMetadata("File"));

    /// <summary>Gets or sets the label displayed on the application button. Defaults to <c>"File"</c>.</summary>
    public object? ApplicationLabel
    {
        get => GetValue(ApplicationLabelProperty);
        set => SetValue(ApplicationLabelProperty, value);
    }

    /// <summary>Identifies the <see cref="AuxiliaryContent"/> dependency property.</summary>
    public static readonly DependencyProperty AuxiliaryContentProperty = DependencyProperty.Register(
        nameof(AuxiliaryContent), typeof(object), typeof(WorkbenchRibbon),
        new PropertyMetadata(null));

    /// <summary>Gets or sets auxiliary content placed at the trailing end of the tab strip (such as a theme toggle or help button).</summary>
    public object? AuxiliaryContent
    {
        get => GetValue(AuxiliaryContentProperty);
        set => SetValue(AuxiliaryContentProperty, value);
    }

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() => new WorkbenchRibbonTab();

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) => item is WorkbenchRibbonTab;
}
