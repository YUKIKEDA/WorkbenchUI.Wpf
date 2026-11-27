using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class ColorPickerPage : UserControl
{
    public ColorPickerPage()
    {
        InitializeComponent();

        PickerExample.Xaml = """
            <wb:ColorPicker Width="80" SelectedColor="#CC0078D7" />
            <wb:ColorPicker Width="80" SelectedColor="#E81123" IsAlphaEnabled="False" />
            """;

        EditorExample.Xaml = """
            <wb:ColorEditor Width="300"
                            SelectedColor="{Binding SelectedColor, Mode=TwoWay}" />
            """;
    }
}
