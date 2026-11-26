using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Workspace host for process chapters. Displays only the chapter at <see cref="SelectedIndex"/>.
/// </summary>
public class ChapterHost : ItemsControl
{
    static ChapterHost()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ChapterHost), new FrameworkPropertyMetadata(typeof(ChapterHost)));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChapterHost"/> class.
    /// </summary>
    public ChapterHost()
    {
        Loaded += (_, _) => UpdateVisibility();
        ItemContainerGenerator.StatusChanged += (_, _) =>
        {
            if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                UpdateVisibility();
            }
        };
    }

    /// <summary>Identifies the <see cref="SelectedIndex"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
        nameof(SelectedIndex), typeof(int), typeof(ChapterHost),
        new FrameworkPropertyMetadata(0,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedIndexChanged));

    /// <summary>Gets or sets the zero-based index of the currently visible chapter.</summary>
    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() => new ChapterHostItem();

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) => item is ChapterHostItem;

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        UpdateVisibility();
    }

    /// <inheritdoc />
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        UpdateVisibility();
    }

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((ChapterHost)d).UpdateVisibility();

    private void UpdateVisibility()
    {
        for (var i = 0; i < Items.Count; i++)
        {
            if (ItemContainerGenerator.ContainerFromIndex(i) is ChapterHostItem container)
            {
                container.Visibility = i == SelectedIndex ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (Items[i] is UIElement element)
            {
                element.Visibility = i == SelectedIndex ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}

/// <summary>Chapter container (<see cref="HeaderedContentControl"/>) hosted by <see cref="ChapterHost"/>.</summary>
public class ChapterHostItem : HeaderedContentControl
{
    static ChapterHostItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ChapterHostItem), new FrameworkPropertyMetadata(typeof(ChapterHostItem)));
    }
}
