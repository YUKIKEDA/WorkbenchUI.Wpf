using System.Windows;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Gallery.Infrastructure;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class HomePage : UserControl
{
    private readonly IGalleryHost _host;

    public HomePage(IGalleryHost host)
    {
        _host = host;
        InitializeComponent();

        IconStatText.Text = $"{WorkbenchIcons.Names.Count} Built-in Icons";
        TotalCountHint.Text = $"Browse {GalleryCatalog.Pages.Count} component pages across {GalleryCatalog.Categories.Count} categories.";

        PopulateFeatured();
        PopulateCategories();
    }

    private void PopulateFeatured()
    {
        FeaturedPanel.Children.Clear();
        foreach (var page in GalleryCatalog.Pages.Where(p => p.IsFeatured))
        {
            FeaturedPanel.Children.Add(CreateTileCard(page));
        }
    }

    private void PopulateCategories()
    {
        CategoriesHost.Children.Clear();
        foreach (var category in GalleryCatalog.Categories)
        {
            var pages = GalleryCatalog.Pages.Where(p => p.CategoryId == category.Id).ToList();
            if (pages.Count == 0)
            {
                continue;
            }

            var headerRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 4, 0, 8),
            };

            var icon = new WorkbenchIcon
            {
                Icon = category.Icon,
                Size = 15,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
            };
            icon.SetResourceReference(ForegroundProperty, "Wb.Brush.Accent.Default");

            var title = new TextBlock
            {
                Text = category.Title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var countBadge = new Border
            {
                CornerRadius = new CornerRadius(3),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 1, 6, 1),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = pages.Count.ToString(),
                    FontSize = 10,
                },
            };
            countBadge.SetResourceReference(Border.BackgroundProperty, "Wb.Brush.Surface.Panel");
            countBadge.SetResourceReference(Border.BorderBrushProperty, "Wb.Brush.Border.Default");
            ((TextBlock)countBadge.Child).SetResourceReference(TextBlock.ForegroundProperty, "Wb.Brush.Text.Secondary");

            headerRow.Children.Add(icon);
            headerRow.Children.Add(title);
            headerRow.Children.Add(countBadge);

            var wrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 20) };
            foreach (var page in pages)
            {
                wrap.Children.Add(CreateTileCard(page));
            }

            CategoriesHost.Children.Add(headerRow);
            CategoriesHost.Children.Add(wrap);
        }
    }

    private Button CreateTileCard(GalleryPageDescriptor page)
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Top row: Icon + Title + optional Badge
        var topRow = new Grid();
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var iconBorder = new Border
        {
            Width = 26,
            Height = 26,
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new WorkbenchIcon
            {
                Icon = page.Icon,
                Size = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        iconBorder.SetResourceReference(Border.BackgroundProperty, "Wb.Brush.Surface.Window");
        ((WorkbenchIcon)iconBorder.Child).SetResourceReference(ForegroundProperty, "Wb.Brush.Accent.Default");

        var title = new TextBlock
        {
            Text = page.Title,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };

        Grid.SetColumn(iconBorder, 0);
        Grid.SetColumn(title, 1);
        topRow.Children.Add(iconBorder);
        topRow.Children.Add(title);

        if (!string.IsNullOrWhiteSpace(page.Badge))
        {
            var badge = new Border
            {
                CornerRadius = new CornerRadius(3),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5, 1, 5, 1),
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = page.Badge,
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                },
            };
            badge.SetResourceReference(Border.BackgroundProperty, "Wb.Brush.Accent.Muted");
            badge.SetResourceReference(Border.BorderBrushProperty, "Wb.Brush.Accent.Default");
            Grid.SetColumn(badge, 2);
            topRow.Children.Add(badge);
        }

        // Middle: Summary
        var summary = new TextBlock
        {
            Text = page.Summary,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxHeight = 36,
            Margin = new Thickness(0, 8, 0, 8),
        };
        summary.SetResourceReference(TextBlock.FontSizeProperty, "Wb.FontSize.S");
        summary.SetResourceReference(TextBlock.ForegroundProperty, "Wb.Brush.Text.Secondary");

        // Bottom: Package label
        var packageText = new TextBlock
        {
            Text = page.PackageName,
            FontSize = 10,
        };
        packageText.SetResourceReference(TextBlock.FontFamilyProperty, "Wb.FontFamily.Monospace");
        packageText.SetResourceReference(TextBlock.ForegroundProperty, "Wb.Brush.Text.Disabled");

        Grid.SetRow(topRow, 0);
        Grid.SetRow(summary, 1);
        Grid.SetRow(packageText, 2);
        root.Children.Add(topRow);
        root.Children.Add(summary);
        root.Children.Add(packageText);

        var button = new Button
        {
            Width = 248,
            MinHeight = 106,
            Margin = new Thickness(0, 0, 10, 10),
            Content = root,
        };
        button.SetResourceReference(StyleProperty, "Gallery.TileCardButton");
        button.Click += (_, _) => _host.NavigateTo(page.Id);
        return button;
    }

    private void ExploreTokens_Click(object sender, RoutedEventArgs e) => _host.NavigateTo("tokens");

    private void BrowseIcons_Click(object sender, RoutedEventArgs e) => _host.NavigateTo("icons");

    private void ExploreNavigationView_Click(object sender, RoutedEventArgs e) => _host.NavigateTo("navigation-view");

    private void LaunchDockingShell_Click(object sender, RoutedEventArgs e) => _host.LaunchDockingShell();
}
