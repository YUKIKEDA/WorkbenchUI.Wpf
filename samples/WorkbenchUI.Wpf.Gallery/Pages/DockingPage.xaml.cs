using System.Windows;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class DockingPage : UserControl
{
    public DockingPage()
    {
        InitializeComponent();

        ThemeExample.Xaml = """
            <avalondock:DockingManager x:Name="DockManager">
                <avalondock:DockingManager.Theme>
                    <dock:WorkbenchDockTheme />
                </avalondock:DockingManager.Theme>
                <avalondock:LayoutRoot>
                    <avalondock:LayoutPanel Orientation="Horizontal">
                        <avalondock:LayoutAnchorablePane DockWidth="260">
                            <avalondock:LayoutAnchorable ContentId="ModelTree" Title="Model Tree" />
                        </avalondock:LayoutAnchorablePane>
                        <avalondock:LayoutDocumentPane />
                    </avalondock:LayoutPanel>
                </avalondock:LayoutRoot>
            </avalondock:DockingManager>
            """;

        ThemeExample.CSharp = """
            // Save current pane arrangement to XML
            string xml = DockLayout.SaveToString(DockManager);

            // Restore layout, resolving panes by ContentId
            DockLayout.LoadFromString(DockManager, xml, id => ResolvePaneContent(id));
            """;
    }

    private void OnLaunchShell(object sender, RoutedEventArgs e)
    {
        var window = new DockingShellWindow
        {
            Owner = Window.GetWindow(this),
        };
        window.Show();
    }
}
