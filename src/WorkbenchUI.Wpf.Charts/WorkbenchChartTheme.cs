using System.Windows;

namespace WorkbenchUI.Wpf.Charts;

/// <summary>
/// Applies WorkbenchUI.Wpf design token colors to a <see cref="ScottPlot.Plot"/>.
/// </summary>
public static class WorkbenchChartTheme
{
    /// <summary>
    /// Returns the default series color palette (starting with the active accent color),
    /// tuned for contrast against the current Dark or Light background.
    /// </summary>
    public static ScottPlot.Color[] GetSeriesPalette()
    {
        var accent = GetColor("Wb.Color.Accent.Default", "#007ACC");
        return IsLightBackground()
            ?
            [
                accent,
                FromHex("#107C10"), // green
                FromHex("#9D5D00"), // amber
                FromHex("#C42B1C"), // red
                FromHex("#005FB8"), // blue
                FromHex("#6936AA"), // purple
                FromHex("#00776E"), // teal
                FromHex("#CA5010"), // orange
            ]
            :
            [
                accent,
                FromHex("#89D185"), // green
                FromHex("#CCA700"), // yellow
                FromHex("#F14C4C"), // red
                FromHex("#75BEFF"), // light blue
                FromHex("#B180D7"), // purple
                FromHex("#4EC9B0"), // teal
                FromHex("#CE9178"), // orange
            ];
    }

    /// <summary>
    /// Applies the active WorkbenchUI.Wpf theme colors to a <see cref="ScottPlot.Plot"/>
    /// (background, axes, grid, legend, and series palette).
    /// </summary>
    public static void Apply(ScottPlot.Plot plot)
    {
        ArgumentNullException.ThrowIfNull(plot);

        var text = GetColor("Wb.Color.Text.Muted", "#C5C5C5");
        var border = GetColor("Wb.Color.Border.Default", "#3F3F46");

        plot.FigureBackground.Color = GetColor("Wb.Color.Surface.Window", "#1E1E1E");
        plot.DataBackground.Color = GetColor("Wb.Color.Surface.Panel", "#252526");

        plot.Axes.Color(text);
        plot.Grid.MajorLineColor = border.WithOpacity(.5);
        plot.Grid.MinorLineColor = border.WithOpacity(.2);

        plot.Legend.BackgroundColor = GetColor("Wb.Color.Surface.Elevated", "#2D2D30");
        plot.Legend.FontColor = text;
        plot.Legend.OutlineColor = border;

        plot.Add.Palette = new WorkbenchPalette(GetSeriesPalette());

        // Select an appropriate fallback font for non-ASCII labels.
        plot.Font.Automatic();
    }

    /// <summary>Converts a WPF <see cref="System.Windows.Media.Color"/> to a <see cref="ScottPlot.Color"/>.</summary>
    public static ScottPlot.Color ToScottPlotColor(System.Windows.Media.Color color) =>
        new(color.R, color.G, color.B, color.A);

    private static bool IsLightBackground()
    {
        var background = GetColor("Wb.Color.Surface.Window", "#1E1E1E");
        var luminance = (0.299 * background.R + 0.587 * background.G + 0.114 * background.B) / 255.0;
        return luminance > 0.5;
    }

    private static ScottPlot.Color GetColor(string resourceKey, string fallbackHex) =>
        Application.Current?.TryFindResource(resourceKey) is System.Windows.Media.Color color
            ? ToScottPlotColor(color)
            : FromHex(fallbackHex);

    private static ScottPlot.Color FromHex(string hex) => ScottPlot.Color.FromHex(hex);

    private sealed class WorkbenchPalette(ScottPlot.Color[] colors) : ScottPlot.IPalette
    {
        public ScottPlot.Color[] Colors { get; } = colors;

        public string Name => "Workbench";

        public string Description => "WorkbenchUI.Wpf design token palette";

        public ScottPlot.Color GetColor(int index) => Colors[index % Colors.Length];
    }
}
