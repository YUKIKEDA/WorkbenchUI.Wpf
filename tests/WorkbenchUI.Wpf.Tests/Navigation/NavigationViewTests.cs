using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Tests.Navigation;

public class NavigationViewTests
{
    private sealed class DummyPresentationSource : PresentationSource
    {
        protected override CompositionTarget GetCompositionTargetCore() => null!;
        public override Visual RootVisual { get; set; } = null!;
        public override bool IsDisposed => false;
    }

    private static void MeasureAndLayout(FrameworkElement element, double width = 960, double height = 600)
    {
        element.BeginInit();
        element.EndInit();
        element.Measure(new Size(width, height));
        element.Arrange(new Rect(0, 0, width, height));
        element.UpdateLayout();
    }

    private static void InvokeItemViaKeyboard(NavigationViewItem item)
    {
        item.RaiseEvent(new KeyEventArgs(
            Keyboard.PrimaryDevice,
            PresentationSource.FromVisual(item) ?? new DummyPresentationSource(),
            0,
            Key.Enter)
        {
            RoutedEvent = UIElement.KeyDownEvent,
        });
    }

    [Fact]
    public void SelectingLeafItem_UpdatesIsSelectedAndRaisesSelectionChanged()
    {
        StaTestRunner.Run(() =>
        {
            var home = new NavigationViewItem { Content = "Home", Icon = WorkbenchIcons.Home };
            var tokens = new NavigationViewItem { Content = "Tokens", Icon = WorkbenchIcons.Settings };
            var nav = new NavigationView();
            nav.MenuItems.Add(home);
            nav.MenuItems.Add(tokens);

            MeasureAndLayout(nav);

            NavigationViewSelectionChangedEventArgs? lastArgs = null;
            nav.SelectionChanged += (_, e) => lastArgs = e;

            InvokeItemViaKeyboard(home);
            Assert.Same(home, nav.SelectedItem);
            Assert.True(home.IsSelected);
            Assert.False(tokens.IsSelected);
            Assert.NotNull(lastArgs);
            Assert.Same(home, lastArgs!.SelectedItem);
            Assert.Same(home, lastArgs.SelectedItemContainer);

            InvokeItemViaKeyboard(tokens);
            Assert.Same(tokens, nav.SelectedItem);
            Assert.False(home.IsSelected);
            Assert.True(tokens.IsSelected);
        });
    }

    [Fact]
    public void ParentItem_WithDefaultSelectsOnInvoked_TogglesExpansionWithoutSelecting()
    {
        StaTestRunner.Run(() =>
        {
            var leaf = new NavigationViewItem { Content = "Leaf" };
            var group = new NavigationViewItem { Content = "Group" };
            var child = new NavigationViewItem { Content = "Child" };
            group.MenuItems.Add(child);

            var nav = new NavigationView();
            nav.MenuItems.Add(leaf);
            nav.MenuItems.Add(group);
            MeasureAndLayout(nav);

            InvokeItemViaKeyboard(leaf);
            Assert.Same(leaf, nav.SelectedItem);

            Assert.True(group.HasMenuItems);
            Assert.False(group.IsExpanded);

            InvokeItemViaKeyboard(group);
            Assert.True(group.IsExpanded);
            Assert.Same(leaf, nav.SelectedItem);
            Assert.False(group.IsSelected);

            InvokeItemViaKeyboard(group);
            Assert.False(group.IsExpanded);
        });
    }

    [Fact]
    public void ParentItem_WithSelectsOnInvokedTrue_SelectsAndExpands()
    {
        StaTestRunner.Run(() =>
        {
            var group = new NavigationViewItem { Content = "Group", SelectsOnInvoked = true };
            group.MenuItems.Add(new NavigationViewItem { Content = "Child" });

            var nav = new NavigationView();
            nav.MenuItems.Add(group);
            MeasureAndLayout(nav);

            InvokeItemViaKeyboard(group);

            Assert.True(group.IsExpanded);
            Assert.True(group.IsSelected);
            Assert.Same(group, nav.SelectedItem);
        });
    }

    [Fact]
    public void SelectingChildItem_SetsParentIsChildSelectedAndChildDepth()
    {
        StaTestRunner.Run(() =>
        {
            var child = new NavigationViewItem { Content = "Button" };
            var group = new NavigationViewItem { Content = "Inputs", IsExpanded = true };
            group.MenuItems.Add(child);

            var nav = new NavigationView();
            nav.MenuItems.Add(group);
            MeasureAndLayout(nav);

            Assert.Equal(0, group.Depth);
            Assert.Equal(1, child.Depth);

            nav.SelectedItem = child;
            Assert.True(child.IsSelected);
            Assert.False(group.IsSelected);
            Assert.True(group.IsChildSelected);

            nav.SelectedItem = null;
            Assert.False(child.IsSelected);
            Assert.False(group.IsChildSelected);
        });
    }

