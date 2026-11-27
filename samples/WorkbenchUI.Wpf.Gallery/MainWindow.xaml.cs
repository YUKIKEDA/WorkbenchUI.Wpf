using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Gallery.Infrastructure;
using WorkbenchUI.Wpf.Theming;

namespace WorkbenchUI.Wpf.Gallery;

public partial class MainWindow : WorkbenchWindow, IGalleryHost
{
    private readonly Dictionary<string, NavigationViewItem> _navItemsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, NavigationViewItem> _parentGroupsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(AccentPreset Preset, WorkbenchIcon CheckIcon)> _accentRows = [];
    private DependencyPropertyDescriptor? _paneModeDescriptor;
    private DependencyPropertyDescriptor? _isPaneOpenDescriptor;
    private bool _suppressNavSelection;

    public event EventHandler? ShellStateChanged;

    public NavigationViewPaneDisplayMode PaneDisplayMode
    {
        get => RootNav.PaneDisplayMode;
        set => RootNav.PaneDisplayMode = value;
    }

    public bool IsPaneOpen
    {
        get => RootNav.IsPaneOpen;
        set => RootNav.IsPaneOpen = value;
    }

    public MainWindow()
    {
        InitializeComponent();

        BuildNavigationTree();
        BuildAccentPopover();

        GlobalSearchBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(OnSearchTextChanged));
        ThemeManager.ThemeChanged += OnThemeManagerChanged;
        _paneModeDescriptor = DependencyPropertyDescriptor.FromProperty(NavigationView.PaneDisplayModeProperty, typeof(NavigationView));
        _paneModeDescriptor?.AddValueChanged(RootNav, OnRootNavShellPropertyChanged);
        _isPaneOpenDescriptor = DependencyPropertyDescriptor.FromProperty(NavigationView.IsPaneOpenProperty, typeof(NavigationView));
        _isPaneOpenDescriptor?.AddValueChanged(RootNav, OnRootNavShellPropertyChanged);
        Closed += OnWindowClosed;

        var args = Environment.GetCommandLineArgs();

        // --light: Launch with the Light theme (for visual inspection and screenshots)
        if (args.Contains("--light"))
        {
            ThemeManager.SetTheme(WorkbenchThemeVariant.Light);
        }

        UpdateThemeAndAccentVisuals();

        // Determine initial page (--page <id> overrides Home)
        var initialPageId = GalleryCatalog.HomePageId;
        var pageArgIdx = Array.IndexOf(args, "--page");
        if (pageArgIdx >= 0 && pageArgIdx + 1 < args.Length && GalleryCatalog.FindById(args[pageArgIdx + 1]) is not null)
        {
            initialPageId = args[pageArgIdx + 1];
        }

        NavigateTo(initialPageId);

        // --msgbox: Show WorkbenchMessageBox immediately on startup (for visual inspection and screenshots)
        if (args.Contains("--msgbox"))
        {
            Loaded += (_, _) => Dispatcher.BeginInvoke(() => WorkbenchMessageBox.Show(
                this,
                "The model has been modified. Do you want to save?",
                "Confirmation",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question));
        }

        // --dockshell: Open the docking shell demo immediately on startup (for visual inspection and screenshots)
        if (args.Contains("--dockshell"))
        {
            Loaded += (_, _) => Dispatcher.BeginInvoke(LaunchDockingShell);
        }

