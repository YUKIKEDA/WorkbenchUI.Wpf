using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Control that renders a line icon from <see cref="WorkbenchIcons"/>.
/// Scales a <see cref="Geometry"/> authored on a 16×16 canvas inside a <see cref="Viewbox"/> so stroke proportions
/// remain consistent at any size, while inheriting color from <see cref="Control.Foreground"/>.
/// </summary>
public class WorkbenchIcon : Control
{
    static WorkbenchIcon()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(WorkbenchIcon), new FrameworkPropertyMetadata(typeof(WorkbenchIcon)));
        FocusableProperty.OverrideMetadata(
            typeof(WorkbenchIcon), new FrameworkPropertyMetadata(false));
        IsTabStopProperty.OverrideMetadata(
            typeof(WorkbenchIcon), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(Geometry), typeof(WorkbenchIcon),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Gets or sets the icon <see cref="Geometry"/> to display (typically a member of <see cref="WorkbenchIcons"/>).</summary>
    public Geometry? Icon
    {
        get => (Geometry?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(double), typeof(WorkbenchIcon),
        new FrameworkPropertyMetadata(16.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Gets or sets the display size (side length of the square, in DIPs). Defaults to 16.</summary>
    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Identifies the <see cref="StrokeThickness"/> dependency property.</summary>
    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness), typeof(double), typeof(WorkbenchIcon),
        new FrameworkPropertyMetadata(1.2, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Gets or sets the stroke thickness relative to the 16×16 canvas (scales proportionally with <see cref="Size"/>). Defaults to 1.2.</summary>
    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }
}
