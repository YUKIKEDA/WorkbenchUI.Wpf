namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Extension point for converting between display units and internal base units in <see cref="NumericBox"/>.
/// The control library remains unit-system agnostic and delegates conversions to the application.
/// Example: internal values in SI meters (m) and display in millimeters (mm), so entering "500 mm" yields 0.5.
/// </summary>
public interface IUnitProvider
{
    /// <summary>Gets the display unit symbol (e.g., "mm") shown in the <see cref="NumericBox"/> unit label.</summary>
    string DisplayUnit { get; }

    /// <summary>Converts an internal base-unit value to a display-unit value.</summary>
    double ToDisplay(double baseValue);

    /// <summary>Converts a display-unit value to an internal base-unit value.</summary>
    double FromDisplay(double displayValue);

    /// <summary>
    /// Converts a value entered with an explicit unit symbol (such as 500 and "mm" from "500 mm") to a base-unit value.
    /// Returns <see langword="false"/> if the unit symbol is unsupported (treated as a validation error by <see cref="NumericBox"/>).
    /// </summary>
    bool TryConvertFrom(double value, string unitSymbol, out double baseValue);
}
