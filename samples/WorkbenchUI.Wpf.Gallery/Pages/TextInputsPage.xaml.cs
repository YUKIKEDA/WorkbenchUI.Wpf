using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class TextInputsPage : UserControl
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

    public TextInputsPage()
    {
        InitializeComponent();
        DataContext = new ValidationDemoModel();

        PartsList.ItemsSource = Parts;
        _partsView = CollectionViewSource.GetDefaultView(PartsList.ItemsSource);
        _searchChangedHandler = (_, _) => ApplyFilter();

        TextBoxExample.Xaml = """
            <TextBox Text="Editable text" />
            <TextBox Text="Read-only" IsReadOnly="True" />
            <TextBox Text="{Binding Thickness, ValidatesOnExceptions=True, UpdateSourceTrigger=PropertyChanged}" />
            """;

        TextBoxExample.CSharp = """
            public double Thickness
            {
                get => _thickness;
                set
                {
                    if (value is < 0 or > 100)
                        throw new ArgumentException("Value must be between 0 and 100.");
                    _thickness = value;
                }
            }
            """;

        SearchBoxExample.Xaml = """
            <wb:SearchBox x:Name="PartsSearch"
                          Width="320"
                          Placeholder="Filter parts..."
                          SearchDelay="0:0:0.2" />
            """;

        SearchBoxExample.CSharp = """
            // Bind or react to debounced SearchBox.SearchText
            view.Filter = o => o is string s &&
                s.Contains(PartsSearch.SearchText.Trim(), StringComparison.OrdinalIgnoreCase);
            """;

        RichTextExample.Xaml = """
            <PasswordBox Password="secret123" />
            <RichTextBox Height="88" />
            <Label Content="_Solver:" Target="{Binding ElementName=SolverCombo}" />
            <TextBlock><Hyperlink Click="Help_Click">Help Link</Hyperlink></TextBlock>
            """;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_searchHooked)
        {
            DependencyPropertyDescriptor
                .FromProperty(SearchBox.SearchTextProperty, typeof(SearchBox))
                .AddValueChanged(PartsSearch, _searchChangedHandler);
            _searchHooked = true;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
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

    private void HelpLink_Click(object sender, RoutedEventArgs e) =>
        LinkStatus.Text = "Solver Configuration Help link clicked.";
}

/// <summary>Demo model for validation error display (red border + tooltip).</summary>
public class ValidationDemoModel
{
    private double _thickness = 3.2;

    public double Thickness
    {
        get => _thickness;
        set
        {
            if (value is < 0 or > 100)
            {
                throw new ArgumentException("Value must be between 0 and 100.");
            }

            _thickness = value;
        }
    }
}
