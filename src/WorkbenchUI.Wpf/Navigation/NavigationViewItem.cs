using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Represents a selectable navigation destination or expandable group in a <see cref="NavigationView"/>.
/// Supports built-in vector <see cref="Icon"/> geometry, optional <see cref="Badge"/> content,
/// nested <see cref="MenuItems"/> (rendered inline when expanded or in a flyout when compact),
/// and selection state indication.
/// </summary>
[TemplatePart(Name = PartItemButton, Type = typeof(UIElement))]
[TemplatePart(Name = PartInlineChildrenHost, Type = typeof(ItemsControl))]
[TemplatePart(Name = PartFlyoutChildrenHost, Type = typeof(ItemsControl))]
[TemplatePart(Name = PartFlyout, Type = typeof(Popup))]
public class NavigationViewItem : ContentControl
{
    private const string PartItemButton = "PART_ItemButton";
    private const string PartInlineChildrenHost = "PART_InlineChildrenHost";
    private const string PartFlyoutChildrenHost = "PART_FlyoutChildrenHost";
    private const string PartFlyout = "PART_Flyout";

    private UIElement? _itemButton;
    private ItemsControl? _inlineChildrenHost;
    private ItemsControl? _flyoutChildrenHost;
    private Popup? _flyout;
    private UIElement? _flyoutChild;
    private long _flyoutClosedTick = long.MinValue;
    private bool _isPressed;
    private bool _syncingSelection;

