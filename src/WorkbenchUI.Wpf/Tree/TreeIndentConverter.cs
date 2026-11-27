using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>Converts a node's nesting level (<see cref="int"/>) into a left-margin <see cref="Thickness"/>.</summary>
public sealed class TreeIndentConverter : IValueConverter
{
    /// <summary>Gets or sets the indentation width per nesting level, in pixels.</summary>
    public double IndentSize { get; set; } = 16.0;

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int level ? new Thickness(level * IndentSize, 0, 0, 0) : new Thickness(0);

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
