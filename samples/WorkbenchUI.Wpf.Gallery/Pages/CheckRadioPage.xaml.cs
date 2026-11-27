using System.Windows;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class CheckRadioPage : UserControl
{
    public CheckRadioPage()
    {
        InitializeComponent();

        CheckBoxExample.Xaml = """
            <CheckBox Content="Checked" IsChecked="True" />
            <CheckBox Content="Unchecked" IsChecked="False" />
            <CheckBox Content="Indeterminate" IsChecked="{x:Null}" IsThreeState="True" />
            <CheckBox Content="Disabled" IsChecked="True" IsEnabled="False" />
            """;

        RadioButtonExample.Xaml = """
            <StackPanel>
                <RadioButton Content="Structural (Static)" GroupName="AnalysisMode" IsChecked="True" />
                <RadioButton Content="Modal Analysis" GroupName="AnalysisMode" />
                <RadioButton Content="Thermal Conduction" GroupName="AnalysisMode" />
                <RadioButton Content="Disabled Option" GroupName="AnalysisMode" IsEnabled="False" />
            </StackPanel>
            """;
    }

    private void LiveCheckBox_StateChanged(object sender, RoutedEventArgs e)
    {
        if (CheckStateText is null)
        {
            return;
        }

        var state = LiveCheckBox.IsChecked switch
        {
            true => "True",
            false => "False",
            null => "null (Indeterminate)",
        };
        CheckStateText.Text = $"IsChecked = {state}";
    }

    private void SetIndeterminate_Click(object sender, RoutedEventArgs e)
    {
        LiveCheckBox.IsThreeState = true;
        LiveCheckBox.IsChecked = null;
    }

    private void AnalysisMode_Checked(object sender, RoutedEventArgs e)
    {
        if (RadioSelectionText is not null && sender is RadioButton rb)
        {
            RadioSelectionText.Text = $"Selected mode: {rb.Content}";
        }
    }
}
