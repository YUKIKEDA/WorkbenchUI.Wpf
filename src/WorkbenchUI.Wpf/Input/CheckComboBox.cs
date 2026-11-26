using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Multi-select combo box control for filtering result components, load cases, layers, and similar sets.
/// <list type="bullet">
/// <item>Selection state is exposed via <see cref="SelectedItems"/> (<see cref="IList"/>). Passing an <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/> enables two-way synchronization.</item>
/// <item>When closed, displays the concatenated names of selected items by default; setting <see cref="SummaryFormat"/> (e.g., "{0} selected") switches to a count summary.</item>
/// <item>Setting <see cref="SelectAllContent"/> displays a tri-state "Select All" row.</item>
/// </list>
/// </summary>
[TemplatePart(Name = PartToggleButton, Type = typeof(ToggleButton))]
[TemplatePart(Name = PartPopup, Type = typeof(Popup))]
public class CheckComboBox : ItemsControl
{
    private const string PartToggleButton = "PART_ToggleButton";
    private const string PartPopup = "PART_Popup";

    /// <summary>Name of the "Select All" check box inside the control template.</summary>
    internal const string SelectAllPartName = "PART_SelectAllCheckBox";

    private ToggleButton? _toggleButton;
    private Popup? _popup;
    private int _popupClosedTick;
    private bool _updating;

