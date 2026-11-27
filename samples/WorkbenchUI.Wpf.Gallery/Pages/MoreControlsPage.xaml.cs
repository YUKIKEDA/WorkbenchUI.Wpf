using System.Windows;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages
{
    public partial class MoreControlsPage : UserControl
    {
        private sealed record Material(string Name, double YoungsModulus, double PoissonsRatio, double Density);

        public MoreControlsPage()
        {
            InitializeComponent();

            MaterialList.ItemsSource = new[]
            {
                new Material("Structural Steel (S45C)", 205, 0.29, 7850),
                new Material("Aluminum Alloy (A6061-T6)", 68.9, 0.33, 2700),
                new Material("Titanium Alloy (Ti-6Al-4V)", 113.8, 0.342, 4430),
                new Material("Copper (C1100)", 117, 0.34, 8940),
                new Material("PEEK", 3.6, 0.38, 1300),
                new Material("Silicon Nitride (Si3N4)", 310, 0.27, 3250),
            };
        }

        private void Reopen_Click(object sender, RoutedEventArgs e)
        {
            InfoBarInfo.IsOpen = true;
            InfoBarSuccess.IsOpen = true;
            InfoBarWarning.IsOpen = true;
            InfoBarError.IsOpen = true;
            InfoBarStatus.Text = "";
        }

        private void InfoBar_Closed(object sender, RoutedEventArgs e)
        {
            InfoBarStatus.Text = "Error InfoBar was closed (Closed event).";
        }

        private void Retry_Click(object sender, RoutedEventArgs e)
        {
            InfoBarStatus.Text = "Retry action triggered.";
        }

        private void HelpLink_Click(object sender, RoutedEventArgs e)
        {
            LinkStatus.Text = "Help link clicked.";
        }
    }
}
