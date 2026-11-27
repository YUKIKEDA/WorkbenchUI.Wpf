using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class Vector3BoxPage : UserControl
{
    public Vector3BoxPage()
    {
        InitializeComponent();

        PlaygroundExample.Xaml = """
            <wb:Vector3Box X="{Binding GravityX, Mode=TwoWay}"
                           Y="{Binding GravityY, Mode=TwoWay}"
                           Z="{Binding GravityZ, Mode=TwoWay}"
                           Unit="m/s²"
                           Format="G4" />
            """;

        NullableExample.Xaml = """
            <wb:Vector3Box XLabel="R" YLabel="θ" ZLabel="Z"
                           X="25.0" Y="{x:Null}" Z="100.0"
                           Minimum="-500" Maximum="500"
                           Format="F1" />
            """;
    }

    private void UnitCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ForceVector is not null && UnitCombo.SelectedItem is ComboBoxItem item)
        {
            ForceVector.Unit = item.Tag is string tag ? tag : item.Content?.ToString();
        }
    }

    private void FormatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ForceVector is not null && FormatCombo.SelectedItem is ComboBoxItem item)
        {
            ForceVector.Format = item.Content?.ToString() ?? "G4";
        }
    }
}
