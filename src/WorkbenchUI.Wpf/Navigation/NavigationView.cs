using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Shell navigation control featuring a collapsible left navigation pane (switching smoothly
/// between an expanded hierarchy sidebar and a compact icon-only rail with child flyouts),
/// optional footer items, pane header/footer slots, and a main content host.
/// </summary>
[TemplatePart(Name = PartPaneToggleButton, Type = typeof(ButtonBase))]
public class NavigationView : ContentControl
{
    private const string PartPaneToggleButton = "PART_PaneToggleButton";
    private const double MinCompactPaneLength = 32.0;

    private readonly HashSet<NavigationViewItem> _hookedItems = [];
    private readonly HashSet<INotifyCollectionChanged> _hookedCollections = [];
    private ButtonBase? _paneToggleButton;
    private bool _wasBelowAutoCollapseWidth;

    static NavigationView()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NavigationView),
            new FrameworkPropertyMetadata(typeof(NavigationView)));
    }

    /// <summary>Initializes a new instance of the <see cref="NavigationView"/> class.</summary>
    public NavigationView()
    {
        var menuItems = new ObservableCollection<object>();
        var footerItems = new ObservableCollection<object>();

        SetValue(MenuItemsPropertyKey, menuItems);
        SetValue(FooterMenuItemsPropertyKey, footerItems);
        SetValue(EffectiveMenuItemsPropertyKey, menuItems);
        SetValue(EffectiveFooterMenuItemsPropertyKey, footerItems);
        SetValue(PaneWidthPropertyKey, OpenPaneLength);

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    #region Dependency properties & routed events

    private static readonly DependencyPropertyKey MenuItemsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(MenuItems), typeof(ObservableCollection<object>), typeof(NavigationView),
        new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="MenuItems"/> dependency property.</summary>
    public static readonly DependencyProperty MenuItemsProperty = MenuItemsPropertyKey.DependencyProperty;

    /// <summary>Gets the collection of primary navigation items (read-only collection property).</summary>
    public ObservableCollection<object> MenuItems => (ObservableCollection<object>)GetValue(MenuItemsProperty);

    /// <summary>Identifies the <see cref="MenuItemsSource"/> dependency property.</summary>
    public static readonly DependencyProperty MenuItemsSourceProperty = DependencyProperty.Register(
        nameof(MenuItemsSource), typeof(IEnumerable), typeof(NavigationView),
        new PropertyMetadata(null, OnMenuItemsSourceChanged));

    /// <summary>Gets or sets a collection source for primary navigation items (overrides <see cref="MenuItems"/> when non-null).</summary>
    public IEnumerable? MenuItemsSource
    {
        get => (IEnumerable?)GetValue(MenuItemsSourceProperty);
        set => SetValue(MenuItemsSourceProperty, value);
    }

    private static readonly DependencyPropertyKey FooterMenuItemsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(FooterMenuItems), typeof(ObservableCollection<object>), typeof(NavigationView),
        new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="FooterMenuItems"/> dependency property.</summary>
    public static readonly DependencyProperty FooterMenuItemsProperty = FooterMenuItemsPropertyKey.DependencyProperty;

    /// <summary>Gets the collection of footer navigation items anchored to the bottom of the pane (read-only collection property).</summary>
    public ObservableCollection<object> FooterMenuItems => (ObservableCollection<object>)GetValue(FooterMenuItemsProperty);

    /// <summary>Identifies the <see cref="FooterMenuItemsSource"/> dependency property.</summary>
    public static readonly DependencyProperty FooterMenuItemsSourceProperty = DependencyProperty.Register(
        nameof(FooterMenuItemsSource), typeof(IEnumerable), typeof(NavigationView),
        new PropertyMetadata(null, OnFooterMenuItemsSourceChanged));

    /// <summary>Gets or sets a collection source for footer navigation items (overrides <see cref="FooterMenuItems"/> when non-null).</summary>
    public IEnumerable? FooterMenuItemsSource
    {
        get => (IEnumerable?)GetValue(FooterMenuItemsSourceProperty);
        set => SetValue(FooterMenuItemsSourceProperty, value);
    }

    private static readonly DependencyPropertyKey EffectiveMenuItemsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(EffectiveMenuItems), typeof(IEnumerable), typeof(NavigationView), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="EffectiveMenuItems"/> dependency property.</summary>
    public static readonly DependencyProperty EffectiveMenuItemsProperty = EffectiveMenuItemsPropertyKey.DependencyProperty;

    /// <summary>Gets the active primary navigation items collection (<see cref="MenuItemsSource"/> or <see cref="MenuItems"/>; read-only).</summary>
    public IEnumerable EffectiveMenuItems => (IEnumerable)GetValue(EffectiveMenuItemsProperty);

    private static readonly DependencyPropertyKey EffectiveFooterMenuItemsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(EffectiveFooterMenuItems), typeof(IEnumerable), typeof(NavigationView), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="EffectiveFooterMenuItems"/> dependency property.</summary>
    public static readonly DependencyProperty EffectiveFooterMenuItemsProperty = EffectiveFooterMenuItemsPropertyKey.DependencyProperty;

    /// <summary>Gets the active footer navigation items collection (<see cref="FooterMenuItemsSource"/> or <see cref="FooterMenuItems"/>; read-only).</summary>
    public IEnumerable EffectiveFooterMenuItems => (IEnumerable)GetValue(EffectiveFooterMenuItemsProperty);

    private static readonly DependencyPropertyKey HasFooterMenuItemsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasFooterMenuItems), typeof(bool), typeof(NavigationView), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="HasFooterMenuItems"/> dependency property.</summary>
    public static readonly DependencyProperty HasFooterMenuItemsProperty = HasFooterMenuItemsPropertyKey.DependencyProperty;

    /// <summary>Gets a value indicating whether any footer navigation items are present (read-only).</summary>
    public bool HasFooterMenuItems => (bool)GetValue(HasFooterMenuItemsProperty);

    /// <summary>Identifies the <see cref="SelectedItem"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
        nameof(SelectedItem), typeof(object), typeof(NavigationView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

    /// <summary>Gets or sets the currently selected navigation item.</summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>Identifies the <see cref="IsPaneOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsPaneOpenProperty = DependencyProperty.Register(
        nameof(IsPaneOpen), typeof(bool), typeof(NavigationView),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsPaneOpenChanged));

    /// <summary>Gets or sets a value indicating whether the navigation pane is expanded (true) or collapsed to compact rail mode (false).</summary>
    public bool IsPaneOpen
    {
        get => (bool)GetValue(IsPaneOpenProperty);
        set => SetValue(IsPaneOpenProperty, value);
    }

    /// <summary>Identifies the <see cref="PaneDisplayMode"/> dependency property.</summary>
    public static readonly DependencyProperty PaneDisplayModeProperty = DependencyProperty.Register(
        nameof(PaneDisplayMode), typeof(NavigationViewPaneDisplayMode), typeof(NavigationView),
        new PropertyMetadata(NavigationViewPaneDisplayMode.Auto, OnPaneDisplayModeChanged));

    /// <summary>Gets or sets how the navigation pane adapts to window width and user toggling.</summary>
    public NavigationViewPaneDisplayMode PaneDisplayMode
    {
        get => (NavigationViewPaneDisplayMode)GetValue(PaneDisplayModeProperty);
        set => SetValue(PaneDisplayModeProperty, value);
    }

    /// <summary>Identifies the <see cref="OpenPaneLength"/> dependency property.</summary>
    public static readonly DependencyProperty OpenPaneLengthProperty = DependencyProperty.Register(
        nameof(OpenPaneLength), typeof(double), typeof(NavigationView),
        new PropertyMetadata(240.0, OnPaneLengthChanged, CoerceOpenPaneLength));

    /// <summary>Gets or sets the width of the expanded navigation pane in DIPs (default 240).</summary>
    public double OpenPaneLength
    {
        get => (double)GetValue(OpenPaneLengthProperty);
        set => SetValue(OpenPaneLengthProperty, value);
    }

    /// <summary>Identifies the <see cref="CompactPaneLength"/> dependency property.</summary>
    public static readonly DependencyProperty CompactPaneLengthProperty = DependencyProperty.Register(
        nameof(CompactPaneLength), typeof(double), typeof(NavigationView),
        new PropertyMetadata(48.0, OnPaneLengthChanged, CoerceCompactPaneLength));

    /// <summary>Gets or sets the width of the compact icon rail in DIPs (default 48, minimum 32).</summary>
    public double CompactPaneLength
    {
        get => (double)GetValue(CompactPaneLengthProperty);
        set => SetValue(CompactPaneLengthProperty, value);
    }

    /// <summary>Identifies the <see cref="AutoCollapseWidth"/> dependency property.</summary>
    public static readonly DependencyProperty AutoCollapseWidthProperty = DependencyProperty.Register(
        nameof(AutoCollapseWidth), typeof(double), typeof(NavigationView),
        new PropertyMetadata(860.0, OnAutoCollapseWidthChanged));

    /// <summary>Gets or sets the control width threshold below which <see cref="NavigationViewPaneDisplayMode.Auto"/> collapses the pane (default 860).</summary>
    public double AutoCollapseWidth
    {
        get => (double)GetValue(AutoCollapseWidthProperty);
        set => SetValue(AutoCollapseWidthProperty, value);
    }

    /// <summary>Identifies the <see cref="IsPaneToggleButtonVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsPaneToggleButtonVisibleProperty = DependencyProperty.Register(
        nameof(IsPaneToggleButtonVisible), typeof(bool), typeof(NavigationView), new PropertyMetadata(true));

    /// <summary>Gets or sets a value indicating whether the pane collapse/expand toggle button is visible.</summary>
    public bool IsPaneToggleButtonVisible
    {
        get => (bool)GetValue(IsPaneToggleButtonVisibleProperty);
        set => SetValue(IsPaneToggleButtonVisibleProperty, value);
    }

    /// <summary>Identifies the <see cref="PaneToggleToolTip"/> dependency property.</summary>
    public static readonly DependencyProperty PaneToggleToolTipProperty = DependencyProperty.Register(
        nameof(PaneToggleToolTip), typeof(string), typeof(NavigationView), new PropertyMetadata("Toggle navigation"));

    /// <summary>Gets or sets the tooltip shown on the pane toggle button.</summary>
    public string PaneToggleToolTip
    {
        get => (string)GetValue(PaneToggleToolTipProperty);
        set => SetValue(PaneToggleToolTipProperty, value);
    }

    /// <summary>Identifies the <see cref="PaneHeader"/> dependency property.</summary>
    public static readonly DependencyProperty PaneHeaderProperty = DependencyProperty.Register(
        nameof(PaneHeader), typeof(object), typeof(NavigationView), new PropertyMetadata(null));

    /// <summary>Gets or sets optional header content displayed next to the toggle button or below it in the expanded pane.</summary>
    public object? PaneHeader
    {
        get => GetValue(PaneHeaderProperty);
        set => SetValue(PaneHeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="PaneFooter"/> dependency property.</summary>
    public static readonly DependencyProperty PaneFooterProperty = DependencyProperty.Register(
        nameof(PaneFooter), typeof(object), typeof(NavigationView), new PropertyMetadata(null));

    /// <summary>Gets or sets optional custom footer content placed at the bottom of the navigation pane.</summary>
    public object? PaneFooter
    {
        get => GetValue(PaneFooterProperty);
        set => SetValue(PaneFooterProperty, value);
    }

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(object), typeof(NavigationView), new PropertyMetadata(null));

    /// <summary>Gets or sets optional header content displayed above the main page content.</summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="HeaderTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register(
        nameof(HeaderTemplate), typeof(DataTemplate), typeof(NavigationView), new PropertyMetadata(null));

    /// <summary>Gets or sets the data template used to render <see cref="Header"/>.</summary>
    public DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    private static readonly DependencyPropertyKey PaneWidthPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(PaneWidth), typeof(double), typeof(NavigationView), new PropertyMetadata(240.0));

    /// <summary>Identifies the <see cref="PaneWidth"/> dependency property.</summary>
    public static readonly DependencyProperty PaneWidthProperty = PaneWidthPropertyKey.DependencyProperty;

    /// <summary>Gets the current rendered width of the navigation pane (read-only).</summary>
    public double PaneWidth => (double)GetValue(PaneWidthProperty);

    private static readonly DependencyPropertyKey IsCompactPanePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsCompactPane), typeof(bool), typeof(NavigationView), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsCompactPane"/> dependency property.</summary>
    public static readonly DependencyProperty IsCompactPaneProperty = IsCompactPanePropertyKey.DependencyProperty;

    /// <summary>Gets a value indicating whether the pane is currently in compact icon-rail state (read-only).</summary>
    public bool IsCompactPane => (bool)GetValue(IsCompactPaneProperty);

    /// <summary>Identifies the <see cref="SelectionChanged"/> routed event.</summary>
    public static readonly RoutedEvent SelectionChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(SelectionChanged), RoutingStrategy.Bubble,
        typeof(NavigationViewSelectionChangedEventHandler), typeof(NavigationView));

    /// <summary>Occurs when the selected navigation item changes.</summary>
    public event NavigationViewSelectionChangedEventHandler SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    #endregion

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_paneToggleButton is not null)
        {
            _paneToggleButton.Click -= OnPaneToggleClick;
        }

        base.OnApplyTemplate();

        _paneToggleButton = GetTemplateChild(PartPaneToggleButton) as ButtonBase;
        if (_paneToggleButton is not null)
        {
            _paneToggleButton.Click += OnPaneToggleClick;
        }

        UpdatePaneMetrics();
        RefreshAll();
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new FrameworkElementAutomationPeer(this);

    /// <inheritdoc />
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        EvaluateAutoCollapse(sizeInfo.NewSize.Width);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ActualWidth > 0)
        {
            EvaluateAutoCollapse(ActualWidth, forceOnInitial: true);
        }

        RefreshAll();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => DetachAllHandlers();

    private void OnPaneToggleClick(object sender, RoutedEventArgs e) =>
        SetCurrentValue(IsPaneOpenProperty, !IsPaneOpen);

    private static object CoerceCompactPaneLength(DependencyObject d, object baseValue) =>
        Math.Max(MinCompactPaneLength, (double)baseValue);

    private static object CoerceOpenPaneLength(DependencyObject d, object baseValue)
    {
        var view = (NavigationView)d;
        return Math.Max(view.CompactPaneLength, (double)baseValue);
    }

    private static void OnPaneLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (NavigationView)d;
        view.CoerceValue(OpenPaneLengthProperty);
        view.UpdatePaneMetrics();
    }

    private static void OnIsPaneOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (NavigationView)d;
        view.UpdatePaneMetrics();
        if ((bool)e.NewValue)
        {
            view.CloseAllFlyouts();
        }

        view.SyncAllItems();
    }

    private static void OnPaneDisplayModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (NavigationView)d;
        switch ((NavigationViewPaneDisplayMode)e.NewValue)
        {
            case NavigationViewPaneDisplayMode.Left:
                view.SetCurrentValue(IsPaneOpenProperty, true);
                break;

            case NavigationViewPaneDisplayMode.LeftCompact:
                view.SetCurrentValue(IsPaneOpenProperty, false);
                break;

            case NavigationViewPaneDisplayMode.Auto:
                if (view.ActualWidth > 0)
                {
                    view.EvaluateAutoCollapse(view.ActualWidth, forceOnInitial: true);
                }

                break;
        }
    }

    private static void OnAutoCollapseWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (NavigationView)d;
        if (view.ActualWidth > 0)
        {
            view.EvaluateAutoCollapse(view.ActualWidth, forceOnInitial: true);
        }
    }

    private void EvaluateAutoCollapse(double width, bool forceOnInitial = false)
    {
        if (PaneDisplayMode != NavigationViewPaneDisplayMode.Auto || width <= 0)
        {
            return;
        }

        var isBelow = width < AutoCollapseWidth;
        if (forceOnInitial || isBelow != _wasBelowAutoCollapseWidth)
        {
            _wasBelowAutoCollapseWidth = isBelow;
            SetCurrentValue(IsPaneOpenProperty, !isBelow);
        }
    }

    private static void OnMenuItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (NavigationView)d;
        view.SetValue(EffectiveMenuItemsPropertyKey, (IEnumerable?)e.NewValue ?? view.MenuItems);
        view.RefreshAll();
    }

    private static void OnFooterMenuItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (NavigationView)d;
        view.SetValue(EffectiveFooterMenuItemsPropertyKey, (IEnumerable?)e.NewValue ?? view.FooterMenuItems);
        view.RefreshAll();
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (NavigationView)d;
        view.SyncSelectionVisuals();
        view.RaiseEvent(new NavigationViewSelectionChangedEventArgs(
            SelectionChangedEvent, view, e.NewValue, e.NewValue as NavigationViewItem));
    }

    private void UpdatePaneMetrics()
    {
        SetValue(IsCompactPanePropertyKey, !IsPaneOpen);
        SetValue(PaneWidthPropertyKey, IsPaneOpen ? OpenPaneLength : CompactPaneLength);
    }

    private void RefreshAll()
    {
        DetachAllHandlers();
        AttachTreeHandlers(EffectiveMenuItems);
        AttachTreeHandlers(EffectiveFooterMenuItems);
        UpdateHasFooterMenuItems();
        SyncAllItems();
    }

    private void UpdateHasFooterMenuItems()
    {
        var hasAny = false;
        foreach (var _ in EffectiveFooterMenuItems)
        {
            hasAny = true;
            break;
        }

        SetValue(HasFooterMenuItemsPropertyKey, hasAny);
    }

    private void DetachAllHandlers()
    {
        foreach (var collection in _hookedCollections)
        {
            collection.CollectionChanged -= OnAnyCollectionChanged;
        }

        _hookedCollections.Clear();

        foreach (var item in _hookedItems)
        {
            item.Invoked -= OnItemInvoked;
            item.ChildrenStructureChanged -= OnItemChildrenChanged;
        }

        _hookedItems.Clear();
    }

    private void AttachTreeHandlers(IEnumerable? collection)
    {
        if (collection is null)
        {
            return;
        }

        if (collection is INotifyCollectionChanged incc && _hookedCollections.Add(incc))
        {
            incc.CollectionChanged += OnAnyCollectionChanged;
        }

        foreach (var entry in collection)
        {
            if (entry is NavigationViewItem item)
            {
                if (_hookedItems.Add(item))
                {
                    item.Invoked += OnItemInvoked;
                    item.ChildrenStructureChanged += OnItemChildrenChanged;
                }

                AttachTreeHandlers(item.MenuItems);
            }
        }
    }

    private void OnAnyCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshAll();

    private void OnItemChildrenChanged(NavigationViewItem item) => RefreshAll();

    private void OnItemInvoked(NavigationViewItem item)
    {
        if (item.HasMenuItems)
        {
            if (IsPaneOpen)
            {
                item.SetCurrentValue(NavigationViewItem.IsExpandedProperty, !item.IsExpanded);
                if (item.SelectsOnInvoked == true)
                {
                    SetCurrentValue(SelectedItemProperty, item);
                }
            }
            else
            {
                var nextOpen = !item.IsFlyoutOpen;
                CloseAllFlyouts(except: nextOpen ? item : null);
                item.SetCurrentValue(NavigationViewItem.IsFlyoutOpenProperty, nextOpen);
            }

            return;
        }

        if (item.SelectsOnInvoked != false)
        {
            CloseAllFlyouts();
            SetCurrentValue(SelectedItemProperty, item);
        }
    }

    private void CloseAllFlyouts(NavigationViewItem? except = null)
    {
        foreach (var item in _hookedItems)
        {
            if (!ReferenceEquals(item, except) && item.IsFlyoutOpen)
            {
                item.SetCurrentValue(NavigationViewItem.IsFlyoutOpenProperty, false);
            }
        }
    }

    private void SyncAllItems()
    {
        var isCompact = !IsPaneOpen;
        SyncItemsCollection(EffectiveMenuItems, depth: 0, topLevelCompact: isCompact);
        SyncItemsCollection(EffectiveFooterMenuItems, depth: 0, topLevelCompact: isCompact);
        SyncSelectionVisuals();
    }

    private static void SyncItemsCollection(IEnumerable items, int depth, bool topLevelCompact)
    {
        foreach (var entry in items)
        {
            switch (entry)
            {
                case NavigationViewItemHeader header:
                    header.SetIsCompact(topLevelCompact);
                    break;

                case NavigationViewItem item:
                    // Nested children inside inline expanded trees or flyout popups always show icon + label.
                    item.SetLayoutState(depth, isCompact: depth == 0 && topLevelCompact);
                    if (item.HasMenuItems)
                    {
                        SyncItemsCollection(item.MenuItems, depth + 1, topLevelCompact);
                    }

                    break;
            }
        }
    }

    private void SyncSelectionVisuals()
    {
        var selected = SelectedItem;
        SyncSelectionInCollection(EffectiveMenuItems, selected);
        SyncSelectionInCollection(EffectiveFooterMenuItems, selected);
    }

    private static bool SyncSelectionInCollection(IEnumerable items, object? selected)
    {
        var anySelectedHere = false;

        foreach (var entry in items)
        {
            if (entry is not NavigationViewItem item)
            {
                continue;
            }

            var isSelfSelected = ReferenceEquals(entry, selected);
            var isChildSelected = item.HasMenuItems && SyncSelectionInCollection(item.MenuItems, selected);
            item.SetSelectionState(isSelfSelected, isChildSelected);

            if (isSelfSelected || isChildSelected)
            {
                anySelectedHere = true;
            }
        }

        return anySelectedHere;
    }
}
