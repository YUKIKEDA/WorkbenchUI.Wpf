using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class ShellPage : UserControl
{
    public ShellPage()
    {
        InitializeComponent();

        PartsList.ItemsSource = new[]
        {
            "Bracket-01", "Bracket-02", "Base-Plate", "Housing-Front", "Housing-Rear",
            "Shaft-Main", "Shaft-Sub", "Bearing-6204", "Flange-DN50", "Pipe-Inlet",
        };

        ShellExample.Xaml = """
            <DockPanel>
                <ToolBarTray DockPanel.Dock="Top">
                    <ToolBar>
                        <Button ToolTip="Open" wb:ButtonAssist.Icon="{x:Static wb:WorkbenchIcons.FolderOpen}" />
                        <Button ToolTip="Save" wb:ButtonAssist.Icon="{x:Static wb:WorkbenchIcons.Save}" />
                    </ToolBar>
                </ToolBarTray>

                <StatusBar DockPanel.Dock="Bottom">
                    <StatusBarItem Content="Ready" />
                    <StatusBarItem DockPanel.Dock="Right" Content="X: 12.5  Y: -3.2  Z: 0.0" />
                </StatusBar>

                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="220" MinWidth="120" />
                        <ColumnDefinition Width="6" />
                        <ColumnDefinition Width="*" MinWidth="120" />
                    </Grid.ColumnDefinitions>
                    <GroupBox Grid.Column="0" Header="Model Parts">...</GroupBox>
                    <GridSplitter Grid.Column="1" HorizontalAlignment="Stretch" ResizeBehavior="PreviousAndNext" />
                    <GroupBox Grid.Column="2" Header="Viewport">...</GroupBox>
                </Grid>
            </DockPanel>
            """;
    }
}
