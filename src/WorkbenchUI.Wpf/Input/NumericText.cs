using System.Globalization;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Parses numeric input strings with optional scientific notation and trailing unit symbols.
/// </summary>
internal static class NumericText
{
    /// <summary>Non-letter characters allowed as part of a unit symbol.</summary>
    private const string ExtraUnitChars = "%°²³/";

    /// <summary>
    /// Parses a string as a number with an optional trailing unit symbol.
    /// </summary>
    /// <param name="text">The input string.</param>
    /// <param name="culture">The format provider for numeric parsing.</param>
    /// <param name="number">When this method returns, contains the parsed number.</param>
    /// <param name="unitSymbol">When this method returns, contains the trailing unit symbol, or <see langword="null"/> if none.</param>
    /// <returns><see langword="true"/> if the numeric portion parsed successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string text, IFormatProvider culture, out double number, out string? unitSymbol)
    {
        number = 0;
        unitSymbol = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();

        // Scan backward from the end to extract trailing unit characters (letters, %, °, etc.).
        // Scanning from the end avoids mistaking 'e' in "1e-3" for a unit because digits follow it.
        var unitStart = trimmed.Length;
        while (unitStart > 0 && IsUnitChar(trimmed[unitStart - 1]))
        {
            unitStart--;
        }

        var numericPart = trimmed[..unitStart].Trim();
        var unitPart = trimmed[unitStart..];

        if (numericPart.Length == 0)
        {
            return false;
        }

        if (!double.TryParse(numericPart, NumberStyles.Float, culture, out number))
        {
            return false;
        }

        unitSymbol = unitPart.Length > 0 ? unitPart : null;
        return true;
    }

    private static bool IsUnitChar(char c) => char.IsLetter(c) || ExtraUnitChars.Contains(c);
}
