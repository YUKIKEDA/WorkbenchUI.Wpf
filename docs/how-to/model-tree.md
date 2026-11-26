---
title: Use ModelTree with Tri-State Visibility
description: How to bind ITreeNode hierarchies, handle multi-selection, tri-state visibility propagation, and inline rename.
sidebar_position: 4
---

# How to Use ModelTree

`ModelTree` flattens a node hierarchy into a virtualized `ListBox`, giving you large-model performance, extended multi-selection (`Ctrl` / `Shift`), tri-state visibility toggles, and inline renaming.

## Quick Setup with `TreeNode`

Instantiate or subclass `TreeNode` (which implements `ITreeNode` and `IRenamableNode`):

```csharp
var root = new TreeNode
{
    Name = "Model",
    IsExpanded = true,
    Icon = WorkbenchIcons.Layers,
};
root.Children.Add(new TreeNode { Name = "Part A", Icon = WorkbenchIcons.Cube });
root.Children.Add(new TreeNode { Name = "Part B", Icon = WorkbenchIcons.Cube });

MyModelTree.ItemsSource = new ObservableCollection<ITreeNode> { root };
```

In XAML:

```xml
<wb:ModelTree x:Name="MyModelTree"
              SelectionMode="Extended"
              ShowVisibilityToggles="True"
              SelectionChanged="OnModelTreeSelectionChanged" />
```

## Connecting Existing ViewModels via `ITreeNode`

If your application already has ViewModel base classes, implement `ITreeNode` directly (and optionally `IRenamableNode` for `F2` / programmatic inline renaming):

- Return a stable `ObservableCollection<ITreeNode>` from `Children` so `ModelTree` reacts to additions and removals.
- Raise `PropertyChanged` for `Name`, `IsExpanded`, `IsVisible`, and `IsSelected`.

## Tri-State Visibility Propagation

When the user clicks the eye icon on a row (or you call `MyModelTree.ToggleVisibility(node)` / `SetVisibility(node, isVisible)`):

- Toggling a parent node propagates `true` or `false` to all descendants.
- When children have mixed states, ancestor nodes automatically update `IsVisible` to `null` (indeterminate).

## Multi-Selection and Inline Rename

```csharp
// Read selected nodes
IReadOnlyList<ITreeNode> selected = MyModelTree.GetSelectedNodes();

// Programmatically start inline rename (requires IRenamableNode with CanRename == true)
if (selected.FirstOrDefault() is { } first)
{
    MyModelTree.BeginRename(first);
}
```
