using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class ToggleSwitchPage : UserControl
{
    public ToggleSwitchPage()
    {
        InitializeComponent();

        PlaygroundExample.Xaml = """
            <wb:ToggleSwitch x:Name="AutoSaveSwitch"
                             Content="Auto-save project"
                             IsChecked="True" />
            """;

        StatesExample.Xaml = """
            <StackPanel>
                <wb:ToggleSwitch Content="Live-update results during solve" IsChecked="True" />
                <wb:ToggleSwitch Content="Snap camera to active section plane" />
                <wb:ToggleSwitch Content="Disabled (On)" IsChecked="True" IsEnabled="False" />
            </StackPanel>
            """;
    }

    private void LabelBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (LiveSwitch is not null)
        {
            LiveSwitch.Content = LabelBox.Text;
        }
    }
}
