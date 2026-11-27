using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class SlidersPage : UserControl
{
    private readonly ColorScale _scale = new()
    {
        ColorMap = ColorMap.Jet,
        Minimum = 100,
        Maximum = 350,
        BelowRangeColor = Colors.Magenta,
        AboveRangeColor = Colors.White,
    };

    public SlidersPage()
    {
        InitializeComponent();

        RangeLegend.Scale = _scale;
        ContourRange.SetBinding(RangeSlider.LowerValueProperty,
            new Binding(nameof(ColorScale.Minimum)) { Source = _scale, Mode = BindingMode.TwoWay });
        ContourRange.SetBinding(RangeSlider.UpperValueProperty,
            new Binding(nameof(ColorScale.Maximum)) { Source = _scale, Mode = BindingMode.TwoWay });

        SliderExample.Xaml = """
            <Slider Minimum="0" Maximum="100" Value="40" TickFrequency="10" IsSnapToTickEnabled="False" />
            """;

        RangeSliderExample.Xaml = """
            <wb:RangeSlider Minimum="0"
                            Maximum="500"
                            LowerValue="{Binding MinStress, Mode=TwoWay}"
                            UpperValue="{Binding MaxStress, Mode=TwoWay}" />

            <wb:RangeSlider Orientation="Vertical"
                            Height="160"
                            Minimum="0" Maximum="1"
                            LowerValue="0.25" UpperValue="0.75" />
            """;

        RangeSliderExample.CSharp = """
            ContourRange.SetBinding(RangeSlider.LowerValueProperty,
                new Binding(nameof(ColorScale.Minimum)) { Source = scale, Mode = BindingMode.TwoWay });
            ContourRange.SetBinding(RangeSlider.UpperValueProperty,
                new Binding(nameof(ColorScale.Maximum)) { Source = scale, Mode = BindingMode.TwoWay });
            """;
    }

    private void MaxCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DemoSlider is not null && MaxCombo.SelectedItem is ComboBoxItem { Tag: string raw } && double.TryParse(raw, out var max))
        {
            DemoSlider.Maximum = max;
        }
    }
}
