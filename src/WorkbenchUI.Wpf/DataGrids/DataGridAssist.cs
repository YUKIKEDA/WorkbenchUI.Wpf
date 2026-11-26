using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Attached behavior that automatically applies themed display and editing styles to <see cref="DataGrid"/> columns.
/// </summary>
/// <remarks>
/// Because <see cref="DataGridTextColumn"/> and <see cref="DataGridComboBoxColumn"/> default to WPF's built-in static
/// Default(Editing)ElementStyle, implicit theme styles do not reach the generated elements.
/// This behavior replaces styles only on columns still using the default style, leaving explicitly styled columns untouched.
/// </remarks>
public static class DataGridAssist
{
    /// <summary>Resource key for the themed editing <see cref="TextBox"/> style used in <see cref="DataGrid"/> cells.</summary>
    public const string EditingTextBoxStyleKey = "Wb.DataGrid.EditingTextBox";

    /// <summary>Resource key for the themed display <see cref="TextBlock"/> style used in <see cref="DataGrid"/> cells.</summary>
    public const string ElementTextBlockStyleKey = "Wb.DataGrid.ElementTextBlock";

    /// <summary>Identifies the <c>AutoApplyColumnStyles</c> attached dependency property.</summary>
    public static readonly DependencyProperty AutoApplyColumnStylesProperty =
        DependencyProperty.RegisterAttached(
            "AutoApplyColumnStyles",
            typeof(bool),
            typeof(DataGridAssist),
            new PropertyMetadata(false, OnAutoApplyColumnStylesChanged));

    /// <summary>Gets the value of the <c>AutoApplyColumnStyles</c> attached property for the specified <see cref="DataGrid"/>.</summary>
    public static bool GetAutoApplyColumnStyles(DataGrid grid)
        => (bool)grid.GetValue(AutoApplyColumnStylesProperty);

    /// <summary>Sets the value of the <c>AutoApplyColumnStyles</c> attached property for the specified <see cref="DataGrid"/>.</summary>
    public static void SetAutoApplyColumnStyles(DataGrid grid, bool value)
        => grid.SetValue(AutoApplyColumnStylesProperty, value);

    private static readonly DependencyProperty IsHookedProperty =
        DependencyProperty.RegisterAttached(
            "IsHooked", typeof(bool), typeof(DataGridAssist), new PropertyMetadata(false));

    private static Style? _fallbackEditingTextBoxStyle;
    private static Style? _fallbackElementTextBlockStyle;

    private static void OnAutoApplyColumnStylesChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid || !(bool)e.NewValue)
        {
            return;
        }

        if (!(bool)grid.GetValue(IsHookedProperty))
        {
            grid.SetValue(IsHookedProperty, true);
            grid.Columns.CollectionChanged += (_, args) =>
            {
                if (args.NewItems is null || !GetAutoApplyColumnStyles(grid))
                {
                    return;
                }

                foreach (DataGridColumn column in args.NewItems)
                {
                    ApplyColumnStyle(grid, column);
                }
            };

            grid.Initialized += (_, _) =>
            {
                if (GetAutoApplyColumnStyles(grid))
                {
                    ApplyAll(grid);
                }
            };
            grid.Loaded += (_, _) =>
            {
                if (GetAutoApplyColumnStyles(grid))
                {
                    ApplyAll(grid);
                }
            };
        }

        ApplyAll(grid);
    }

    private static void ApplyAll(DataGrid grid)
    {
        foreach (var column in grid.Columns)
        {
            ApplyColumnStyle(grid, column);
        }
    }

    private static void ApplyColumnStyle(DataGrid grid, DataGridColumn column)
    {
        switch (column)
        {
            case DataGridTextColumn text:
                if (ReferenceEquals(text.EditingElementStyle, DataGridTextColumn.DefaultEditingElementStyle))
                {
                    text.EditingElementStyle = FindEditingTextBoxStyle(grid);
                }

                if (ReferenceEquals(text.ElementStyle, DataGridTextColumn.DefaultElementStyle))
                {
                    text.ElementStyle = FindElementTextBlockStyle(grid);
                }
                break;

            case DataGridComboBoxColumn combo
                when ReferenceEquals(combo.EditingElementStyle, DataGridComboBoxColumn.DefaultEditingElementStyle):
                if (FindResource(grid, typeof(ComboBox)) is Style comboStyle)
                {
                    combo.EditingElementStyle = comboStyle;
                }
                break;
        }
    }

    private static Style FindEditingTextBoxStyle(DataGrid grid)
        => FindResource(grid, EditingTextBoxStyleKey) as Style
           ?? GetFallbackEditingTextBoxStyle();

    private static Style FindElementTextBlockStyle(DataGrid grid)
        => FindResource(grid, ElementTextBlockStyleKey) as Style
           ?? GetFallbackElementTextBlockStyle();

    private static object? FindResource(FrameworkElement element, object key)
    {
        if (Application.Current?.TryFindResource(key) is { } appResource)
        {
            return appResource;
        }

        return element.TryFindResource(key);
    }

    private static ControlTemplate CreateEmptyErrorTemplate()
    {
        var factory = new FrameworkElementFactory(typeof(AdornedElementPlaceholder));
        return new ControlTemplate { VisualTree = factory };
    }

    private static Style GetFallbackEditingTextBoxStyle()
    {
        if (_fallbackEditingTextBoxStyle is not null)
        {
            return _fallbackEditingTextBoxStyle;
        }

        var style = new Style(typeof(TextBox));
        style.Setters.Add(new Setter(Control.BackgroundProperty, System.Windows.Media.Brushes.Transparent));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, System.Windows.Media.Brushes.Transparent));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(5, 0, 5, 0)));
        style.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        style.Setters.Add(new Setter(Validation.ErrorTemplateProperty, CreateEmptyErrorTemplate()));

        var errorTrigger = new Trigger { Property = Validation.HasErrorProperty, Value = true };
        errorTrigger.Setters.Add(new Setter(
            Control.BorderBrushProperty,
            new DynamicResourceExtension("Wb.Brush.Error")));
        errorTrigger.Setters.Add(new Setter(
            FrameworkElement.ToolTipProperty,
            new Binding("(Validation.Errors)[0].ErrorContent") { RelativeSource = RelativeSource.Self }));
        style.Triggers.Add(errorTrigger);

        style.Seal();
        _fallbackEditingTextBoxStyle = style;
        return style;
    }

    private static Style GetFallbackElementTextBlockStyle()
    {
        if (_fallbackElementTextBlockStyle is not null)
        {
            return _fallbackElementTextBlockStyle;
        }

        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));
        style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(6, 0, 6, 0)));
        style.Setters.Add(new Setter(Validation.ErrorTemplateProperty, CreateEmptyErrorTemplate()));

        var errorTrigger = new Trigger { Property = Validation.HasErrorProperty, Value = true };
        errorTrigger.Setters.Add(new Setter(
            FrameworkElement.ToolTipProperty,
            new Binding("(Validation.Errors)[0].ErrorContent") { RelativeSource = RelativeSource.Self }));
        style.Triggers.Add(errorTrigger);

        style.Seal();
        _fallbackElementTextBlockStyle = style;
        return style;
    }
}
