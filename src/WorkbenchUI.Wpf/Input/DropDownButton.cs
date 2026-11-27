using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Button that opens a drop-down menu when clicked.
/// Specify a <see cref="ContextMenu"/> in <see cref="DropDownMenu"/> so standard menu styling,
/// keyboard navigation, and UI Automation work out of the box.
/// </summary>
public class DropDownButton : Button
{
    private const string PartDropDownButton = "PART_DropDownButton";

    private ButtonBase? _dropDownButton;

    static DropDownButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(DropDownButton), new FrameworkPropertyMetadata(typeof(DropDownButton)));
    }

    /// <summary>Identifies the <see cref="DropDownMenu"/> dependency property.</summary>
    public static readonly DependencyProperty DropDownMenuProperty = DependencyProperty.Register(
        nameof(DropDownMenu), typeof(ContextMenu), typeof(DropDownButton),
        new PropertyMetadata(null, OnDropDownMenuChanged));

    /// <summary>Gets or sets the menu opened when the button is clicked.</summary>
    public ContextMenu? DropDownMenu
    {
        get => (ContextMenu?)GetValue(DropDownMenuProperty);
        set => SetValue(DropDownMenuProperty, value);
    }

    private static readonly DependencyPropertyKey IsDropDownOpenPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsDropDownOpen), typeof(bool), typeof(DropDownButton), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsDropDownOpenProperty =
        IsDropDownOpenPropertyKey.DependencyProperty;

    /// <summary>Gets whether the drop-down menu is currently open (read-only, for template visual states).</summary>
    public bool IsDropDownOpen => (bool)GetValue(IsDropDownOpenProperty);

    /// <summary>Gets whether clicking the main button body opens the menu. <see cref="SplitButton"/> returns <see langword="false"/> so only the arrow opens the menu.</summary>
    protected virtual bool OpensOnClick => true;

    private static void OnDropDownMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var button = (DropDownButton)d;

        if (e.OldValue is ContextMenu oldMenu)
        {
            oldMenu.Opened -= button.OnMenuOpened;
            oldMenu.Closed -= button.OnMenuClosed;
        }

        if (e.NewValue is ContextMenu newMenu)
        {
            newMenu.Opened += button.OnMenuOpened;
            newMenu.Closed += button.OnMenuClosed;
        }

        button.SetValue(IsDropDownOpenPropertyKey, (e.NewValue as ContextMenu)?.IsOpen == true);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_dropDownButton is not null)
        {
            _dropDownButton.Click -= OnDropDownPartClick;
        }

        base.OnApplyTemplate();

        // Arrow button part in SplitButton's template (not present in DropDownButton's template).
        _dropDownButton = GetTemplateChild(PartDropDownButton) as ButtonBase;

        if (_dropDownButton is not null)
        {
            _dropDownButton.Click += OnDropDownPartClick;
        }
    }

    /// <inheritdoc />
    protected override void OnClick()
    {
        base.OnClick();
        if (OpensOnClick)
        {
            OpenDropDown();
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Support standard ComboBox gestures for opening the drop-down menu
        if (!e.Handled && (e.Key == Key.F4 ||
            (e.Key == Key.Down && e.KeyboardDevice.Modifiers == ModifierKeys.Alt)))
        {
            OpenDropDown();
            e.Handled = true;
        }
    }

    private void OnDropDownPartClick(object sender, RoutedEventArgs e)
    {
        // Clicking the arrow part only opens the menu; mark handled so SplitButton.Click does not bubble.
        e.Handled = true;
        OpenDropDown();
    }

    /// <summary>Opens the drop-down menu directly below this button.</summary>
    protected void OpenDropDown()
    {
        if (DropDownMenu is not { } menu || menu.IsOpen)
        {
            return;
        }

        menu.PlacementTarget = this;
        menu.Placement = PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private void OnMenuOpened(object sender, RoutedEventArgs e) =>
        SetValue(IsDropDownOpenPropertyKey, true);

    private void OnMenuClosed(object sender, RoutedEventArgs e) =>
        SetValue(IsDropDownOpenPropertyKey, false);
}
