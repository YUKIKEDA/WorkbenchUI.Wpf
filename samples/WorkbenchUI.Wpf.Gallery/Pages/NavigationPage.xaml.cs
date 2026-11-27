using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class NavigationPage : UserControl
{
    public NavigationPage()
    {
        InitializeComponent();

        for (var i = 1; i <= 30; i++)
        {
            PartsList.Items.Add($"Part-{i:D3} (Solid)");
        }

        MenuExample.Xaml = """
            <Menu>
                <MenuItem Header="_File">
                    <MenuItem Header="_New Project" InputGestureText="Ctrl+N" />
                    <Separator />
                    <MenuItem Header="E_xit" InputGestureText="Alt+F4" />
                </MenuItem>
            </Menu>
            """;

        TabExample.Xaml = """
            <TabControl>
                <TabItem Header="Geometry">...</TabItem>
                <TabItem Header="Mesh">...</TabItem>
                <TabItem Header="Disabled" IsEnabled="False" />
            </TabControl>
            """;

        ListExample.Xaml = """
            <ListBox ItemsSource="{Binding Parts}" Height="160" Width="280" />
            <Button Content="Hover" ToolTip="Young's Modulus E = 205 GPa" />
            """;
    }
}
