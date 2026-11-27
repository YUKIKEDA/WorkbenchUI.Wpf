using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Theming;

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
    public void WorkbenchTheme_ResolvesAllNavigationViewResourcesWithoutUndefinedTokens()
    {
        StaTestRunner.Run(() =>
        {
            var theme = new WorkbenchTheme();
            Application.Current!.Resources.MergedDictionaries.Add(theme);
            try
            {
                var item = new NavigationViewItem { Content = "Home", Icon = WorkbenchIcons.Home };
                var header = new NavigationViewItemHeader { Content = "Section" };
                var nav = new NavigationView();
                nav.MenuItems.Add(header);
                nav.MenuItems.Add(item);
                nav.MenuItems.Add(new NavigationViewItemSeparator());

                MeasureAndLayout(nav);

                var expectedSecondary = Assert.IsType<SolidColorBrush>(theme["Wb.Brush.Text.Secondary"]);
                var itemForeground = Assert.IsType<SolidColorBrush>(item.Foreground);
                Assert.Equal(expectedSecondary.Color, itemForeground.Color);

                var toggleButton = Assert.IsAssignableFrom<Button>(nav.Template.FindName("PART_PaneToggleButton", nav));
                var toggleForeground = Assert.IsType<SolidColorBrush>(toggleButton.Foreground);
                Assert.Equal(expectedSecondary.Color, toggleForeground.Color);
            }
            finally
            {
                Application.Current.Resources.MergedDictionaries.Remove(theme);
                ThemeManager.ResetAccent();
                ThemeManager.SetTheme(WorkbenchThemeVariant.Dark);
            }
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

    [Fact]
    public void CompactMode_ThreeLevelNesting_ExpandsSubgroupInsideFlyoutAndEscapeClosesFlyout()
    {
        StaTestRunner.Run(() =>
        {
            var leaf = new NavigationViewItem { Content = "Leaf" };
            var subGroup = new NavigationViewItem { Content = "SubGroup" };
            subGroup.MenuItems.Add(leaf);
            var rootGroup = new NavigationViewItem { Content = "RootGroup", Icon = WorkbenchIcons.Layers };
            rootGroup.MenuItems.Add(subGroup);

            var nav = new NavigationView { PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact };
            nav.MenuItems.Add(rootGroup);
            MeasureAndLayout(nav);

            Assert.True(rootGroup.IsCompact);
            Assert.False(subGroup.IsCompact);
            Assert.False(leaf.IsCompact);
            Assert.Equal(0, subGroup.Depth);
            Assert.Equal(1, leaf.Depth);

            InvokeItemViaKeyboard(rootGroup);
            nav.UpdateLayout();
            Assert.True(rootGroup.IsFlyoutOpen);

            // Invoking subgroup inside compact flyout must expand it inline WITHOUT closing parent flyout.
            InvokeItemViaKeyboard(subGroup);
            nav.UpdateLayout();
            Assert.True(rootGroup.IsFlyoutOpen);
            Assert.True(subGroup.IsExpanded);

            // Escape raised on descendant child inside flyout closes rootGroup flyout.
            leaf.RaiseEvent(new KeyEventArgs(
                Keyboard.PrimaryDevice,
                PresentationSource.FromVisual(leaf) ?? new DummyPresentationSource(),
                0,
                Key.Escape)
            {
                RoutedEvent = UIElement.KeyDownEvent,
            });
            Assert.False(rootGroup.IsFlyoutOpen);
        });
    }

    [Fact]
    public void CompactMode_ParentWithSelectsOnInvokedTrue_SelectsAndOpensFlyout()
    {
        StaTestRunner.Run(() =>
        {
            var group = new NavigationViewItem { Content = "Group", Icon = WorkbenchIcons.Cube, SelectsOnInvoked = true };
            group.MenuItems.Add(new NavigationViewItem { Content = "Child" });

            var nav = new NavigationView { PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact };
            nav.MenuItems.Add(group);
            MeasureAndLayout(nav);

            InvokeItemViaKeyboard(group);

            Assert.True(group.IsFlyoutOpen);
            Assert.True(group.IsSelected);
            Assert.Same(group, nav.SelectedItem);
        });
    }

    [Fact]
    public void UIElementContent_DoesNotThrowDualParentException()
    {
        StaTestRunner.Run(() =>
        {
            var customPanel = new StackPanel { Orientation = Orientation.Horizontal };
            customPanel.Children.Add(new TextBlock { Text = "PanelItem" });

            var itemA = new NavigationViewItem { Content = customPanel };
            var itemB = new NavigationViewItem { Content = new TextBlock { Text = "TextBlockItem" } };
            itemB.MenuItems.Add(new NavigationViewItem { Content = "Child" });

            var nav = new NavigationView();
            nav.MenuItems.Add(itemA);
            nav.MenuItems.Add(itemB);
            MeasureAndLayout(nav, width: 1000);

            Assert.Null(itemA.LabelText);
            Assert.Equal("TextBlockItem", itemB.LabelText);
            Assert.Equal("T", itemB.CompactMonogram);

            nav.IsPaneOpen = false;
            nav.UpdateLayout();
            InvokeItemViaKeyboard(itemB);
            nav.UpdateLayout();
            Assert.True(itemB.IsFlyoutOpen);
        });
    }

    [Fact]
    public void AutoMode_PreservesExplicitInitialCollapsedAndReloadState()
    {
        StaTestRunner.Run(() =>
        {
            var nav = new NavigationView { IsPaneOpen = false };
            MeasureAndLayout(nav, width: 1200);

            // Explicit IsPaneOpen=False on wide window must not be overwritten by initial Loaded/SizeChanged.
            Assert.False(nav.IsPaneOpen);

            // Simulate Unloaded followed by Loaded -> state stays preserved.
            nav.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            nav.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            Assert.False(nav.IsPaneOpen);
        });
    }

    [Fact]
    public void NavigationViewItem_IsSelected_TwoWaySyncsWithNavigationView()
    {
        StaTestRunner.Run(() =>
        {
            var item1 = new NavigationViewItem { Content = "One", IsSelected = true };
            var item2 = new NavigationViewItem { Content = "Two" };
            var nav = new NavigationView();
            nav.MenuItems.Add(item1);
            nav.MenuItems.Add(item2);

            MeasureAndLayout(nav);
            Assert.Same(item1, nav.SelectedItem);

            var events = 0;
            nav.SelectionChanged += (_, _) => events++;

            item2.IsSelected = true;
            Assert.Same(item2, nav.SelectedItem);
            Assert.False(item1.IsSelected);
            Assert.True(item2.IsSelected);
            Assert.Equal(1, events);
        });
    }

    [Fact]
    public void MenuItemsSource_DynamicChanges_UpdateSelectionAndLayoutState()
    {
        StaTestRunner.Run(() =>
        {
            var items = new ObservableCollection<object>
            {
                new NavigationViewItem { Content = "First", Icon = WorkbenchIcons.Home },
            };

            var nav = new NavigationView { MenuItemsSource = items, IsPaneOpen = false };
            MeasureAndLayout(nav);

            var added = new NavigationViewItem { Content = "Second", Icon = WorkbenchIcons.Settings };
            items.Add(added);

            Assert.True(added.IsCompact);
            InvokeItemViaKeyboard(added);
            Assert.Same(added, nav.SelectedItem);
            Assert.True(added.IsSelected);
        });
    }

    [Fact]
    public void PaneLengths_NaNValues_CoerceToFiniteDefaults()
    {
        StaTestRunner.Run(() =>
        {
            var nav = new NavigationView
            {
                CompactPaneLength = double.NaN,
                OpenPaneLength = double.PositiveInfinity,
            };

            Assert.True(double.IsFinite(nav.CompactPaneLength));
            Assert.True(nav.CompactPaneLength >= 32.0);
            Assert.True(double.IsFinite(nav.OpenPaneLength));
            Assert.True(nav.OpenPaneLength >= nav.CompactPaneLength);
        });
    }

    [Fact]
    public void MouseUpWithoutMouseDown_DoesNotInvokeItem()
    {
        StaTestRunner.Run(() =>
        {
            var item = new NavigationViewItem { Content = "Home" };
            var nav = new NavigationView();
            nav.MenuItems.Add(item);
            MeasureAndLayout(nav);

            var buttonBorder = Assert.IsType<Border>(item.Template.FindName("PART_ItemButton", item));
            buttonBorder.RaiseEvent(new MouseButtonEventArgs(
                Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonUpEvent,
            });

            Assert.Null(nav.SelectedItem);
        });
    }
}
