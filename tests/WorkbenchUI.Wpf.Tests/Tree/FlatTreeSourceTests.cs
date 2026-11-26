using WorkbenchUI.Wpf.Controls;
using Xunit;

namespace WorkbenchUI.Wpf.Tests.Tree;

public class FlatTreeSourceTests
{
    private static TreeNode N(string name, params TreeNode[] children)
    {
        var node = new TreeNode { Name = name };
        foreach (var child in children)
        {
            node.Children.Add(child);
        }

        return node;
    }

    private static string[] Names(FlatTreeSource source) =>
        source.Items.Select(i => i.Node.Name).ToArray();

    // ---------------- Flattening ----------------

    [Fact]
    public void InitialFlatten_CollapsedRoots_ShowsRootsOnly()
    {
        var roots = new[] { N("A", N("A1"), N("A2")), N("B") };
        using var source = new FlatTreeSource(roots);

        Assert.Equal(["A", "B"], Names(source));
    }

    [Fact]
    public void InitialFlatten_RespectsIsExpanded()
    {
        var a = N("A", N("A1", N("A1a")), N("A2"));
        a.IsExpanded = true;
        using var source = new FlatTreeSource([a, N("B")]);

        Assert.Equal(["A", "A1", "A2", "B"], Names(source));
    }

    [Fact]
    public void Levels_AreComputedFromDepth()
    {
        var a = N("A", N("A1", N("A1a")));
        a.IsExpanded = true;
        ((TreeNode)a.Children[0]).IsExpanded = true;
        using var source = new FlatTreeSource([a]);

        Assert.Equal([0, 1, 2], source.Items.Select(i => i.Level).ToArray());
        Assert.Null(source.Items[0].Parent);
        Assert.Same(a, source.Items[1].Parent);
    }

    [Fact]
    public void HasChildren_ReflectsChildPresence()
    {
        var a = N("A", N("A1"));
        a.IsExpanded = true;
        using var source = new FlatTreeSource([a]);

        Assert.True(source.Items[0].HasChildren);
        Assert.False(source.Items[1].HasChildren);
    }

    // ---------------- Expand / Collapse ----------------

    [Fact]
    public void Expand_InsertsChildrenAfterParent()
    {
        var a = N("A", N("A1"), N("A2"));
        using var source = new FlatTreeSource([a, N("B")]);

        a.IsExpanded = true;

        Assert.Equal(["A", "A1", "A2", "B"], Names(source));
    }

    [Fact]
    public void Collapse_RemovesWholeSubtree_IncludingExpandedDescendants()
    {
        var a1 = N("A1", N("A1a"), N("A1b"));
        a1.IsExpanded = true;
        var a = N("A", a1, N("A2"));
        a.IsExpanded = true;
        using var source = new FlatTreeSource([a, N("B")]);

        Assert.Equal(["A", "A1", "A1a", "A1b", "A2", "B"], Names(source));

        a.IsExpanded = false;

        Assert.Equal(["A", "B"], Names(source));
    }

    [Fact]
    public void Expand_AfterCollapse_RestoresDescendantExpansionState()
    {
        var a1 = N("A1", N("A1a"));
        a1.IsExpanded = true;
        var a = N("A", a1);
        a.IsExpanded = true;
        using var source = new FlatTreeSource([a]);

        a.IsExpanded = false;
        a.IsExpanded = true;

        // A1's expanded state is preserved, so A1a becomes visible again
        Assert.Equal(["A", "A1", "A1a"], Names(source));
    }

    // ---------------- Dynamic Child Collection Changes ----------------

    [Fact]
    public void ChildAdded_WhileExpanded_AppearsInList()
    {
        var a = N("A", N("A1"));
        a.IsExpanded = true;
        using var source = new FlatTreeSource([a, N("B")]);

        a.Children.Add(N("A2"));

        Assert.Equal(["A", "A1", "A2", "B"], Names(source));
    }

    [Fact]
    public void ChildRemoved_WhileExpanded_DisappearsFromList()
    {
        var a1 = N("A1");
        var a = N("A", a1, N("A2"));
        a.IsExpanded = true;
        using var source = new FlatTreeSource([a]);

        a.Children.Remove(a1);

        Assert.Equal(["A", "A2"], Names(source));
    }

