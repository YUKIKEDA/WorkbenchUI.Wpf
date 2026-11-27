using System.Windows;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Gallery.Controls;

/// <summary>
/// Standardized Gallery page header showing category kicker, optional badge, title,
/// description, package/namespace pills, and optional header actions.
/// </summary>
public class GalleryPageHeader : Control
{
    static GalleryPageHeader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(GalleryPageHeader),
            new FrameworkPropertyMetadata(typeof(GalleryPageHeader)));
    }

    /// <summary>Identifies the <see cref="Category"/> dependency property.</summary>
    public static readonly DependencyProperty CategoryProperty = DependencyProperty.Register(
        nameof(Category), typeof(string), typeof(GalleryPageHeader), new PropertyMetadata(null));

    /// <summary>Gets or sets the category kicker text displayed above the title.</summary>
    public string? Category
    {
        get => (string?)GetValue(CategoryProperty);
        set => SetValue(CategoryProperty, value);
    }

    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(GalleryPageHeader), new PropertyMetadata(null));

    /// <summary>Gets or sets the page title.</summary>
    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(GalleryPageHeader), new PropertyMetadata(null));

    /// <summary>Gets or sets the concise page subtitle or control description.</summary>
    public string? Description
    {
        get => (string?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>Identifies the <see cref="PackageName"/> dependency property.</summary>
    public static readonly DependencyProperty PackageNameProperty = DependencyProperty.Register(
        nameof(PackageName), typeof(string), typeof(GalleryPageHeader), new PropertyMetadata(null));

    /// <summary>Gets or sets the assembly/package name shown as a pill badge.</summary>
    public string? PackageName
    {
        get => (string?)GetValue(PackageNameProperty);
        set => SetValue(PackageNameProperty, value);
    }

    /// <summary>Identifies the <see cref="NamespaceName"/> dependency property.</summary>
    public static readonly DependencyProperty NamespaceNameProperty = DependencyProperty.Register(
        nameof(NamespaceName), typeof(string), typeof(GalleryPageHeader), new PropertyMetadata(null));

    /// <summary>Gets or sets the .NET/XAML namespace shown as a pill badge.</summary>
    public string? NamespaceName
    {
        get => (string?)GetValue(NamespaceNameProperty);
        set => SetValue(NamespaceNameProperty, value);
    }

    /// <summary>Identifies the <see cref="Badge"/> dependency property.</summary>
    public static readonly DependencyProperty BadgeProperty = DependencyProperty.Register(
        nameof(Badge), typeof(string), typeof(GalleryPageHeader), new PropertyMetadata(null));

    /// <summary>Gets or sets an optional highlight badge such as "New" or "Updated".</summary>
    public string? Badge
    {
        get => (string?)GetValue(BadgeProperty);
        set => SetValue(BadgeProperty, value);
    }

    /// <summary>Identifies the <see cref="ActionContent"/> dependency property.</summary>
    public static readonly DependencyProperty ActionContentProperty = DependencyProperty.Register(
        nameof(ActionContent), typeof(object), typeof(GalleryPageHeader), new PropertyMetadata(null));

    /// <summary>Gets or sets optional right-aligned action content in the header row.</summary>
    public object? ActionContent
    {
        get => GetValue(ActionContentProperty);
        set => SetValue(ActionContentProperty, value);
    }
}
