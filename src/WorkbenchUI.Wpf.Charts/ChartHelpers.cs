using System.Windows;

namespace WorkbenchUI.Wpf.Charts;

/// <summary>Shared internal helpers for composite engineering chart controls.</summary>
internal static class ChartHelpers
{
    /// <summary>
    /// Creates a logarithmic tick generator for log10-transformed values, formatting tick labels as powers of 10.
    /// </summary>
    public static ScottPlot.TickGenerators.NumericAutomatic CreateLogTickGenerator() => new()
    {
        MinorTickGenerator = new ScottPlot.TickGenerators.LogMinorTickGenerator(),
        IntegerTicksOnly = true,
        LabelFormatter = static y => FormatPowerOfTen(y),
    };

    /// <summary>Formats a log10 exponent value as <c>1E-6</c> or <c>100</c>.</summary>
    public static string FormatPowerOfTen(double log10Value)
    {
        var exponent = (int)Math.Round(log10Value);
        return exponent is >= -3 and <= 4
            ? Math.Pow(10, exponent).ToString("0.###")
            : $"1E{exponent}";
    }

    /// <summary>Computes log10 safely while clamping zero or negative residuals to a positive floor.</summary>
    public static double SafeLog10(double value) =>
        Math.Log10(Math.Max(value, 1e-300));

    /// <summary>Resolves a design token <see cref="System.Windows.Media.Color"/> resource or falls back to <paramref name="fallbackHex"/>.</summary>
    public static ScottPlot.Color GetTokenColor(string resourceKey, string fallbackHex) =>
        Application.Current?.TryFindResource(resourceKey) is System.Windows.Media.Color color
            ? WorkbenchChartTheme.ToScottPlotColor(color)
            : ScottPlot.Color.FromHex(fallbackHex);
}
