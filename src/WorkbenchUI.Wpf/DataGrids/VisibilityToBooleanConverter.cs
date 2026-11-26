using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Two-way converter between <see cref="Visibility"/> and <see cref="bool"/>.
/// Intended for binding <c>DataGridRow.DetailsVisibility</c> to <c>ToggleButton.IsChecked</c> (such as <c>Wb.DataGrid.DetailsToggle</c>),
/// where the standard <see cref="System.Windows.Controls.BooleanToVisibilityConverter"/> converts in the opposite direction.
/// </summary>
public sealed class VisibilityToBooleanConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility.Visible;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;
}