    static NavigationViewItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NavigationViewItem),
            new FrameworkPropertyMetadata(typeof(NavigationViewItem)));
    }

    /// <summary>Initializes a new instance of the <see cref="NavigationViewItem"/> class.</summary>
    public NavigationViewItem()
    {
        var items = new ObservableCollection<object>();
        items.CollectionChanged += OnMenuItemsCollectionChanged;
        SetValue(MenuItemsPropertyKey, items);
    }

    #region Dependency properties

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(Geometry), typeof(NavigationViewItem), new PropertyMetadata(null));

    /// <summary>Gets or sets the vector icon <see cref="Geometry"/> displayed for this item.</summary>
    public Geometry? Icon
    {
        get => (Geometry?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the <see cref="Badge"/> dependency property.</summary>
    public static readonly DependencyProperty BadgeProperty = DependencyProperty.Register(
        nameof(Badge), typeof(object), typeof(NavigationViewItem), new PropertyMetadata(null));

    /// <summary>Gets or sets optional badge content (for example, a count or status label) shown on the right.</summary>
    public object? Badge
    {
        get => GetValue(BadgeProperty);
        set => SetValue(BadgeProperty, value);
    }

    /// <summary>Identifies the <see cref="IsExpanded"/> dependency property.</summary>
    public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
        nameof(IsExpanded), typeof(bool), typeof(NavigationViewItem),
        new PropertyMetadata(false, OnIsExpandedChanged));

    /// <summary>Gets or sets a value indicating whether nested child items are expanded inline.</summary>
    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>Identifies the <see cref="IsSelected"/> dependency property.</summary>
    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected), typeof(bool), typeof(NavigationViewItem),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsSelectedChanged));

    /// <summary>Gets or sets a value indicating whether this item is currently selected.</summary>
    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectsOnInvoked"/> dependency property.</summary>
    public static readonly DependencyProperty SelectsOnInvokedProperty = DependencyProperty.Register(
        nameof(SelectsOnInvoked), typeof(bool?), typeof(NavigationViewItem), new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets whether invoking this item selects it.
    /// When <see langword="null"/> (default), leaf items select on invoke while parent items with children toggle expansion without selecting.
    /// </summary>
    public bool? SelectsOnInvoked
    {
        get => (bool?)GetValue(SelectsOnInvokedProperty);
        set => SetValue(SelectsOnInvokedProperty, value);
    }

    /// <summary>Identifies the <see cref="IsFlyoutOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsFlyoutOpenProperty = DependencyProperty.Register(
        nameof(IsFlyoutOpen), typeof(bool), typeof(NavigationViewItem),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsFlyoutOpenChanged));

    /// <summary>Gets or sets a value indicating whether the compact-mode child flyout popup is open.</summary>
    public bool IsFlyoutOpen
    {
        get => (bool)GetValue(IsFlyoutOpenProperty);
        set => SetValue(IsFlyoutOpenProperty, value);
    }

    private static readonly DependencyPropertyKey MenuItemsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(MenuItems), typeof(ObservableCollection<object>), typeof(NavigationViewItem),
        new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="MenuItems"/> dependency property.</summary>
    public static readonly DependencyProperty MenuItemsProperty = MenuItemsPropertyKey.DependencyProperty;

    /// <summary>Gets the collection of nested child navigation items (read-only collection property).</summary>
    public ObservableCollection<object> MenuItems => (ObservableCollection<object>)GetValue(MenuItemsProperty);

    private static readonly DependencyPropertyKey HasMenuItemsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasMenuItems), typeof(bool), typeof(NavigationViewItem), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="HasMenuItems"/> dependency property.</summary>
    public static readonly DependencyProperty HasMenuItemsProperty = HasMenuItemsPropertyKey.DependencyProperty;

    /// <summary>Gets a value indicating whether this item contains nested child items (read-only).</summary>
    public bool HasMenuItems => (bool)GetValue(HasMenuItemsProperty);

    private static readonly DependencyPropertyKey IsCompactPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsCompact), typeof(bool), typeof(NavigationViewItem),
        new PropertyMetadata(false, OnIsCompactChanged));

    /// <summary>Identifies the <see cref="IsCompact"/> dependency property.</summary>
    public static readonly DependencyProperty IsCompactProperty = IsCompactPropertyKey.DependencyProperty;

    /// <summary>Gets a value indicating whether this item should render in compact icon-only rail mode (read-only).</summary>
    public bool IsCompact => (bool)GetValue(IsCompactProperty);

    private static readonly DependencyPropertyKey IsChildSelectedPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsChildSelected), typeof(bool), typeof(NavigationViewItem), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsChildSelected"/> dependency property.</summary>
    public static readonly DependencyProperty IsChildSelectedProperty = IsChildSelectedPropertyKey.DependencyProperty;

    /// <summary>Gets a value indicating whether a descendant child item is currently selected (read-only).</summary>
    public bool IsChildSelected => (bool)GetValue(IsChildSelectedProperty);

    private static readonly DependencyPropertyKey DepthPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Depth), typeof(int), typeof(NavigationViewItem), new PropertyMetadata(0));

    /// <summary>Identifies the <see cref="Depth"/> dependency property.</summary>
    public static readonly DependencyProperty DepthProperty = DepthPropertyKey.DependencyProperty;

    /// <summary>Gets the zero-based hierarchy nesting depth within the navigation tree (read-only).</summary>
    public int Depth => (int)GetValue(DepthProperty);

    private static readonly DependencyPropertyKey LabelTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(LabelText), typeof(string), typeof(NavigationViewItem), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="LabelText"/> dependency property.</summary>
    public static readonly DependencyProperty LabelTextProperty = LabelTextPropertyKey.DependencyProperty;

    /// <summary>
    /// Gets the text label extracted from <see cref="ContentControl.Content"/> for safe use in
    /// tooltips and compact flyout headers without re-parenting <see cref="UIElement"/> content (read-only).
    /// </summary>
    public string? LabelText => (string?)GetValue(LabelTextProperty);

    private static readonly DependencyPropertyKey CompactMonogramPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(CompactMonogram), typeof(string), typeof(NavigationViewItem), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="CompactMonogram"/> dependency property.</summary>
    public static readonly DependencyProperty CompactMonogramProperty = CompactMonogramPropertyKey.DependencyProperty;

    /// <summary>Gets the single uppercase fallback character displayed in compact rail mode when <see cref="Icon"/> is <see langword="null"/> (read-only).</summary>
    public string? CompactMonogram => (string?)GetValue(CompactMonogramProperty);

    #endregion

    internal event Action<NavigationViewItem>? Invoked;
    internal event Action<NavigationViewItem>? ChildrenStructureChanged;
    internal event Action<NavigationViewItem, bool>? IsSelectedChanged;
    internal event Func<NavigationViewItem, bool>? EscapeRequested;

    internal void SetLayoutState(int depth, bool isCompact)
    {
        SetValue(DepthPropertyKey, depth);
        SetValue(IsCompactPropertyKey, isCompact);
    }

    internal void SetSelectionState(bool isSelected, bool isChildSelected)
    {
        _syncingSelection = true;
        try
        {
            SetCurrentValue(IsSelectedProperty, isSelected);
        }
        finally
        {
            _syncingSelection = false;
        }

        SetValue(IsChildSelectedPropertyKey, isChildSelected);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_itemButton is not null)
        {
            _itemButton.MouseLeftButtonDown -= OnItemButtonMouseLeftButtonDown;
            _itemButton.MouseLeftButtonUp -= OnItemButtonMouseLeftButtonUp;
            _itemButton.MouseLeave -= OnItemButtonMouseLeave;
        }

        if (_flyoutChild is not null)
        {
            _flyoutChild.KeyDown -= OnFlyoutChildKeyDown;
        }

        if (_flyout is not null)
        {
            _flyout.Closed -= OnFlyoutClosed;
        }

        // Release active ItemsSource before replacing template parts so UI children detach cleanly.
        if (_inlineChildrenHost is not null)
        {
            _inlineChildrenHost.ItemsSource = null;
        }

        if (_flyoutChildrenHost is not null)
        {
            _flyoutChildrenHost.ItemsSource = null;
        }

        base.OnApplyTemplate();

        _itemButton = GetTemplateChild(PartItemButton) as UIElement;
        _inlineChildrenHost = GetTemplateChild(PartInlineChildrenHost) as ItemsControl;
        _flyoutChildrenHost = GetTemplateChild(PartFlyoutChildrenHost) as ItemsControl;
        _flyout = GetTemplateChild(PartFlyout) as Popup;
        _flyoutChild = _flyout?.Child;

        if (_itemButton is not null)
        {
            _itemButton.MouseLeftButtonDown += OnItemButtonMouseLeftButtonDown;
            _itemButton.MouseLeftButtonUp += OnItemButtonMouseLeftButtonUp;
            _itemButton.MouseLeave += OnItemButtonMouseLeave;
        }

        if (_flyout is not null)
        {
            _flyout.Closed += OnFlyoutClosed;
        }

        if (_flyoutChild is not null)
        {
            // Popup content renders inside a separate PopupRoot visual tree; handle Escape at the child root.
            _flyoutChild.KeyDown += OnFlyoutChildKeyDown;
        }

        SyncChildrenHost();
    }

    /// <inheritdoc />
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);

        var label = newContent switch
        {
            string s => s,
            TextBlock tb => tb.Text,
            DependencyObject dObj => AutomationProperties.GetName(dObj) is { Length: > 0 } name ? name : null,
            _ => newContent?.ToString(),
        };
        SetValue(LabelTextPropertyKey, label);

        string? monogram = null;
        if (!string.IsNullOrWhiteSpace(label))
        {
            foreach (var ch in label)
            {
                if (!char.IsWhiteSpace(ch))
                {
                    monogram = char.ToUpperInvariant(ch).ToString();
                    break;
                }
            }
        }

        SetValue(CompactMonogramPropertyKey, monogram);
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new FrameworkElementAutomationPeer(this);

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled)
        {
            return;
        }

        // Allow Escape from any descendant focused inside the flyout popup to close it.
        if (e.Key == Key.Escape)
        {
            if (IsFlyoutOpen)
            {
                SetCurrentValue(IsFlyoutOpenProperty, false);
                e.Handled = true;
                return;
            }

            if (EscapeRequested?.Invoke(this) == true)
            {
                e.Handled = true;
                return;
            }
        }

        if (!ReferenceEquals(e.OriginalSource, this))
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
            case Key.Space:
                Invoked?.Invoke(this);
                e.Handled = true;
                break;

            case Key.Right when !IsCompact && HasMenuItems && !IsExpanded:
                SetCurrentValue(IsExpandedProperty, true);
                e.Handled = true;
                break;

            case Key.Left when !IsCompact && HasMenuItems && IsExpanded:
                SetCurrentValue(IsExpandedProperty, false);
                e.Handled = true;
                break;
        }
    }

    private void OnFlyoutClosed(object? sender, EventArgs e) =>
        _flyoutClosedTick = Environment.TickCount64;

    private void OnFlyoutChildKeyDown(object sender, KeyEventArgs e)
    {
        if (!e.Handled && e.Key == Key.Escape && IsFlyoutOpen)
        {
            SetCurrentValue(IsFlyoutOpenProperty, false);
            e.Handled = true;
        }
    }

    private bool WasFlyoutJustDismissed() =>
        _flyoutClosedTick != long.MinValue &&
        Environment.TickCount64 - _flyoutClosedTick < 200 &&
        !IsFlyoutOpen;

    private void OnItemButtonMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (WasFlyoutJustDismissed())
        {
            // Clicking the parent icon while StaysOpen=False popup was open closes the popup on mouse-down;
            // suppress the immediately following click so it does not reopen the flyout.
            _isPressed = false;
            e.Handled = true;
            return;
        }

        _isPressed = true;
        e.Handled = true;
    }

    private void OnItemButtonMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPressed)
        {
            return;
        }

        _isPressed = false;
        if (WasFlyoutJustDismissed())
        {
            e.Handled = true;
            return;
        }

        e.Handled = true;
        Focus();
        Invoked?.Invoke(this);
    }

    private void OnItemButtonMouseLeave(object sender, MouseEventArgs e) => _isPressed = false;

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var item = (NavigationViewItem)d;
        if (!item._syncingSelection)
        {
            item.IsSelectedChanged?.Invoke(item, (bool)e.NewValue);
        }
    }

    private void OnMenuItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SetValue(HasMenuItemsPropertyKey, MenuItems.Count > 0);
        SyncChildrenHost();
        ChildrenStructureChanged?.Invoke(this);
    }

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NavigationViewItem)d).SyncChildrenHost();

    private static void OnIsFlyoutOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NavigationViewItem)d).SyncChildrenHost();

    private static void OnIsCompactChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var item = (NavigationViewItem)d;
        if (!(bool)e.NewValue && item.IsFlyoutOpen)
        {
            item.SetCurrentValue(IsFlyoutOpenProperty, false);
        }

        item.SyncChildrenHost();
    }

    /// <summary>
    /// Ensures <see cref="MenuItems"/> is bound to at most one host (<c>PART_InlineChildrenHost</c> OR
    /// <c>PART_FlyoutChildrenHost</c>), always detaching the inactive host first to prevent dual-parent errors.
    /// </summary>
    private void SyncChildrenHost()
    {
        var hasChildren = MenuItems.Count > 0;

        if (IsCompact)
        {
            if (_inlineChildrenHost is not null && _inlineChildrenHost.ItemsSource is not null)
            {
                _inlineChildrenHost.ItemsSource = null;
            }

            if (_flyoutChildrenHost is not null)
            {
                var target = hasChildren && IsFlyoutOpen ? MenuItems : null;
                if (!ReferenceEquals(_flyoutChildrenHost.ItemsSource, target))
                {
                    _flyoutChildrenHost.ItemsSource = target;
                }
            }
        }
        else
        {
            if (_flyoutChildrenHost is not null && _flyoutChildrenHost.ItemsSource is not null)
            {
                _flyoutChildrenHost.ItemsSource = null;
            }

            if (_inlineChildrenHost is not null)
            {
                var target = hasChildren && IsExpanded ? MenuItems : null;
                if (!ReferenceEquals(_inlineChildrenHost.ItemsSource, target))
                {
                    _inlineChildrenHost.ItemsSource = target;
                }
            }
        }
    }
}
