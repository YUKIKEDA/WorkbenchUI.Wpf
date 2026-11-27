using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class DataGridPage : UserControl
{
    private static readonly string[] MaterialTypes = ["Metal", "Polymer", "Composite", "Ceramic"];

    private readonly ObservableCollection<MaterialRow> _materials;

    public DataGridPage()
    {
        InitializeComponent();

        _materials =
        [
            new("Steel (S45C)", "Metal", 205, 0.29, 7850),
            new("Aluminum (A5052)", "Metal", 70.3, 0.33, 2680),
            new("Titanium (Ti-6Al-4V)", "Metal", 113.8, 0.34, 4430),
            new("Copper (C1100)", "Metal", 117, 0.34, 8940),
            new("Stainless (SUS304)", "Metal", 193, 0.29, 8000),
            new("Cast Iron (FC250)", "Metal", 100, 0.26, 7200),
            new("ABS", "Polymer", 2.3, 0.35, 1050),
            new("Polycarbonate", "Polymer", 2.4, 0.37, 1200),
            new("PEEK", "Polymer", 3.6, 0.38, 1300),
            new("Nylon 66", "Polymer", 2.9, 0.39, 1140),
            new("CFRP (UD 0°)", "Composite", 135, 0.30, 1600),
            new("GFRP", "Composite", 38.6, 0.26, 1900),
            new("Alumina (Al2O3)", "Ceramic", 370, 0.22, 3900),
            new("Silicon Carbide", "Ceramic", 410, 0.14, 3100),
            new("Zirconia", "Ceramic", 200, 0.31, 6050),
        ];

        TypeColumn.ItemsSource = MaterialTypes;
        Grid.ItemsSource = _materials;
        MaterialList.ItemsSource = _materials.Take(6).ToList();

        DataGridExample.Xaml = """
            <DataGrid x:Name="Grid"
                      AutoGenerateColumns="False"
                      HeadersVisibility="All"
                      CanUserAddRows="False"
                      RowDetailsTemplate="{StaticResource MaterialDetailsTemplate}">
                <DataGrid.GroupStyle>
                    <GroupStyle ContainerStyle="{StaticResource Wb.DataGrid.GroupItemStyle}" />
                </DataGrid.GroupStyle>
                <DataGrid.Columns>
                    <DataGridTextColumn Header="Name" Width="*" Binding="{Binding Name}" />
                    <DataGridTextColumn Header="ν" Binding="{Binding PoissonRatio, ValidatesOnDataErrors=True}" />
                </DataGrid.Columns>
            </DataGrid>
            """;

        DataGridExample.CSharp = """
            // MaterialRow implements IDataErrorInfo for cell + row-header validation
            public string this[string columnName] => columnName switch
            {
                nameof(PoissonRatio) when _poissonRatio is <= -1 or >= 0.5
                    => "Poisson's ratio must satisfy -1 < ν < 0.5.",
                _ => string.Empty,
            };
            """;

        ListViewExample.Xaml = """
            <ListView ItemsSource="{Binding Materials}" Height="180">
                <ListView.View>
                    <GridView>
                        <GridViewColumn Header="Material" Width="170" DisplayMemberBinding="{Binding Name}" />
                        <GridViewColumn Header="Young's Modulus [GPa]" Width="150" DisplayMemberBinding="{Binding YoungsModulus}" />
                    </GridView>
                </ListView.View>
            </ListView>
            """;
    }

    private void DetailsCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (Grid is null)
        {
            return;
        }

        var enabled = DetailsCheck.IsChecked == true;
        DetailsColumn.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        Grid.RowDetailsTemplate = enabled
            ? (DataTemplate)Resources["MaterialDetailsTemplate"]
            : null;

        if (!enabled)
        {
            foreach (var item in Grid.Items)
            {
                if (Grid.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow row)
                {
                    row.ClearValue(DataGridRow.DetailsVisibilityProperty);
                }
            }
        }
    }

    private void GroupCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (Grid?.ItemsSource is null)
        {
            return;
        }

        var view = CollectionViewSource.GetDefaultView(Grid.ItemsSource);
        view.GroupDescriptions.Clear();
        if (GroupCheck.IsChecked == true)
        {
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(MaterialRow.Type)));
        }
    }
}

/// <summary>Demo material row using IDataErrorInfo to surface property validation errors.</summary>
public sealed class MaterialRow : INotifyPropertyChanged, IDataErrorInfo
{
    private string _name;
    private string _type;
    private double _youngsModulus;
    private double _poissonRatio;
    private double _density;
    private bool _isActive = true;

    public MaterialRow(string name, string type, double youngsModulus, double poissonRatio, double density)
    {
        _name = name;
        _type = type;
        _youngsModulus = youngsModulus;
        _poissonRatio = poissonRatio;
        _density = density;
    }

    public string Name
    {
        get => _name;
        set => Set(ref _name, value);
    }

    public string Type
    {
        get => _type;
        set => Set(ref _type, value);
    }

    public double YoungsModulus
    {
        get => _youngsModulus;
        set
        {
            if (Set(ref _youngsModulus, value))
            {
                OnPropertyChanged(nameof(ShearModulus));
            }
        }
    }

    public double PoissonRatio
    {
        get => _poissonRatio;
        set
        {
            if (Set(ref _poissonRatio, value))
            {
                OnPropertyChanged(nameof(ShearModulus));
            }
        }
    }

    public double Density
    {
        get => _density;
        set => Set(ref _density, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => Set(ref _isActive, value);
    }

    /// <summary>Shear modulus G = E / 2(1+ν) (displayed in RowDetails).</summary>
    public double ShearModulus => _youngsModulus / (2 * (1 + _poissonRatio));

    public string Error => string.Empty;

    public string this[string columnName] => columnName switch
    {
        nameof(YoungsModulus) when _youngsModulus <= 0 => "Young's modulus must be positive.",
        nameof(PoissonRatio) when _poissonRatio is <= -1 or >= 0.5 => "Poisson's ratio must satisfy -1 < ν < 0.5.",
        nameof(Density) when _density <= 0 => "Density must be positive.",
        _ => string.Empty,
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged(string? propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
