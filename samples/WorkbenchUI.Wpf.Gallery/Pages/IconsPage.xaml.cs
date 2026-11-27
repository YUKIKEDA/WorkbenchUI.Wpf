using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class IconsPage : UserControl
{
    public sealed record IconItem(string Name, Geometry Geometry);

    private readonly List<IconItem> _allItems;
    private IconItem _selectedItem;

    public IconsPage()
    {
        InitializeComponent();

        _allItems = WorkbenchIcons.Names
            .Select(name => new IconItem(name, WorkbenchIcons.Get(name)))
            .ToList();
        _selectedItem = _allItems.FirstOrDefault(x => x.Name == "Settings") ?? _allItems[0];

        UsageExample.Xaml = """
            <!-- Reference directly via x:Static -->
            <wb:WorkbenchIcon Icon="{x:Static wb:WorkbenchIcons.Settings}" Size="32" />

            <!-- Or via DynamicResource key (Wb.Icon.<Name>) and semantic color -->
            <wb:WorkbenchIcon Icon="{DynamicResource Wb.Icon.Success}"
                              Size="24"
                              Foreground="{DynamicResource Wb.Brush.Success}" />
            """;

        IconFilterBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(OnFilterTextChanged));
        ApplyFilter(string.Empty);
        SelectIcon(_selectedItem);
    }

    private void OnFilterTextChanged(object sender, TextChangedEventArgs e) =>
        ApplyFilter(IconFilterBox.Text);

    private void ApplyFilter(string? query)
    {
        var q = query?.Trim();
        var filtered = string.IsNullOrEmpty(q)
            ? _allItems
            : _allItems.Where(x => x.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

        IconList.ItemsSource = filtered;
        IconCountHeader.Text = string.IsNullOrEmpty(q)
            ? $"Icon Browser ({filtered.Count})"
            : $"Icon Browser ({filtered.Count} of {_allItems.Count})";
    }

    private void IconTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: IconItem item })
        {
            SelectIcon(item);
        }
    }

    private void SelectIcon(IconItem item)
    {
        _selectedItem = item;
        SelectedIconName.Text = item.Name;
        InspectorIconLarge.Icon = item.Geometry;
        InspectorIcon16.Icon = item.Geometry;
        InspectorIcon24.Icon = item.Geometry;

        StaticSnippetText.Text = $"<wb:WorkbenchIcon Icon=\"{{x:Static wb:WorkbenchIcons.{item.Name}}}\" Size=\"16\" />";
        ResourceSnippetText.Text = $"<wb:WorkbenchIcon Icon=\"{{DynamicResource Wb.Icon.{item.Name}}}\" Size=\"16\" />";
        CopyFeedbackText.Text = string.Empty;
    }

    private void CopyStaticButton_Click(object sender, RoutedEventArgs e) =>
        CopySnippet(StaticSnippetText.Text);

    private void CopyResourceButton_Click(object sender, RoutedEventArgs e) =>
        CopySnippet(ResourceSnippetText.Text);

    private void CopySnippet(string text)
    {
        try
        {
            Clipboard.SetText(text);
            CopyFeedbackText.Text = "Copied XAML snippet to clipboard.";
        }
        catch (ExternalException)
        {
            CopyFeedbackText.Text = "Clipboard unavailable.";
        }
    }
}
