using System.Globalization;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class NumericBoxPage : UserControl
{
    public NumericBoxPage()
    {
        InitializeComponent();
        LengthBox.UnitProvider = new LengthUnitProvider();

        PlaygroundExample.Xaml = """
            <wb:NumericBox Width="200"
                           Value="{Binding Thickness, Mode=TwoWay}"
                           Minimum="0"
                           Maximum="100"
                           Increment="5"
                           Format="G6"
                           Unit="mm" />
            """;

        UnitConversionExample.Xaml = """
            <wb:NumericBox x:Name="LengthBox"
                           Width="200"
                           Value="0.05"
                           Minimum="0"
                           Maximum="1"
                           Increment="10" />
            """;

        UnitConversionExample.CSharp = """
            LengthBox.UnitProvider = new LengthUnitProvider();

            public class LengthUnitProvider : IUnitProvider
            {
                public string DisplayUnit => "mm";
                public double ToDisplay(double baseValue) => baseValue * 1000;
                public double FromDisplay(double displayValue) => displayValue / 1000;

                public bool TryConvertFrom(double value, string unitSymbol, out double baseValue)
                {
                    baseValue = unitSymbol.ToLowerInvariant() switch
                    {
                        "mm" => value * 1e-3,
                        "cm" => value * 1e-2,
                        "m" => value,
                        _ => double.NaN
                    };
                    return !double.IsNaN(baseValue);
                }
            }
            """;

        ValidationExample.Xaml = """
            <wb:NumericBox Width="180" Value="0.00125" Format="G4" Unit="m" />
            <wb:NumericBox Width="180" Value="200" Minimum="0" Maximum="1000" Unit="MPa" />
            """;
    }

    private void UnitCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PlaygroundBox is null || UnitCombo.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        PlaygroundBox.Unit = item.Tag is string tag ? tag : item.Content?.ToString();
    }

    private void FormatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PlaygroundBox is null || FormatCombo.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        PlaygroundBox.Format = item.Content?.ToString() ?? "G";
    }

    private void IncrementCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PlaygroundBox is null || IncrementCombo.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        if (double.TryParse(item.Tag?.ToString(), CultureInfo.InvariantCulture, out var inc))
        {
            PlaygroundBox.Increment = inc;
        }
    }
}

/// <summary>Length unit provider converting between base meters and display millimeters (demo).</summary>
public class LengthUnitProvider : IUnitProvider
{
    private static readonly Dictionary<string, double> FactorsToMeter = new(StringComparer.OrdinalIgnoreCase)
    {
        ["mm"] = 1e-3,
        ["cm"] = 1e-2,
        ["m"] = 1.0,
        ["µm"] = 1e-6,
        ["um"] = 1e-6,
    };

    public string DisplayUnit => "mm";

    public double ToDisplay(double baseValue) => baseValue * 1000;

    public double FromDisplay(double displayValue) => displayValue / 1000;

    public bool TryConvertFrom(double value, string unitSymbol, out double baseValue)
    {
        if (FactorsToMeter.TryGetValue(unitSymbol, out var factor))
        {
            baseValue = value * factor;
            return true;
        }

        baseValue = 0;
        return false;
    }
}
