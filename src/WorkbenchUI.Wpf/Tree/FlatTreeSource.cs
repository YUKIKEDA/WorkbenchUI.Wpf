using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Projects a hierarchical <see cref="ITreeNode"/> collection into a flat list for virtualized rendering.
/// </summary>
internal sealed class FlatTreeSource : IDisposable
{
    private readonly ObservableCollection<FlatTreeItem> _items = [];
    private readonly Dictionary<ITreeNode, FlatTreeItem> _map = [];
    private readonly Dictionary<INotifyCollectionChanged, ITreeNode> _childCollectionOwners = [];
    private readonly IEnumerable<ITreeNode> _roots;

    public FlatTreeSource(IEnumerable<ITreeNode> roots)
    {
        _roots = roots;
        Items = new ReadOnlyObservableCollection<FlatTreeItem>(_items);
        if (roots is INotifyCollectionChanged observableRoots)
        {
            observableRoots.CollectionChanged += OnRootsChanged;
        }

        Rebuild();
    }

    /// <summary>Flat list of currently visible nodes (those whose ancestors are all expanded).</summary>
    public ReadOnlyObservableCollection<FlatTreeItem> Items { get; }

    public void Dispose()
    {
        if (_roots is INotifyCollectionChanged observableRoots)
        {
            observableRoots.CollectionChanged -= OnRootsChanged;
        }

        DetachAll();
        _items.Clear();
    }

    // ---------------- Three-state visibility propagation ----------------

    /// <summary>
    /// Toggles node visibility on eye-icon click: sets to visible if currently hidden (<see langword="false"/>),
    /// or hidden if visible or mixed.
    /// </summary>
    public void ToggleVisibility(ITreeNode node) => SetVisibility(node, node.IsVisible == false);

    /// <summary>
    /// Sets the visibility of a node and all descendants, then recalculates mixed (<see langword="null"/>) states on ancestors.
    /// </summary>
    public void SetVisibility(ITreeNode node, bool isVisible)
    {
        SetVisibilityRecursive(node, isVisible);

        var current = _map.TryGetValue(node, out var item) ? item.Parent : null;
        while (current is not null)
        {
            current.IsVisible = AggregateVisibility(current);
            current = _map.TryGetValue(current, out var parentItem) ? parentItem.Parent : null;
        }
    }

    private static void SetVisibilityRecursive(ITreeNode node, bool isVisible)
    {
        node.IsVisible = isVisible;
        foreach (var child in node.Children)
        {
            SetVisibilityRecursive(child, isVisible);
        }
    }

    /// <summary>Computes parent visibility from children (all true -> true, all false -> false, mixed -> null).</summary>
    private static bool? AggregateVisibility(ITreeNode parent)
    {
        var anyVisible = false;
        var anyHidden = false;
        foreach (var child in parent.Children)
        {
            switch (child.IsVisible)
            {
                case true:
                    anyVisible = true;
                    break;
                case false:
                    anyHidden = true;
                    break;
                default:
                    return null;
            }

            if (anyVisible && anyHidden)
            {
                return null;
            }
        }

        return !anyHidden;
    }

    // ---------------- Flattening ----------------

    private void OnRootsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        DetachAll();
        _items.Clear();

        var index = 0;
        foreach (var root in _roots)
        {
            index = InsertSubtree(root, parent: null, level: 0, index);
        }
    }

    /// <summary>Inserts a node and its expanded descendants starting at <paramref name="index"/> and returns the next insertion index.</summary>
    private int InsertSubtree(ITreeNode node, ITreeNode? parent, int level, int index)
    {
        var item = new FlatTreeItem(node, parent, level) { HasChildren = node.Children.Any() };
        _map[node] = item;
        Subscribe(node);
        _items.Insert(index, item);
        index++;

        if (node.IsExpanded)
        {
            foreach (var child in node.Children)
            {
                index = InsertSubtree(child, node, level + 1, index);
            }
        }

        return index;
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ITreeNode node || e.PropertyName != nameof(ITreeNode.IsExpanded))
        {
            return;
        }

        if (!_map.TryGetValue(node, out var item))
        {
            return;
        }

        if (node.IsExpanded)
        {
            ExpandNode(item);
        }
        else
        {
            CollapseNode(item);
        }
    }

    private void ExpandNode(FlatTreeItem item)
    {
        var index = _items.IndexOf(item);
        if (index < 0)
        {
            return;
        }

        // No-op if children are already materialized (idempotent).
        if (index + 1 < _items.Count && ReferenceEquals(_items[index + 1].Parent, item.Node))
        {
            return;
        }

        var insert = index + 1;
        foreach (var child in item.Node.Children)
        {
            insert = InsertSubtree(child, item.Node, item.Level + 1, insert);
        }
    }

    private void CollapseNode(FlatTreeItem item)
    {
        var index = _items.IndexOf(item);
        if (index >= 0)
        {
            RemoveDescendants(index, item.Level);
        }
    }

    /// <summary>Removes all rows immediately following <paramref name="index"/> whose level is greater than <paramref name="level"/>.</summary>
    private void RemoveDescendants(int index, int level)
    {
        while (index + 1 < _items.Count && _items[index + 1].Level > level)
        {
            var removed = _items[index + 1];
            Unsubscribe(removed.Node);
            _map.Remove(removed.Node);
            _items.RemoveAt(index + 1);
        }
    }

    private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is not INotifyCollectionChanged collection
            || !_childCollectionOwners.TryGetValue(collection, out var node)
            || !_map.TryGetValue(node, out var item))
        {
            return;
        }

        item.HasChildren = node.Children.Any();

        if (!node.IsExpanded)
        {
            return;
        }

        // For changes inside an expanded child collection, remove and re-insert the subtree
        // so all collection change actions (including Move and Replace) are handled uniformly and reliably.
        var index = _items.IndexOf(item);
        if (index < 0)
        {
            return;
        }

        RemoveDescendants(index, item.Level);
        var insert = index + 1;
        foreach (var child in node.Children)
        {
            insert = InsertSubtree(child, node, item.Level + 1, insert);
        }
    }

    // ---------------- Subscription management ----------------

    private void Subscribe(ITreeNode node)
    {
        node.PropertyChanged += OnNodePropertyChanged;
        if (node.Children is INotifyCollectionChanged observableChildren)
        {
            _childCollectionOwners[observableChildren] = node;
            observableChildren.CollectionChanged += OnChildrenChanged;
        }
    }

    private void Unsubscribe(ITreeNode node)
    {
        node.PropertyChanged -= OnNodePropertyChanged;
        if (node.Children is INotifyCollectionChanged observableChildren)
        {
            observableChildren.CollectionChanged -= OnChildrenChanged;
            _childCollectionOwners.Remove(observableChildren);
        }
    }

    private void DetachAll()
    {
        foreach (var item in _items)
        {
            Unsubscribe(item.Node);
        }

        _map.Clear();
        _childCollectionOwners.Clear();
    }
}
