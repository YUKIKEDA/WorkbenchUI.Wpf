using System.Windows;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Gallery.Dialogs;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class WindowsPage : UserControl
{
    public WindowsPage()
    {
        InitializeComponent();

        MessageBoxExample.CSharp = """
            var result = WorkbenchMessageBox.Show(
                Window.GetWindow(this),
                "The model has been modified. Do you want to save?",
                "Confirmation",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);
            """;

        WindowExample.Xaml = """
            <wb:WorkbenchWindow Title="My Tool" Width="900" Height="600">
                <wb:WorkbenchWindow.TitleBarContent>
                    <Menu>...</Menu>
                </wb:WorkbenchWindow.TitleBarContent>
                <!-- Workspace content -->
            </wb:WorkbenchWindow>
            """;

        WindowExample.CSharp = """
            var dialog = new SampleDialog { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                AddMaterial(dialog.MaterialName);
            }
            """;
    }

    private void InfoMessage_Click(object sender, RoutedEventArgs e)
    {
        var result = WorkbenchMessageBox.Show(
            Window.GetWindow(this),
            "Analysis completed successfully.\nResult file: results/case01.frd",
            "Analysis Complete",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        MessageResult.Text = result.ToString();
    }

    private void ConfirmMessage_Click(object sender, RoutedEventArgs e)
    {
        var result = WorkbenchMessageBox.Show(
            Window.GetWindow(this),
            "The model has been modified. Do you want to save?",
            "Confirmation",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);
        MessageResult.Text = result.ToString();
    }

    private void WarningMessage_Click(object sender, RoutedEventArgs e)
    {
        var result = WorkbenchMessageBox.Show(
            Window.GetWindow(this),
            "There are 12 elements with poor mesh quality. Continue solving anyway?",
            "Warning",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        MessageResult.Text = result.ToString();
    }

    private void ErrorMessage_Click(object sender, RoutedEventArgs e)
    {
        var result = WorkbenchMessageBox.Show(
            Window.GetWindow(this),
            "Solver terminated abnormally (exit code: -1073741819).\nPlease check the log.",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        MessageResult.Text = result.ToString();
    }

    private void OpenToolWindow_Click(object sender, RoutedEventArgs e)
    {
        var menu = new Menu { VerticalAlignment = VerticalAlignment.Center };
        var fileMenu = new MenuItem { Header = "File" };
        fileMenu.Items.Add(new MenuItem { Header = "Open..." });
        fileMenu.Items.Add(new MenuItem { Header = "Save" });
        var viewMenu = new MenuItem { Header = "View" };
        viewMenu.Items.Add(new MenuItem { Header = "Wireframe", IsCheckable = true });
        menu.Items.Add(fileMenu);
        menu.Items.Add(viewMenu);

        var window = new WorkbenchWindow
        {
            Title = "Tool Window",
            Width = 520,
            Height = 340,
            Owner = Window.GetWindow(this),
            TitleBarContent = menu,
            Content = new TextBlock
            {
                Text = "Example of WorkbenchWindow with a Menu embedded in the title bar.\n"
                     + "Maximize, snap, and resize retain standard OS behavior.",
                Margin = new Thickness(16),
                TextWrapping = TextWrapping.Wrap,
            },
        };
        window.Show();
    }

    private void OpenDialog_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SampleDialog { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
        {
            DialogResultText.Text = $"Dialog confirmed: added material \"{dialog.MaterialName}\".";
        }
        else
        {
            DialogResultText.Text = "Dialog cancelled.";
        }
    }
}
