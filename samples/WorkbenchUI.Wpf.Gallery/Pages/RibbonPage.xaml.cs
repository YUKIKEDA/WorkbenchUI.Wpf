using System.Windows;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class RibbonPage : UserControl
{
    private readonly List<string> _log = [];

    public RibbonPage()
    {
        InitializeComponent();

        RibbonExample.Xaml = """
            <wb:WorkbenchRibbon ApplicationLabel="File">
                <wb:WorkbenchRibbon.ApplicationMenu>
                    <ContextMenu>
                        <MenuItem Header="_New" />
                        <MenuItem Header="_Open..." />
                    </ContextMenu>
                </wb:WorkbenchRibbon.ApplicationMenu>

                <wb:WorkbenchRibbonTab Header="Analysis">
                    <wb:WorkbenchRibbonGroup Header="Execution">
                        <wb:SplitButton Style="{DynamicResource Wb.Ribbon.SplitButton.Large.Accent}"
                                        wb:ButtonAssist.Icon="{x:Static wb:WorkbenchIcons.Play}"
                                        Content="Run Solve" />
                    </wb:WorkbenchRibbonGroup>
                </wb:WorkbenchRibbonTab>
            </wb:WorkbenchRibbon>
            """;
    }

    private void OnAction(object sender, RoutedEventArgs e)
    {
        var tag = (sender as FrameworkElement)?.Tag as string ?? "(Unknown)";
        _log.Insert(0, $"{DateTime.Now:HH:mm:ss}  {tag}");
        if (_log.Count > 6)
        {
            _log.RemoveAt(_log.Count - 1);
        }

        ActionLog.Text = string.Join(Environment.NewLine, _log);
    }
}
