using System.Windows.Controls;
using System.Windows.Media;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class IconsPage : UserControl
{
    public sealed record IconItem(string Name, Geometry Geometry);

    public IconsPage()
    {
        InitializeComponent();

        var items = WorkbenchIcons.Names
            .Select(name => new IconItem(name, WorkbenchIcons.Get(name)))
            .ToList();
        IconList.ItemsSource = items;
        IconCountHeader.Text = $"All Icons ({items.Count})";
    }
}
