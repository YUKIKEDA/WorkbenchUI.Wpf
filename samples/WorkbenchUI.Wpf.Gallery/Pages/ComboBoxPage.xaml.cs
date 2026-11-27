using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class ComboBoxPage : UserControl
{
    public sealed record LoadCase(string Name);

    private readonly ObservableCollection<string> _selectedComponents = [];
    private readonly ObservableCollection<LoadCase> _selectedCases = [];

    public ComboBoxPage()
    {
        InitializeComponent();

        ComponentCombo.ItemsSource = new[]
        {
            "Sx", "Sy", "Sz", "Sxy", "Syz", "Szx", "Von Mises", "Principal P1", "Principal P2", "Principal P3",
        };
        ComponentCombo.SelectedItems = _selectedComponents;
        _selectedComponents.CollectionChanged += OnSelectionChanged;
        _selectedComponents.Add("Sx");
        _selectedComponents.Add("Von Mises");

        var cases = new[]
        {
            new LoadCase("Gravity"),
            new LoadCase("Wind X"),
            new LoadCase("Wind Y"),
            new LoadCase("Seismic X"),
            new LoadCase("Seismic Y"),
            new LoadCase("Thermal"),
        };
        CaseCombo.ItemsSource = cases;
        CaseCombo.SelectedItems = _selectedCases;
        _selectedCases.Add(cases[0]);
        _selectedCases.Add(cases[3]);

        UpdateComboInfo();

        ComboBoxExample.Xaml = """
            <ComboBox Width="190" SelectedIndex="0">
                <ComboBoxItem Content="Structural (Static)" />
                <ComboBoxItem Content="Modal Analysis" />
            </ComboBox>
            <ComboBox Width="190" IsEditable="True" Text="Aluminum A5052" />
            """;

        CheckComboExample.Xaml = """
            <wb:CheckComboBox x:Name="ComponentCombo"
                              SummaryFormat="{}{0} component(s) selected"
                              SelectAllContent="(Select All)"
                              PlaceholderText="Select components..." />

            <wb:CheckComboBox x:Name="CaseCombo"
                              DisplayMemberPath="Name"
                              PlaceholderText="Select load cases..." />
            """;

        CheckComboExample.CSharp = """
            ComponentCombo.ItemsSource = new[] { "Sx", "Sy", "Sz", "Von Mises" };
            ComponentCombo.SelectedItems = selectedComponents; // ObservableCollection<string>
            """;
    }

    private void OnSelectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        UpdateComboInfo();

    private void UpdateComboInfo() =>
        ComboInfo.Text = $"SelectedItems: [{string.Join(", ", _selectedComponents)}]";
}
