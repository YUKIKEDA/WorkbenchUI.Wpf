using System.Windows.Media;
using WorkbenchUI.Wpf.Charts;

namespace WorkbenchUI.Wpf.Charts.Tests;

public class WorkbenchChartThemeTests
{
    [Fact]
    public void ToScottPlotColor_PreservesRgba()
    {
        var wpfColor = Color.FromArgb(0x80, 0x12, 0x34, 0x56);
        var spColor = WorkbenchChartTheme.ToScottPlotColor(wpfColor);

        Assert.Equal(0x12, spColor.R);
        Assert.Equal(0x34, spColor.G);
        Assert.Equal(0x56, spColor.B);
        Assert.Equal(0x80, spColor.A);
    }

    [Fact]
    public void GetSeriesPalette_ReturnsEightColors()
    {
        var palette = WorkbenchChartTheme.GetSeriesPalette();
        Assert.Equal(8, palette.Length);
    }

    [Fact]
    public void Apply_NullPlot_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => WorkbenchChartTheme.Apply(null!));
    }

    [Fact]
    public void Apply_ConfiguresPlotThemeWithoutApplication()
    {
        var plot = new ScottPlot.Plot();
        WorkbenchChartTheme.Apply(plot);

        Assert.Equal("Workbench", plot.Add.Palette.Name);
        Assert.Equal(ScottPlot.Color.FromHex("#1E1E1E"), plot.FigureBackground.Color);
        Assert.Equal(ScottPlot.Color.FromHex("#252526"), plot.DataBackground.Color);
    }
}
