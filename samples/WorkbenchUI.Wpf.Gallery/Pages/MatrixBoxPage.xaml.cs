using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class MatrixBoxPage : UserControl
{
    private readonly EventHandler _matrixValuesHandler;
    private bool _isHooked;

    public MatrixBoxPage()
    {
        InitializeComponent();
        _matrixValuesHandler = (_, _) => UpdateMatrixInfo();

        StiffnessMatrix.RowHeaders = new[] { "X", "Y", "Z" };
        StiffnessMatrix.ColumnHeaders = new[] { "X", "Y", "Z" };
        StiffnessMatrix.Values = new[,]
        {
            { 210.0, 80.0, 80.0 },
            { 80.0, 210.0, 80.0 },
            { 80.0, 80.0, 210.0 },
        };

        TransformMatrix.Values = new[,]
        {
            { 1.0, 0.0, 0.0 },
            { 0.0, 1.0, 0.0 },
            { 0.0, 0.0, 1.0 },
        };

        UpdateMatrixInfo();

        SymmetricExample.Xaml = """
            <wb:MatrixBox x:Name="StiffnessMatrix"
                          Width="250"
                          IsSymmetric="True"
                          Format="G4"
                          Values="{Binding ElasticityTensor, Mode=TwoWay}" />
            """;

        SymmetricExample.CSharp = """
            StiffnessMatrix.RowHeaders = new[] { "X", "Y", "Z" };
            StiffnessMatrix.ColumnHeaders = new[] { "X", "Y", "Z" };
            StiffnessMatrix.Values = new[,]
            {
                { 210.0, 80.0, 80.0 },
                { 80.0, 210.0, 80.0 },
                { 80.0, 80.0, 210.0 },
            };
            """;

        TransformExample.Xaml = """
            <wb:MatrixBox Width="220" Format="F2" Values="{Binding RotationMatrix}" />
            """;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_isHooked)
        {
            DependencyPropertyDescriptor
                .FromProperty(MatrixBox.ValuesProperty, typeof(MatrixBox))
                .AddValueChanged(StiffnessMatrix, _matrixValuesHandler);
            _isHooked = true;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_isHooked)
        {
            DependencyPropertyDescriptor
                .FromProperty(MatrixBox.ValuesProperty, typeof(MatrixBox))
                .RemoveValueChanged(StiffnessMatrix, _matrixValuesHandler);
            _isHooked = false;
        }
    }

    private void FormatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StiffnessMatrix is not null && FormatCombo.SelectedItem is ComboBoxItem item)
        {
            StiffnessMatrix.Format = item.Content?.ToString() ?? "G4";
        }
    }

    private void UpdateMatrixInfo()
    {
        if (StiffnessMatrix.Values is not double[,] v)
        {
            return;
        }

        var sb = new StringBuilder("Values =");
        for (var r = 0; r < v.GetLength(0); r++)
        {
            sb.AppendLine().Append("  ");
            for (var c = 0; c < v.GetLength(1); c++)
            {
                sb.Append($"{v[r, c],7:G4}");
            }
        }

        MatrixInfo.Text = sb.ToString();
    }
}
