using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Converts a <see cref="ColorMap"/> to a horizontal <see cref="LinearGradientBrush"/>
/// (for example, for color map selection previews in <see cref="ColorScaleEditor"/>).
/// </summary>
public sealed class ColorMapToBrushConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ColorMap map)
        {
            return null;
        }

        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 0),
        };
        foreach (var (offset, color) in map.Stops)
        {
            brush.GradientStops.Add(new GradientStop(color, offset));
        }

        brush.Freeze();
        return brush;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
