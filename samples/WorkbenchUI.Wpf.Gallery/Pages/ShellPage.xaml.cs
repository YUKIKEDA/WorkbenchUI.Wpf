using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages
{
    public partial class ShellPage : UserControl
    {
        private static readonly string[] Parts =
        [
            "Bracket-01", "Bracket-02", "Base-Plate", "Housing-Front", "Housing-Rear",
            "Shaft-Main", "Shaft-Sub", "Bearing-6204", "Bearing-6205", "Bolt-M8x20",
            "Bolt-M10x30", "Nut-M8", "Washer-8", "Cover-Top", "Cover-Bottom",
            "Gasket-A", "Gasket-B", "Flange-DN50", "Pipe-Inlet", "Pipe-Outlet",
        ];

        private readonly ICollectionView _partsView;
        private readonly EventHandler _searchChangedHandler;
        private bool _searchHooked;

        public ShellPage()
        {
            InitializeComponent();

            PartsList.ItemsSource = Parts;
            _partsView = CollectionViewSource.GetDefaultView(PartsList.ItemsSource);
            _searchChangedHandler = (_, _) => ApplyFilter();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!_searchHooked)
            {
                DependencyPropertyDescriptor
                    .FromProperty(SearchBox.SearchTextProperty, typeof(SearchBox))
                    .AddValueChanged(PartsSearch, _searchChangedHandler);
                _searchHooked = true;
            }
        }

        private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_searchHooked)
            {
                DependencyPropertyDescriptor
                    .FromProperty(SearchBox.SearchTextProperty, typeof(SearchBox))
                    .RemoveValueChanged(PartsSearch, _searchChangedHandler);
                _searchHooked = false;
            }
        }

        private void ApplyFilter()
        {
            var text = PartsSearch.SearchText;
            SearchEcho.Text = string.IsNullOrEmpty(text) ? "(none)" : text;
            _partsView.Filter = string.IsNullOrWhiteSpace(text)
                ? null
                : o => o is string s && s.Contains(text.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
