using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class NavigationViewPage : UserControl
{
    public NavigationViewPage()
    {
        InitializeComponent();

        PlaygroundModeCombo.ItemsSource = Enum.GetValues<NavigationViewPaneDisplayMode>();
        PlaygroundModeCombo.SelectedItem = NavigationViewPaneDisplayMode.Left;

        PlaygroundExample.Xaml = """
            <wb:NavigationView PaneDisplayMode="Left"
                               OpenPaneLength="210"
                               CompactPaneLength="48"
                               SelectionChanged="Nav_SelectionChanged">
                <wb:NavigationView.MenuItems>
                    <wb:NavigationViewItem Content="Overview" Icon="{x:Static wb:WorkbenchIcons.Home}" IsSelected="True" />
                    <wb:NavigationViewItem Content="Geometry" Icon="{x:Static wb:WorkbenchIcons.Cube}" IsExpanded="True">
                        <wb:NavigationViewItem.MenuItems>
                            <wb:NavigationViewItem Content="Parts" Icon="{x:Static wb:WorkbenchIcons.Layers}" Badge="12" />
                            <wb:NavigationViewItem Content="Assemblies" Icon="{x:Static wb:WorkbenchIcons.Folder}" />
                        </wb:NavigationViewItem.MenuItems>
                    </wb:NavigationViewItem>
                    <wb:NavigationViewItem Content="Mesh" Icon="{x:Static wb:WorkbenchIcons.Mesh}" />
                    <wb:NavigationViewItem Content="Results" Icon="{x:Static wb:WorkbenchIcons.Contour}" Badge="Ready" />
                </wb:NavigationView.MenuItems>

                <wb:NavigationView.FooterMenuItems>
                    <wb:NavigationViewItem Content="Settings" Icon="{x:Static wb:WorkbenchIcons.Settings}" />
                </wb:NavigationView.FooterMenuItems>

                <!-- Main workspace content -->
                <ContentControl Content="{Binding ActiveView}" />
            </wb:NavigationView>
            """;

        PlaygroundExample.CSharp = """
            private void Nav_SelectionChanged(object sender, NavigationViewSelectionChangedEventArgs e)
            {
                if (e.SelectedItemContainer is NavigationViewItem item)
                {
                    StatusText.Text = item.LabelText;
                }
            }
            """;

        CompactFlyoutExample.Xaml = """
            <wb:NavigationView PaneDisplayMode="LeftCompact" IsPaneOpen="False">
                <wb:NavigationView.MenuItems>
                    <wb:NavigationViewItem Content="Workspace" Icon="{x:Static wb:WorkbenchIcons.Home}" IsSelected="True" />
                    <wb:NavigationViewItem Content="Analysis" Icon="{x:Static wb:WorkbenchIcons.Play}">
                        <wb:NavigationViewItem.MenuItems>
                            <wb:NavigationViewItem Content="Structural" Icon="{x:Static wb:WorkbenchIcons.Cube}" IsExpanded="True">
                                <wb:NavigationViewItem.MenuItems>
                                    <wb:NavigationViewItem Content="Static Linear" Icon="{x:Static wb:WorkbenchIcons.Contour}" />
                                    <wb:NavigationViewItem Content="Modal Frequencies" Icon="{x:Static wb:WorkbenchIcons.ChartLine}" />
                                </wb:NavigationViewItem.MenuItems>
                            </wb:NavigationViewItem>
                            <wb:NavigationViewItem Content="Thermal" Icon="{x:Static wb:WorkbenchIcons.Sun}" />
                        </wb:NavigationViewItem.MenuItems>
                    </wb:NavigationViewItem>
                </wb:NavigationView.MenuItems>
            </wb:NavigationView>
            """;

        SlotsExample.Xaml = """
            <wb:NavigationView PaneDisplayMode="Left" OpenPaneLength="210">
                <wb:NavigationView.PaneHeader>
                    <TextBlock Text="CAE Studio v2.4" FontWeight="SemiBold" />
                </wb:NavigationView.PaneHeader>

                <wb:NavigationView.MenuItems>
                    <wb:NavigationViewItemHeader Content="MODELING" />
                    <wb:NavigationViewItem Content="CAD Import" Icon="{x:Static wb:WorkbenchIcons.FolderOpen}" IsSelected="True" />
                    <wb:NavigationViewItem Content="Meshing" Icon="{x:Static wb:WorkbenchIcons.Mesh}" />
                    <wb:NavigationViewItemSeparator />
                    <wb:NavigationViewItemHeader Content="SOLVE &amp; JOBS" />
                    <wb:NavigationViewItem Content="Cluster Queue" Icon="{x:Static wb:WorkbenchIcons.Server}" Badge="3" />
                </wb:NavigationView.MenuItems>

                <wb:NavigationView.PaneFooter>
                    <TextBlock Text="License: Floating OK" Margin="10,4,10,8" />
                </wb:NavigationView.PaneFooter>
            </wb:NavigationView>
            """;
    }

    private void PlaygroundModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PlaygroundModeCombo.SelectedItem is NavigationViewPaneDisplayMode mode)
        {
            PlaygroundNav.PaneDisplayMode = mode;
        }
    }

    private void PlaygroundNav_SelectionChanged(object sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (e.SelectedItemContainer is NavigationViewItem item)
        {
            PlaygroundStatusText.Text = string.IsNullOrEmpty(item.LabelText) ? "(Selected)" : item.LabelText;
        }
    }

    private void CompactNav_SelectionChanged(object sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (e.SelectedItemContainer is NavigationViewItem item)
        {
            CompactStatusText.Text = string.IsNullOrEmpty(item.LabelText) ? "(Selected)" : item.LabelText;
        }
    }
}
