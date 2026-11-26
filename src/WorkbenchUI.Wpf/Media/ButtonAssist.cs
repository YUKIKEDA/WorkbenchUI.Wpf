using System.Windows;
using System.Windows.Media;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Attached properties for adding vector icons to button controls.
/// Setting <c>ButtonAssist.Icon</c> to a <see cref="Geometry"/> displays the icon to the left of the content in themed button templates.
/// </summary>
public static class ButtonAssist
{
    /// <summary>Identifies the <c>Icon</c> attached dependency property.</summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.RegisterAttached(
        "Icon", typeof(Geometry), typeof(ButtonAssist),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Gets the icon <see cref="Geometry"/> attached to <paramref name="element"/>.</summary>
    public static Geometry? GetIcon(DependencyObject element) =>
        (Geometry?)element.GetValue(IconProperty);

    /// <summary>Sets the icon <see cref="Geometry"/> attached to <paramref name="element"/>.</summary>
    public static void SetIcon(DependencyObject element, Geometry? value) =>
        element.SetValue(IconProperty, value);

    /// <summary>Identifies the <c>IconSize</c> attached dependency property.</summary>
    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.RegisterAttached(
        "IconSize", typeof(double), typeof(ButtonAssist),
        new FrameworkPropertyMetadata(16.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Gets the icon display size in DIPs (defaults to 16; large ribbon buttons use 24).</summary>
    public static double GetIconSize(DependencyObject element) =>
        (double)element.GetValue(IconSizeProperty);

    /// <summary>Sets the icon display size in DIPs.</summary>
    public static void SetIconSize(DependencyObject element, double value) =>
        element.SetValue(IconSizeProperty, value);
}
