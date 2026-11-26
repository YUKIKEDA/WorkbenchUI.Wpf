using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Gallery.Dialogs;

namespace WorkbenchUI.Wpf.Gallery.Pages
{
    public partial class WindowsPage : UserControl
    {
        public WindowsPage()
        {
            InitializeComponent();
        }

        // ---------------- WorkbenchMessageBox ----------------

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

        // ---------------- WorkbenchWindow / WorkbenchDialogWindow ----------------

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
                Toasts.Show($"Added material \"{dialog.MaterialName}\".", ToastLevel.Success);
            }
        }

        // ---------------- ToastHost ----------------

        private void InfoToast_Click(object sender, RoutedEventArgs e) =>
            Toasts.Show("Generating mesh in the background.");

        private void SuccessToast_Click(object sender, RoutedEventArgs e) =>
            Toasts.Show("Analysis completed.", ToastLevel.Success);

        private void WarningToast_Click(object sender, RoutedEventArgs e) =>
            Toasts.Show("Contact pairs were detected automatically. Please verify settings.", ToastLevel.Warning);

        private void ErrorToast_Click(object sender, RoutedEventArgs e) =>
            Toasts.Show("Lost connection to the license server.", ToastLevel.Error, TimeSpan.FromSeconds(8));

        // ---------------- BusyOverlay ----------------

        private void StartBusy_Click(object sender, RoutedEventArgs e)
        {
            DefaultBusyOverlay.IsBusy = true;
            ProgressBusyOverlay.IsBusy = true;
            BusyProgress.IsRunning = true;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                DefaultBusyOverlay.IsBusy = false;
                ProgressBusyOverlay.IsBusy = false;
                BusyProgress.IsRunning = false;
            };
            timer.Start();
        }
    }
}
