using System.Windows;
using AvalonDock.Themes;

namespace WorkbenchUI.Wpf.Docking;

/// <summary>
/// AvalonDock theme styled with WorkbenchUI.Wpf design tokens.
/// <para>
/// Reuses the VS2013 control templates while dynamically binding colors to <c>Wb.Color.*</c>
/// resources, so runtime theme and accent changes apply automatically.
/// </para>
/// <example>
/// <code language="xaml">
/// &lt;ad:DockingManager&gt;
///     &lt;ad:DockingManager.Theme&gt;
///         &lt;wb:WorkbenchDockTheme /&gt;
///     &lt;/ad:DockingManager.Theme&gt;
///     ...
/// &lt;/ad:DockingManager&gt;
/// </code>
/// </example>
/// </summary>
public class WorkbenchDockTheme : DictionaryTheme
{
    /// <summary>Initializes a new instance of <see cref="WorkbenchDockTheme"/>.</summary>
    public WorkbenchDockTheme()
        : base(new ResourceDictionary
        {
            Source = new Uri(
                "pack://application:,,,/WorkbenchUI.Wpf.Docking;component/Themes/WorkbenchDockResources.xaml"),
        })
    {
    }
}
