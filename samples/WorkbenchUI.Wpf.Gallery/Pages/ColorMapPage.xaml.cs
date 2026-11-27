using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class ColorMapPage : UserControl
{
    private const int PixelWidth = 320;
    private const int PixelHeight = 210;

    private readonly ColorScale _previewScale;
    private readonly WriteableBitmap _bitmap;
    private readonly bool _initialized;

    public ColorMapPage()
    {
        InitializeComponent();

        _previewScale = new ColorScale
        {
            ColorMap = ColorMap.Jet,
            Minimum = 0,
            Maximum = 100,
            BelowRangeColor = Colors.Magenta,
            AboveRangeColor = Colors.White,
        };
        PreviewLegend.Scale = _previewScale;

        _bitmap = new WriteableBitmap(PixelWidth, PixelHeight, 96, 96, PixelFormats.Bgra32, null);
        PreviewImage.Source = _bitmap;

        MapSelector.ItemsSource = ColorMap.BuiltIn;
        MapSelector.SelectedIndex = 0;

        ScaleEditor.Scale = EditorLegend.Scale = new ColorScale
        {
            ColorMap = ColorMap.Jet,
            Minimum = 0,
            Maximum = 350,
            LevelCount = 10,
        };

        LegendContinuous.Scale = new ColorScale { ColorMap = ColorMap.Jet, Minimum = 0, Maximum = 250 };
        LegendDiscrete.Scale = new ColorScale { ColorMap = ColorMap.Viridis, Minimum = 0, Maximum = 1, LevelCount = 10 };
        LegendLog.Scale = new ColorScale { ColorMap = ColorMap.Jet, Minimum = 1e-3, Maximum = 1e3, IsLogarithmic = true };
        LegendCoolwarm.Scale = new ColorScale { ColorMap = ColorMap.Coolwarm, Minimum = -50, Maximum = 50 };
        LegendLog.TickCount = 7;

        ContourExample.Xaml = """
            <wb:ColorMapLegend x:Name="PreviewLegend" Height="210" Title="Stress [MPa]" />
            """;

        ContourExample.CSharp = """
            var scale = new ColorScale
            {
                ColorMap = ColorMap.Jet,
                Minimum = 0,
                Maximum = 100,
                BelowRangeColor = Colors.Magenta,
                AboveRangeColor = Colors.White,
            };
            PreviewLegend.Scale = scale;
            Color c = scale.GetColor(value);
            """;

        EditorExample.Xaml = """
            <wb:ColorScaleEditor x:Name="ScaleEditor" Width="300" />
            <wb:ColorMapLegend x:Name="EditorLegend" Height="250" Title="Stress [MPa]" />
            """;

        EditorExample.CSharp = """
            ScaleEditor.Scale = EditorLegend.Scale = new ColorScale
            {
                ColorMap = ColorMap.Jet,
                Minimum = 0,
                Maximum = 350,
                LevelCount = 10,
            };
            """;

        VariationsExample.Xaml = """
            <wb:ColorMapLegend Height="180" Title="Continuous (Jet)" />
            <wb:ColorMapLegend Height="180" Title="Discrete (10 levels)" />
            <wb:ColorMapLegend Height="180" Title="Log (1e-3 to 1e3)" />
            """;

        _initialized = true;
        RenderPreview();
    }

    private void OnSettingsChanged(object sender, RoutedEventArgs e)
    {
        if (!_initialized)
        {
            return;
        }

        if (MapSelector.SelectedItem is ColorMap map)
        {
            _previewScale.ColorMap = map;
        }

        var levels = (int)LevelSlider.Value;
        _previewScale.LevelCount = levels > 0 ? levels : null;
        _previewScale.IsLogarithmic = LogCheck.IsChecked == true;
        _previewScale.Minimum = _previewScale.IsLogarithmic ? 0.1 : 0.0;

        RenderPreview();
    }

    private void RenderPreview()
    {
        var pixels = new int[PixelWidth * PixelHeight];
        for (var y = 0; y < PixelHeight; y++)
        {
            var v = (double)y / PixelHeight * 2 - 1;
            for (var x = 0; x < PixelWidth; x++)
            {
                var u = (double)x / PixelWidth * 2 - 1;
                var value = 55 + 55 * Math.Sin(3.0 * u) * Math.Cos(3.0 * v)
                          + 10 * Math.Exp(-8.0 * ((u + 0.4) * (u + 0.4) + (v - 0.3) * (v - 0.3)));

                var color = _previewScale.GetColor(value);
                pixels[y * PixelWidth + x] =
                    (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
            }
        }

        _bitmap.WritePixels(
            new Int32Rect(0, 0, PixelWidth, PixelHeight),
            pixels, PixelWidth * 4, 0);
    }
}