    [Fact]
    public void ChildAdded_ToLeaf_UpdatesHasChildren()
    {
        var a = N("A");
        using var source = new FlatTreeSource([a]);
        Assert.False(source.Items[0].HasChildren);

        a.Children.Add(N("A1"));

        Assert.True(source.Items[0].HasChildren);
        // Remains collapsed, so it does not appear in the flattened list
        Assert.Equal(["A"], Names(source));
    }

    [Fact]
    public void RootsCollectionChanged_RebuildsList()
    {
        var roots = new System.Collections.ObjectModel.ObservableCollection<ITreeNode> { N("A") };
        using var source = new FlatTreeSource(roots);

        roots.Add(N("B"));

        Assert.Equal(["A", "B"], Names(source));
    }

    // ---------------- Tri-State Visibility Propagation ----------------

    [Fact]
    public void ToggleVisibility_OnParent_PropagatesToAllDescendants()
    {
        var a = N("A", N("A1", N("A1a")), N("A2"));
        a.IsExpanded = true;
        using var source = new FlatTreeSource([a]);

        source.ToggleVisibility(a);

        Assert.False(a.IsVisible);
        Assert.All(a.Children, c => Assert.False(c.IsVisible));
        Assert.False(a.Children[0].Children.Single().IsVisible);
    }

    [Fact]
    public void HideOneChild_ParentBecomesMixed()
    {
        var a1 = N("A1");
        var a = N("A", a1, N("A2"));
        a.IsExpanded = true;
        using var source = new FlatTreeSource([a]);

        source.ToggleVisibility(a1);

        Assert.False(a1.IsVisible);
        Assert.Null(a.IsVisible); // Mixed
    }

    [Fact]
    public void HideAllChildren_ParentBecomesHidden()
    {
        var a1 = N("A1");
        var a2 = N("A2");
        var a = N("A", a1, a2);
        a.IsExpanded = true;
        using var source = new FlatTreeSource([a]);

        source.ToggleVisibility(a1);
        source.ToggleVisibility(a2);

        Assert.False(a.IsVisible);
    }

    [Fact]
    public void MixedState_PropagatesUpToGrandparent()
    {
        var leaf = N("A1a");
        var a1 = N("A1", leaf, N("A1b"));
        a1.IsExpanded = true;
        var a = N("A", a1);
        a.IsExpanded = true;
        using var source = new FlatTreeSource([a]);

        source.ToggleVisibility(leaf);

        Assert.Null(a1.IsVisible);
        Assert.Null(a.IsVisible);
    }

    [Fact]
    public void ToggleHiddenNode_ShowsSubtree_AndRestoresAncestors()
    {
        var a1 = N("A1", N("A1a"));
        a1.IsExpanded = true;
        var a = N("A", a1, N("A2"));
        a.IsExpanded = true;
        using var source = new FlatTreeSource([a]);

        source.ToggleVisibility(a1); // Hide -> parent becomes mixed
        Assert.Null(a.IsVisible);

        source.ToggleVisibility(a1); // Show again

        Assert.True(a1.IsVisible);
        Assert.True(a1.Children.Single().IsVisible);
        Assert.True(a.IsVisible); // All children are visible again, so parent becomes visible
    }

    [Fact]
    public void ToggleMixedParent_HidesEverything()
    {
        var a1 = N("A1");
        var a = N("A", a1, N("A2"));
        a.IsExpanded = true;
        using var source = new FlatTreeSource([a]);

        source.ToggleVisibility(a1); // Parent becomes mixed (null)
        source.ToggleVisibility(a);  // Toggle mixed parent -> hide all

        Assert.False(a.IsVisible);
        Assert.All(a.Children, c => Assert.False(c.IsVisible));
    }

    [Fact]
    public void SetVisibility_OnCollapsedSubtree_StillPropagates()
    {
        // Propagates even to collapsed descendants not currently visible
        var a = N("A", N("A1", N("A1a")));
        using var source = new FlatTreeSource([a]); // A starts collapsed

        source.SetVisibility(a, false);

        Assert.False(a.Children[0].IsVisible);
        Assert.False(a.Children[0].Children.Single().IsVisible);
    }
}
