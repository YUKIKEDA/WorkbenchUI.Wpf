using System.Windows;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Theming;
using WorkbenchUI.Wpf.Gallery.Pages;

namespace WorkbenchUI.Wpf.Gallery
{
    public partial class MainWindow : WorkbenchWindow
    {
        private static readonly (string Title, Func<object> CreatePage)[] Pages =
        [
            ("Design Tokens", () => new TokensPage()),
            ("Icons", () => new IconsPage()),
            ("Inputs & Buttons", () => new InputsPage()),
            ("Ribbon", () => new RibbonPage()),
            ("Navigation & Menus", () => new NavigationPage()),
            ("NumericBox & Expander", () => new NumericPage()),
            ("Pickers & Range", () => new PickersPage()),
            ("PropertyGrid", () => new PropertyGridPage()),
            ("ModelTree", () => new ModelTreePage()),
            ("ColorMap Legend", () => new ColorMapPage()),
            ("Post-processing", () => new PostProcessingPage()),
            ("LogConsole & Progress", () => new LogConsolePage()),
            ("Shell", () => new ShellPage()),
            ("Windows & Dialogs", () => new WindowsPage()),
            ("DataGrid", () => new DataGridPage()),
            ("More Controls", () => new MoreControlsPage()),
            ("Misc Inputs & Wizard", () => new MiscInputsPage()),
            ("Docking", () => new DockingPage()),
            ("Charts", () => new ChartsPage()),
            ("3D Viewport", () => new Viewport3DPage()),
            ("3D Deformation", () => new ViewportDeformationPage()),
            ("3D Section", () => new ViewportSectionPage()),
            ("3D Probe", () => new ViewportProbePage()),
            ("3D Glyphs", () => new ViewportGlyphsPage()),
            ("3D Voxels", () => new ViewportVoxelsPage()),
            ("3D Benchmark", () => new ViewportBenchmarkPage()),
        ];

        public MainWindow()
        {
            InitializeComponent();

            foreach (var (title, _) in Pages)
            {
                NavList.Items.Add(title);
            }

            NavList.SelectedIndex = 0;

            // --light: Launch with the Light theme (for visual inspection and screenshots)
            if (Environment.GetCommandLineArgs().Contains("--light"))
            {
                ThemeToggle.IsChecked = true;
            }

            // --msgbox: Show WorkbenchMessageBox immediately on startup (for visual inspection and screenshots)
            if (Environment.GetCommandLineArgs().Contains("--msgbox"))
            {
                Loaded += (_, _) => Dispatcher.BeginInvoke(() => WorkbenchMessageBox.Show(
                    this,
                    "The model has been modified. Do you want to save?",
                    "Confirmation",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question));
            }

            // --dockshell: Open the docking shell demo immediately on startup (for visual inspection and screenshots)
            if (Environment.GetCommandLineArgs().Contains("--dockshell"))
            {
                Loaded += (_, _) => Dispatcher.BeginInvoke(() =>
                    new DockingShellWindow { Owner = this }.Show());
            }

            // --smoke: Instantiate every page once to catch XAML/resource resolution errors and exit immediately
            if (Environment.GetCommandLineArgs().Contains("--smoke"))
            {
                foreach (var (_, createPage) in Pages)
                {
                    _ = createPage();
                }

                // Also include the docking shell in XAML and theme resolution verification
                _ = new DockingShellWindow();

                Application.Current.Shutdown(0);
            }
        }

        private void ThemeToggle_Checked(object sender, RoutedEventArgs e) =>
            ThemeManager.SetTheme(WorkbenchThemeVariant.Light);

        private void ThemeToggle_Unchecked(object sender, RoutedEventArgs e) =>
            ThemeManager.SetTheme(WorkbenchThemeVariant.Dark);

        private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavList.SelectedIndex >= 0)
            {
                PageHost.Content = Pages[NavList.SelectedIndex].CreatePage();
            }
        }
    }
}