    [Fact]
    public void CompactMode_ToggleFlyoutAndSwitchBackToExpanded_DoesNotThrow()
    {
        StaTestRunner.Run(() =>
        {
            var header = new NavigationViewItemHeader { Content = "Foundation" };
            var child = new NavigationViewItem { Content = "Design Tokens" };
            var group = new NavigationViewItem
            {
                Content = "Foundation Group",
                Icon = WorkbenchIcons.Layers,
                IsExpanded = true,
            };
            group.MenuItems.Add(child);

            var nav = new NavigationView();
            nav.MenuItems.Add(header);
            nav.MenuItems.Add(group);
            MeasureAndLayout(nav, width: 1000);

            Assert.False(group.IsCompact);
            Assert.False(child.IsCompact);
            Assert.False(header.IsCompact);

            // Switch to compact mode: top-level becomes compact, nested child stays non-compact for flyout rendering.
            nav.IsPaneOpen = false;
            nav.UpdateLayout();

            Assert.True(nav.IsCompactPane);
            Assert.Equal(nav.CompactPaneLength, nav.PaneWidth);
            Assert.True(header.IsCompact);
            Assert.True(group.IsCompact);
            Assert.False(child.IsCompact);

            // Invoke parent in compact mode -> opens flyout without altering selection.
            InvokeItemViaKeyboard(group);
            nav.UpdateLayout();
            Assert.True(group.IsFlyoutOpen);

            // Invoke child inside flyout -> selects child and closes flyout.
            InvokeItemViaKeyboard(child);
            nav.UpdateLayout();
            Assert.Same(child, nav.SelectedItem);
            Assert.False(group.IsFlyoutOpen);
            Assert.True(group.IsChildSelected);

            // Re-open flyout then expand pane back -> no dual-parent exception and flyouts close cleanly.
            InvokeItemViaKeyboard(group);
            nav.UpdateLayout();
            Assert.True(group.IsFlyoutOpen);

            nav.IsPaneOpen = true;
            nav.UpdateLayout();
            Assert.False(nav.IsCompactPane);
            Assert.Equal(nav.OpenPaneLength, nav.PaneWidth);
            Assert.False(group.IsCompact);
            Assert.False(group.IsFlyoutOpen);
        });
    }

    [Fact]
    public void PaneDisplayMode_AndAutoCollapseWidth_ControlIsPaneOpen()
    {
        StaTestRunner.Run(() =>
        {
            var nav = new NavigationView { AutoCollapseWidth = 800 };
            MeasureAndLayout(nav, width: 1000);
            Assert.True(nav.IsPaneOpen);

            // Shrinking below threshold in Auto mode collapses the pane.
            nav.Measure(new Size(700, 600));
            nav.Arrange(new Rect(0, 0, 700, 600));
            nav.UpdateLayout();
            Assert.False(nav.IsPaneOpen);

            // Growing above threshold in Auto mode expands the pane.
            nav.Measure(new Size(900, 600));
            nav.Arrange(new Rect(0, 0, 900, 600));
            nav.UpdateLayout();
            Assert.True(nav.IsPaneOpen);

            nav.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
            Assert.False(nav.IsPaneOpen);

            nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
            Assert.True(nav.IsPaneOpen);
        });
    }

    [Fact]
    public void FooterMenuItems_TrackHasFooterMenuItemsAndShareSingleSelection()
    {
        StaTestRunner.Run(() =>
        {
            var mainItem = new NavigationViewItem { Content = "Home" };
            var settings = new NavigationViewItem { Content = "Settings", Icon = WorkbenchIcons.Settings };
            var nav = new NavigationView();
            nav.MenuItems.Add(mainItem);

            MeasureAndLayout(nav);
            Assert.False(nav.HasFooterMenuItems);

            nav.FooterMenuItems.Add(settings);
            Assert.True(nav.HasFooterMenuItems);

            InvokeItemViaKeyboard(mainItem);
            Assert.True(mainItem.IsSelected);
            Assert.False(settings.IsSelected);

            InvokeItemViaKeyboard(settings);
            Assert.Same(settings, nav.SelectedItem);
            Assert.False(mainItem.IsSelected);
            Assert.True(settings.IsSelected);
        });
    }
}
