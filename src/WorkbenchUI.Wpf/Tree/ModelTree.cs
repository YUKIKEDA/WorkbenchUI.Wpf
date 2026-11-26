using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Virtualized hierarchical model tree control.
/// Projects the node hierarchy into a flat list via <see cref="FlatTreeSource"/> and delegates virtualization,
/// multi-selection (including Shift range selection), and keyboard navigation to an internal <see cref="ListBox"/>,
/// with built-in tri-state visibility toggle propagation.
/// </summary>
[TemplatePart(Name = PartList, Type = typeof(ListBox))]
public class ModelTree : Control
{
    private const string PartList = "PART_List";

    /// <summary>Template part name of the visibility toggle button in each row.</summary>
    internal const string VisibilityTogglePartName = "PART_VisibilityToggle";

    /// <summary>Template part name of the inline rename <see cref="TextBox"/> in each row.</summary>
    internal const string RenameBoxPartName = "PART_RenameBox";

    private ListBox? _list;
    private FlatTreeSource? _source;
    private FlatTreeItem? _renamingItem;
    private bool _endingRename;

    static ModelTree()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ModelTree), new FrameworkPropertyMetadata(typeof(ModelTree)));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelTree"/> class.
    /// </summary>
    public ModelTree()
    {
        // Visibility toggles live inside virtualized row templates, so handle their bubbling Click events centrally.
        AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnItemButtonClick));

        // The inline rename TextBox also lives inside virtualized rows, so handle its bubbling events centrally.
        AddHandler(KeyDownEvent, new KeyEventHandler(OnRenameBoxKeyDown));
        AddHandler(LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnRenameBoxLostFocus));
    }

    /// <summary>Occurs when the selection in the internal <see cref="ListBox"/> changes.</summary>
    public event SelectionChangedEventHandler? SelectionChanged;

    /// <summary>Identifies the <see cref="ItemsSource"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable<ITreeNode>), typeof(ModelTree),
        new PropertyMetadata(null, OnItemsSourceChanged));

    /// <summary>Gets or sets the root node collection. Observable collections are tracked for additions and removals.</summary>
    public IEnumerable<ITreeNode>? ItemsSource
    {
        get => (IEnumerable<ITreeNode>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectionMode"/> dependency property.</summary>
    public static readonly DependencyProperty SelectionModeProperty =
        ListBox.SelectionModeProperty.AddOwner(typeof(ModelTree),
            new FrameworkPropertyMetadata(SelectionMode.Extended));

    /// <summary>Gets or sets the selection behavior of the tree.</summary>
    public SelectionMode SelectionMode
    {
        get => (SelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    /// <summary>Identifies the <see cref="ShowVisibilityToggles"/> dependency property.</summary>
    public static readonly DependencyProperty ShowVisibilityTogglesProperty = DependencyProperty.Register(
        nameof(ShowVisibilityToggles), typeof(bool), typeof(ModelTree), new PropertyMetadata(true));

    /// <summary>Gets or sets a value indicating whether the visibility toggle column is shown.</summary>
    public bool ShowVisibilityToggles
    {
        get => (bool)GetValue(ShowVisibilityTogglesProperty);
        set => SetValue(ShowVisibilityTogglesProperty, value);
    }

    private static readonly DependencyPropertyKey FlatItemsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(FlatItems), typeof(ReadOnlyObservableCollection<FlatTreeItem>), typeof(ModelTree),
        new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="FlatItems"/> dependency property.</summary>
    public static readonly DependencyProperty FlatItemsProperty = FlatItemsPropertyKey.DependencyProperty;

    /// <summary>Gets the flattened list of visible rows bound by the template's <see cref="ListBox"/>.</summary>
    public ReadOnlyObservableCollection<FlatTreeItem>? FlatItems =>
        (ReadOnlyObservableCollection<FlatTreeItem>?)GetValue(FlatItemsProperty);

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var tree = (ModelTree)d;
        tree._source?.Dispose();
        tree._source = e.NewValue is IEnumerable<ITreeNode> roots ? new FlatTreeSource(roots) : null;
        tree.SetValue(FlatItemsPropertyKey, tree._source?.Items);
    }

    /// <summary>Toggles visibility for the specified node and all its descendants, updating mixed ancestor states.</summary>
    public void ToggleVisibility(ITreeNode node) => _source?.ToggleVisibility(node);

    /// <summary>Sets visibility for the specified node and all its descendants, updating mixed ancestor states.</summary>
    public void SetVisibility(ITreeNode node, bool isVisible) => _source?.SetVisibility(node, isVisible);

    /// <summary>Collects all selected nodes across the entire tree, including nodes under collapsed branches.</summary>
    public IReadOnlyList<ITreeNode> GetSelectedNodes()
    {
        var result = new List<ITreeNode>();
        if (ItemsSource is not null)
        {
            Collect(ItemsSource);
        }

        return result;

        void Collect(IEnumerable<ITreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.IsSelected)
                {
                    result.Add(node);
                }

                Collect(node.Children);
            }
        }
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_list is not null)
        {
            _list.SelectionChanged -= OnListSelectionChanged;
        }

        base.OnApplyTemplate();
        _list = GetTemplateChild(PartList) as ListBox;
        if (_list is not null)
        {
            _list.SelectionChanged += OnListSelectionChanged;
        }
    }

    private void OnListSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        SelectionChanged?.Invoke(this, e);

    private void OnItemButtonClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { Name: VisibilityTogglePartName } element
            && element.DataContext is FlatTreeItem item)
        {
            _source?.ToggleVisibility(item.Node);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Begins inline renaming for the specified node.
    /// Does nothing if the node does not implement <see cref="IRenamableNode"/> or is currently hidden under a collapsed branch.
    /// Pressing Enter or losing focus commits the edit (whitespace-only names are ignored); pressing Esc cancels it.
    /// </summary>
    public void BeginRename(ITreeNode node)
    {
        if (node is not IRenamableNode || _source is null || _list is null)
        {
            return;
        }

        var item = _source.Items.FirstOrDefault(i => ReferenceEquals(i.Node, node));
        if (item is null)
        {
            return;
        }

        EndRename(commit: true);
        _renamingItem = item;
        item.IsRenaming = true;

        // Wait until the virtualized row container is realized before focusing its rename TextBox.
        _list.ScrollIntoView(item);
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
        {
            if (!ReferenceEquals(_renamingItem, item))
            {
                return;
            }

            var container = _list.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
            if (FindDescendant<TextBox>(container, RenameBoxPartName) is TextBox box)
            {
                box.Focus();
                box.SelectAll();
            }
        });
    }

    private void OnRenameBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (_renamingItem is null ||
            e.OriginalSource is not TextBox { Name: RenameBoxPartName })
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            EndRename(commit: true);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            EndRename(commit: false);
        }
    }

    private void OnRenameBoxLostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (_renamingItem is not null &&
            e.OriginalSource is TextBox { Name: RenameBoxPartName })
        {
            EndRename(commit: true);
        }
    }

    private void EndRename(bool commit)
    {
        if (_renamingItem is null || _endingRename)
        {
            return;
        }

        _endingRename = true;
        try
        {
            var item = _renamingItem;
            _renamingItem = null;

            if (commit &&
                _list?.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem container &&
                FindDescendant<TextBox>(container, RenameBoxPartName) is TextBox box &&
                item.Node is IRenamableNode renamable)
            {
                var newName = box.Text.Trim();
                if (newName.Length > 0)
                {
                    renamable.Name = newName;
                }
            }

            item.IsRenaming = false;

            // Do not steal focus back if keyboard focus moved outside this control (e.g., user clicked another control).
            if (IsKeyboardFocusWithin)
            {
                FocusItem(item);
            }
        }
        finally
        {
            _endingRename = false;
        }
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Handled || _list?.SelectedItem is not FlatTreeItem item)
        {
            return;
        }

        // Pass Left/Right through to the rename TextBox for caret movement while renaming.
        if (_renamingItem is not null)
        {
            return;
        }

        if (e.Key == Key.F2 && item.Node is IRenamableNode)
        {
            BeginRename(item.Node);
            e.Handled = true;
            return;
        }

        // Handle Left/Right as standard tree expand/collapse before the internal ListBox intercepts them for directional navigation.
        if (e.Key == Key.Right)
        {
            if (item.HasChildren && !item.Node.IsExpanded)
            {
                item.Node.IsExpanded = true;
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Left)
        {
            if (item.HasChildren && item.Node.IsExpanded)
            {
                item.Node.IsExpanded = false;
                e.Handled = true;
            }
            else if (item.Parent is not null && _source is not null && _list is not null)
            {
                var parentItem = _source.Items.FirstOrDefault(i => ReferenceEquals(i.Node, item.Parent));
                if (parentItem is not null)
                {
                    _list.UnselectAll();
                    _list.SelectedItem = parentItem;
                    FocusItem(parentItem);
                    e.Handled = true;
                }
            }
        }
    }

    /// <inheritdoc />
    protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseRightButtonDown(e);

        // Select an unselected row exclusively on right-click before opening its context menu.
        var container = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (container is { IsSelected: false } && _list is not null)
        {
            _list.UnselectAll();
            container.IsSelected = true;
            container.Focus();
        }
    }

    private void FocusItem(FlatTreeItem item)
    {
        if (_list is null)
        {
            return;
        }

        _list.ScrollIntoView(item);
        _list.UpdateLayout();
        (_list.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem)?.Focus();
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null and not T)
        {
            current = current is Visual or Visual3D
                ? VisualTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }

        return current as T;
    }

    private static T? FindDescendant<T>(DependencyObject? root, string name) where T : FrameworkElement
    {
        if (root is null)
        {
            return null;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed && typed.Name == name)
            {
                return typed;
            }

            if (FindDescendant<T>(child, name) is T found)
            {
                return found;
            }
        }

        return null;
    }
}
