namespace WorkbenchUI.Wpf.Theming;

/// <summary>
/// Available built-in theme variants for <see cref="WorkbenchTheme"/>.
/// Each member name corresponds to a <c>Tokens.{Variant}.xaml</c> resource dictionary.
/// </summary>
public enum WorkbenchThemeVariant
{
    /// <summary>Dark theme (default, neutral dark gray surfaces).</summary>
    Dark,

    /// <summary>Light theme (neutral light surfaces).</summary>
    Light,
}
