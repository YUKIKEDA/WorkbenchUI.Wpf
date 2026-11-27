using System.Windows;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class ToastHostPage : UserControl
{
    public ToastHostPage()
    {
        InitializeComponent();

        LevelCombo.ItemsSource = Enum.GetValues<ToastLevel>();
        LevelCombo.SelectedItem = ToastLevel.Info;

        ToastExample.Xaml = """
            <Grid>
                <!-- Page layout content -->
                <ScrollViewer>...</ScrollViewer>

                <!-- Toast overlay placed on top at bottom-right -->
                <wb:ToastHost x:Name="Toasts" />
            </Grid>
            """;

        ToastExample.CSharp = """
            Toasts.Show("Generating mesh in the background.");
            Toasts.Show("Analysis completed.", ToastLevel.Success);
            Toasts.Show("License warning.", ToastLevel.Warning);
            Toasts.Show("Connection lost.", ToastLevel.Error, TimeSpan.FromSeconds(8));
            """;
    }

    private void InfoToast_Click(object sender, RoutedEventArgs e) =>
        Toasts.Show("Generating mesh in the background.");

    private void SuccessToast_Click(object sender, RoutedEventArgs e) =>
        Toasts.Show("Analysis completed.", ToastLevel.Success);

    private void WarningToast_Click(object sender, RoutedEventArgs e) =>
        Toasts.Show("Contact pairs were detected automatically. Please verify settings.", ToastLevel.Warning);

    private void ErrorToast_Click(object sender, RoutedEventArgs e) =>
        Toasts.Show("Lost connection to the license server.", ToastLevel.Error, TimeSpan.FromSeconds(8));

    private void ShowCustomToast_Click(object sender, RoutedEventArgs e)
    {
        var level = LevelCombo.SelectedItem is ToastLevel l ? l : ToastLevel.Info;
        var seconds = DurationCombo.SelectedItem is ComboBoxItem { Tag: string raw } && double.TryParse(raw, out var s)
            ? s
            : 4.0;
        var msg = string.IsNullOrWhiteSpace(CustomMessageBox.Text) ? "Notification" : CustomMessageBox.Text;
        Toasts.Show(msg, level, TimeSpan.FromSeconds(seconds));
    }
}