    static CheckComboBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheckComboBox), new FrameworkPropertyMetadata(typeof(CheckComboBox)));
    }

    /// <summary>Initializes a new instance of the <see cref="CheckComboBox"/> class.</summary>
    public CheckComboBox()
    {
        // Item check boxes live inside the template, so handle their bubbling events centrally.
        // Use Checked/Unchecked rather than Click because UI Automation Toggle does not raise Click.
        AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler(OnItemToggled));
        AddHandler(ToggleButton.UncheckedEvent, new RoutedEventHandler(OnItemToggled));
    }

    #region Dependency properties

    /// <summary>Identifies the <see cref="SelectedItems"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
        nameof(SelectedItems), typeof(IList), typeof(CheckComboBox),
        new PropertyMetadata(null, OnSelectedItemsChanged));

    /// <summary>
    /// Gets or sets the collection of selected items. If it implements <see cref="INotifyCollectionChanged"/>
    /// (such as <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/>), external changes also update the display.
    /// </summary>
    public IList? SelectedItems
    {
        get => (IList?)GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
        nameof(IsDropDownOpen), typeof(bool), typeof(CheckComboBox),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Gets or sets whether the drop-down popup is open.</summary>
    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>Identifies the <see cref="MaxDropDownHeight"/> dependency property.</summary>
    public static readonly DependencyProperty MaxDropDownHeightProperty = DependencyProperty.Register(
        nameof(MaxDropDownHeight), typeof(double), typeof(CheckComboBox), new PropertyMetadata(300.0));

    /// <summary>Gets or sets the maximum height of the drop-down popup.</summary>
    public double MaxDropDownHeight
    {
        get => (double)GetValue(MaxDropDownHeightProperty);
        set => SetValue(MaxDropDownHeightProperty, value);
    }

    /// <summary>Identifies the <see cref="SummaryFormat"/> dependency property.</summary>
    public static readonly DependencyProperty SummaryFormatProperty = DependencyProperty.Register(
        nameof(SummaryFormat), typeof(string), typeof(CheckComboBox),
        new PropertyMetadata(null, OnSummaryAffectingChanged));

    /// <summary>
    /// Gets or sets the format string for count-based summary display (e.g., "{0} selected").
    /// When <see langword="null"/> (default), selected item display names are joined with commas.
    /// </summary>
    public string? SummaryFormat
    {
        get => (string?)GetValue(SummaryFormatProperty);
        set => SetValue(SummaryFormatProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectAllContent"/> dependency property.</summary>
    public static readonly DependencyProperty SelectAllContentProperty = DependencyProperty.Register(
        nameof(SelectAllContent), typeof(object), typeof(CheckComboBox), new PropertyMetadata(null));

    /// <summary>Gets or sets the label for the "Select All" row. When <see langword="null"/> (default), the row is hidden.</summary>
    public object? SelectAllContent
    {
        get => GetValue(SelectAllContentProperty);
        set => SetValue(SelectAllContentProperty, value);
    }

    /// <summary>Identifies the <see cref="PlaceholderText"/> dependency property.</summary>
    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(
        nameof(PlaceholderText), typeof(string), typeof(CheckComboBox), new PropertyMetadata(null));

    /// <summary>Gets or sets the muted placeholder text shown when no items are selected.</summary>
    public string? PlaceholderText
    {
        get => (string?)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    private static readonly DependencyPropertyKey SummaryTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(SummaryText), typeof(string), typeof(CheckComboBox), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="SummaryText"/> dependency property.</summary>
    public static readonly DependencyProperty SummaryTextProperty = SummaryTextPropertyKey.DependencyProperty;

    /// <summary>Gets the summary text displayed when the drop-down is closed (read-only).</summary>
    public string SummaryText => (string)GetValue(SummaryTextProperty);

    private static readonly DependencyPropertyKey SelectAllStatePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(SelectAllState), typeof(bool?), typeof(CheckComboBox), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="SelectAllState"/> dependency property.</summary>
    public static readonly DependencyProperty SelectAllStateProperty = SelectAllStatePropertyKey.DependencyProperty;

    /// <summary>Gets the tri-state value of the "Select All" check box (<see langword="true"/> = all, <see langword="false"/> = none, <see langword="null"/> = partial; read-only).</summary>
    public bool? SelectAllState => (bool?)GetValue(SelectAllStateProperty);

    #endregion

    // The default ItemsControl peer exposes only items, hiding the template's toggle button
    // and "Select All" row from UI Automation; use a generic FrameworkElementAutomationPeer instead.
    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new FrameworkElementAutomationPeer(this);

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() => new CheckComboBoxItem();

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) => item is CheckComboBoxItem;

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is CheckComboBoxItem container)
        {
            var updating = _updating;
            _updating = true;
            container.IsChecked = SelectedItems?.Contains(item) == true;
            _updating = updating;
        }
    }

    /// <inheritdoc />
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        UpdateSummary();
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_popup is not null)
        {
            _popup.Closed -= OnPopupClosed;
        }

        if (_toggleButton is not null)
        {
            _toggleButton.PreviewMouseLeftButtonDown -= OnToggleButtonPreviewMouseDown;
        }

        base.OnApplyTemplate();
        _popup = GetTemplateChild(PartPopup) as Popup;
        _toggleButton = GetTemplateChild(PartToggleButton) as ToggleButton;

        if (_popup is not null)
        {
            _popup.Closed += OnPopupClosed;
        }

        if (_toggleButton is not null)
        {
            _toggleButton.PreviewMouseLeftButtonDown += OnToggleButtonPreviewMouseDown;
        }

        UpdateSummary();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled)
        {
            return;
        }

        if (e.Key == Key.F4 ||
            (e.Key == Key.Down && e.KeyboardDevice.Modifiers == ModifierKeys.Alt))
        {
            SetCurrentValue(IsDropDownOpenProperty, !IsDropDownOpen);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && IsDropDownOpen)
        {
            SetCurrentValue(IsDropDownOpenProperty, false);
            e.Handled = true;
        }
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        _popupClosedTick = Environment.TickCount;
    }

    private void OnToggleButtonPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Clicking the toggle button while the popup is open closes it on capture loss,
        // which would otherwise immediately re-open it on the same click (standard WPF Popup pitfall).
        // Ignore clicks immediately after closing so clicking the toggle cleanly dismisses the popup.
        if (Environment.TickCount - _popupClosedTick < 200 && !IsDropDownOpen)
        {
            e.Handled = true;
        }
    }

    private void OnItemToggled(object sender, RoutedEventArgs e)
    {
        if (_updating || SelectedItems is not IList selected)
        {
            return;
        }

        // "Select All" row: clear if all items are selected; otherwise (none or partial) select all.
        // Decide based on the prior SelectAllState rather than the checkbox's new state
        // to avoid depending on tri-state toggle transitions.
        if (e.OriginalSource is FrameworkElement { Name: SelectAllPartName })
        {
            e.Handled = true;
            var selectAll = SelectAllState != true;
            _updating = true;
            try
            {
                selected.Clear();
                if (selectAll)
                {
                    foreach (var it in Items)
                    {
                        selected.Add(it);
                    }
                }

                foreach (var it in Items)
                {
                    if (ItemContainerGenerator.ContainerFromItem(it) is CheckComboBoxItem c)
                    {
                        c.IsChecked = selectAll;
                    }
                }
            }
            finally
            {
                _updating = false;
            }

            UpdateSummary();
            return;
        }

        if (e.OriginalSource is not CheckComboBoxItem container)
        {
            return;
        }

        var item = ItemContainerGenerator.ItemFromContainer(container);
        if (item is null || item == DependencyProperty.UnsetValue)
        {
            return;
        }

        if (container.IsChecked == true)
        {
            if (!selected.Contains(item))
            {
                selected.Add(item);
            }
        }
        else if (selected.Contains(item))
        {
            selected.Remove(item);
        }

        // Explicitly refresh in case SelectedItems does not implement INotifyCollectionChanged
        UpdateSummary();
    }

    private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var box = (CheckComboBox)d;
        if (e.OldValue is INotifyCollectionChanged oldIncc)
        {
            oldIncc.CollectionChanged -= box.OnSelectedItemsCollectionChanged;
        }

        if (e.NewValue is INotifyCollectionChanged newIncc)
        {
            newIncc.CollectionChanged += box.OnSelectedItemsCollectionChanged;
        }

        box.RefreshFromSelection();
    }

    private void OnSelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_updating)
        {
            RefreshFromSelection();
        }
        else
        {
            UpdateSummary();
        }
    }

    private static void OnSummaryAffectingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((CheckComboBox)d).UpdateSummary();

    /// <summary>Synchronizes realized containers and summary text from <see cref="SelectedItems"/>.</summary>
    private void RefreshFromSelection()
    {
        _updating = true;
        try
        {
            foreach (var item in Items)
            {
                if (ItemContainerGenerator.ContainerFromItem(item) is CheckComboBoxItem container)
                {
                    container.IsChecked = SelectedItems?.Contains(item) == true;
                }
            }
        }
        finally
        {
            _updating = false;
        }

        UpdateSummary();
    }

    private void UpdateSummary()
    {
        var selected = SelectedItems;
        var count = selected?.Count ?? 0;

        // Updating SelectAllState propagates via OneWay binding and re-fires Checked/Unchecked
        // on the "Select All" checkbox; guard against treating that as a user toggle.
        var wasUpdating = _updating;
        _updating = true;
        try
        {
            SetValue(SelectAllStatePropertyKey,
                count == 0 ? false
                : Items.Count > 0 && count >= Items.Count ? true
                : (bool?)null);
        }
        finally
        {
            _updating = wasUpdating;
        }

        string summary;
        if (count == 0)
        {
            summary = string.Empty;
        }
        else if (SummaryFormat is { Length: > 0 } format)
        {
            summary = string.Format(CultureInfo.CurrentCulture, format, count);
        }
        else
        {
            // Join display names in Items order for deterministic output independent of selection order
            summary = string.Join(", ", Items.Cast<object>()
                .Where(i => selected!.Contains(i))
                .Select(GetItemText));
        }

        SetValue(SummaryTextPropertyKey, summary);
    }

    private string GetItemText(object? item)
    {
        if (item is null)
        {
            return string.Empty;
        }

        if (DisplayMemberPath is { Length: > 0 } path)
        {
            object? current = item;
            foreach (var part in path.Split('.'))
            {
                current = current?.GetType().GetProperty(part)?.GetValue(current);
            }

            return current?.ToString() ?? string.Empty;
        }

        return item.ToString() ?? string.Empty;
    }
}

/// <summary>
/// Item container for <see cref="CheckComboBox"/>, rendered as a full-row clickable check box.
/// </summary>
public class CheckComboBoxItem : CheckBox
{
    static CheckComboBoxItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheckComboBoxItem), new FrameworkPropertyMetadata(typeof(CheckComboBoxItem)));
    }
}
