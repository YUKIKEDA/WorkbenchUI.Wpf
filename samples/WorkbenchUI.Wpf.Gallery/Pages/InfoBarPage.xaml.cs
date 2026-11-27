using System.Windows;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class InfoBarPage : UserControl
{
    public InfoBarPage()
    {
        InitializeComponent();

        SeverityCombo.ItemsSource = Enum.GetValues<InfoBarSeverity>();
        SeverityCombo.SelectedItem = InfoBarSeverity.Warning;

        PlaygroundExample.Xaml = """
            <wb:InfoBar Severity="Warning"
                        Title="Mesh Quality Notice"
                        Message="24 elements exceed an aspect ratio of 10."
                        IsClosable="True"
                        IsOpen="{Binding ShowMeshWarning, Mode=TwoWay}" />
            """;

        SeveritiesExample.Xaml = """
            <wb:InfoBar Severity="Info" Title="Info" Message="Mesh is up to date." />
            <wb:InfoBar Severity="Success" Title="Completed" Message="Analysis finished successfully." />
            <wb:InfoBar Severity="Warning" Message="Non-closable banner" IsClosable="False" />
            <wb:InfoBar Severity="Error" Title="Error" Message="Failed to connect.">
                <wb:InfoBar.ActionContent>
                    <Button Content="Retry" Click="Retry_Click" />
                </wb:InfoBar.ActionContent>
            </wb:InfoBar>
            """;
    }

    private void SeverityCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PlaygroundBar is not null && SeverityCombo.SelectedItem is InfoBarSeverity severity)
        {
            PlaygroundBar.Severity = severity;
        }
    }

    private void Reopen_Click(object sender, RoutedEventArgs e)
    {
        InfoBarInfo.IsOpen = true;
        InfoBarSuccess.IsOpen = true;
        InfoBarWarning.IsOpen = true;
        InfoBarError.IsOpen = true;
        InfoBarStatus.Text = string.Empty;
    }

    private void InfoBarError_Closed(object sender, RoutedEventArgs e) =>
        InfoBarStatus.Text = "Error InfoBar was closed (Closed event raised).";

    private void Retry_Click(object sender, RoutedEventArgs e) =>
        InfoBarStatus.Text = "Retry action triggered.";
}