        // --smoke: Instantiate every page once to catch XAML/resource resolution errors and exit immediately
        if (args.Contains("--smoke"))
        {
            foreach (var descriptor in GalleryCatalog.AllNavigablePages)
            {
                _ = descriptor.CreatePage(this);
            }

            _ = new DockingShellWindow();
            Application.Current.Shutdown(0);
        }
    }

    public void NavigateTo(string pageId)
    {
        var descriptor = GalleryCatalog.FindById(pageId) ?? GalleryCatalog.HomeDescriptor;

        if (_navItemsById.TryGetValue(descriptor.Id, out var navItem))
        {
            if (_parentGroupsById.TryGetValue(descriptor.Id, out var parentGroup) && RootNav.IsPaneOpen)
            {
                parentGroup.IsExpanded = true;
            }

            if (!ReferenceEquals(RootNav.SelectedItem, navItem))
            {
                _suppressNavSelection = true;
                try
                {
                    RootNav.SelectedItem = navItem;
                }
                finally
                {
                    _suppressNavSelection = false;
                }
            }
        }

        PageHost.Padding = descriptor.IsFullBleed
            ? new Thickness(0)
            : new Thickness(24, 18, 24, 24);
        PageHost.Content = descriptor.CreatePage(this);
    }

    public void LaunchDockingShell() =>
        new DockingShellWindow { Owner = this }.Show();

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (!e.Handled &&
            e.Key == Key.K &&
            Keyboard.Modifiers == ModifierKeys.Control)
        {
            var innerTextBox = FindVisualChild<TextBox>(GlobalSearchBox);
            if (innerTextBox is not null)
            {
                innerTextBox.Focus();
                innerTextBox.SelectAll();
            }
            else
            {
                GlobalSearchBox.Focus();
            }

            e.Handled = true;
        }
    }

    private void BuildNavigationTree()
    {
        var homeItem = new NavigationViewItem
        {
            Content = GalleryCatalog.HomeDescriptor.Title,
            Icon = GalleryCatalog.HomeDescriptor.Icon,
            Tag = GalleryCatalog.HomePageId,
        };
        RootNav.MenuItems.Add(homeItem);
        _navItemsById[GalleryCatalog.HomePageId] = homeItem;
        RootNav.MenuItems.Add(new NavigationViewItemSeparator());

        foreach (var category in GalleryCatalog.Categories)
        {
            var categoryItem = new NavigationViewItem
            {
                Content = category.Title,
                Icon = category.Icon,
                SelectsOnInvoked = false,
                IsExpanded = category.IsExpandedByDefault,
            };

            foreach (var page in GalleryCatalog.Pages.Where(p => p.CategoryId == category.Id))
            {
                var childItem = new NavigationViewItem
                {
                    Content = page.Title,
                    Icon = page.Icon,
                    Tag = page.Id,
                    Badge = page.Badge,
                };
                categoryItem.MenuItems.Add(childItem);
                _navItemsById[page.Id] = childItem;
                _parentGroupsById[page.Id] = categoryItem;
            }

            RootNav.MenuItems.Add(categoryItem);
        }

        var settingsItem = new NavigationViewItem
        {
            Content = GalleryCatalog.SettingsDescriptor.Title,
            Icon = GalleryCatalog.SettingsDescriptor.Icon,
            Tag = GalleryCatalog.SettingsPageId,
        };
        RootNav.FooterMenuItems.Add(settingsItem);
        _navItemsById[GalleryCatalog.SettingsPageId] = settingsItem;
    }

    private void BuildAccentPopover()
    {
        AccentPresetsHost.Children.Clear();
        _accentRows.Clear();

        foreach (var preset in GalleryAccentPresets.Presets)
        {
            var swatch = new Border
            {
                Width = 14,
                Height = 14,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(preset.PreviewColor),
                VerticalAlignment = VerticalAlignment.Center,
            };
            swatch.SetResourceReference(Border.BorderBrushProperty, "Wb.Brush.Border.Default");
            swatch.BorderThickness = new Thickness(1);

            var label = new TextBlock
            {
                Text = preset.Name,
                Margin = new Thickness(8, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };

            var checkIcon = new WorkbenchIcon
            {
                Icon = WorkbenchIcons.Check,
                Size = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed,
            };
            checkIcon.SetResourceReference(ForegroundProperty, "Wb.Brush.Accent.Default");

            var rowGrid = new Grid();
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(swatch, 0);
            Grid.SetColumn(label, 1);
            Grid.SetColumn(checkIcon, 2);
            rowGrid.Children.Add(swatch);
            rowGrid.Children.Add(label);
            rowGrid.Children.Add(checkIcon);

            var button = new Button
            {
                Content = rowGrid,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 1, 0, 1),
            };
            button.SetResourceReference(StyleProperty, "Wb.Button.Subtle");
            button.Click += (_, _) =>
            {
                GalleryAccentPresets.Apply(preset);
                AccentPopup.IsOpen = false;
            };

            AccentPresetsHost.Children.Add(button);
            _accentRows.Add((preset, checkIcon));
        }
    }

    private void RootNav_SelectionChanged(object sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (_suppressNavSelection || !ReferenceEquals(e.OriginalSource, RootNav))
        {
            return;
        }

        if (e.SelectedItemContainer?.Tag is string pageId)
        {
            NavigateTo(pageId);
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = GlobalSearchBox.Text?.Trim();
        if (string.IsNullOrEmpty(query))
        {
            SearchResultsPopup.IsOpen = false;
            SearchResultsList.ItemsSource = null;
            return;
        }

        var results = GalleryCatalog.Search(query);
        SearchResultsList.ItemsSource = results;
        SearchResultsList.Visibility = results.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        SearchEmptyText.Visibility = results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (results.Count > 0)
        {
            SearchResultsList.SelectedIndex = 0;
        }

        SearchResultsPopup.IsOpen = true;
    }

    private void GlobalSearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!SearchResultsPopup.IsOpen || SearchResultsList.Items.Count == 0)
        {
            if (e.Key == Key.Escape)
            {
                SearchResultsPopup.IsOpen = false;
            }

            return;
        }

        switch (e.Key)
        {
            case Key.Down:
                SearchResultsList.SelectedIndex = Math.Min(SearchResultsList.SelectedIndex + 1, SearchResultsList.Items.Count - 1);
                SearchResultsList.ScrollIntoView(SearchResultsList.SelectedItem);
                e.Handled = true;
                break;

            case Key.Up:
                SearchResultsList.SelectedIndex = Math.Max(SearchResultsList.SelectedIndex - 1, 0);
                SearchResultsList.ScrollIntoView(SearchResultsList.SelectedItem);
                e.Handled = true;
                break;

            case Key.Enter:
                CommitSearchSelection();
                e.Handled = true;
                break;

            case Key.Escape:
                SearchResultsPopup.IsOpen = false;
                e.Handled = true;
                break;
        }
    }

    private void SearchResultsList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
        CommitSearchSelection();

    private void SearchResultsList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitSearchSelection();
            e.Handled = true;
        }
    }

    private void CommitSearchSelection()
    {
        if (SearchResultsList.SelectedItem is not GalleryPageDescriptor descriptor)
        {
            return;
        }

        SearchResultsPopup.IsOpen = false;
        GlobalSearchBox.Clear();
        NavigateTo(descriptor.Id);
    }

    private void AccentPopoverButton_Click(object sender, RoutedEventArgs e) =>
        AccentPopup.IsOpen = !AccentPopup.IsOpen;

    private void OpenAccentSettings_Click(object sender, RoutedEventArgs e)
    {
        AccentPopup.IsOpen = false;
        NavigateTo(GalleryCatalog.SettingsPageId);
    }

    private void ThemeQuickToggleButton_Click(object sender, RoutedEventArgs e)
    {
        var next = ThemeManager.CurrentTheme == WorkbenchThemeVariant.Dark
            ? WorkbenchThemeVariant.Light
            : WorkbenchThemeVariant.Dark;
        ThemeManager.SetTheme(next);
    }

    private void OnThemeManagerChanged(object? sender, EventArgs e) =>
        UpdateThemeAndAccentVisuals();

    private void UpdateThemeAndAccentVisuals()
    {
        var isDark = ThemeManager.CurrentTheme == WorkbenchThemeVariant.Dark;
        ThemeQuickToggleIcon.Icon = isDark ? WorkbenchIcons.Sun : WorkbenchIcons.Moon;
        ThemeQuickToggleButton.ToolTip = isDark ? "Switch to Light theme" : "Switch to Dark theme";

        var currentAccent = ThemeManager.CurrentAccent;
        foreach (var (preset, checkIcon) in _accentRows)
        {
            var isMatch = preset.Color == currentAccent;
            checkIcon.Visibility = isMatch ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void OnRootNavShellPropertyChanged(object? sender, EventArgs e) =>
        ShellStateChanged?.Invoke(this, EventArgs.Empty);

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        ThemeManager.ThemeChanged -= OnThemeManagerChanged;
        _paneModeDescriptor?.RemoveValueChanged(RootNav, OnRootNavShellPropertyChanged);
        _isPaneOpenDescriptor?.RemoveValueChanged(RootNav, OnRootNavShellPropertyChanged);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed)
            {
                return typed;
            }

            var nested = FindVisualChild<T>(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
}
