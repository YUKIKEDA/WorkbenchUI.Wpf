using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Property grid control backed by an explicit <see cref="PropertyItem"/> model collection.
/// <list type="bullet">
/// <item>Provide a collection of <see cref="PropertyItem"/> instances to <see cref="ItemsSource"/>.</item>
/// <item>Includes collapsible category grouping and case-insensitive substring name filtering.</item>
/// <item>Displays <see cref="PropertyItem.Description"/> as a row hover tooltip.</item>
/// </list>
/// </summary>
public class PropertyGrid : Control
{
    static PropertyGrid()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(PropertyGrid), new FrameworkPropertyMetadata(typeof(PropertyGrid)));
    }

    /// <summary>Identifies the <see cref="ItemsSource"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(PropertyGrid),
        new PropertyMetadata(null, OnItemsSourceChanged));

    /// <summary>Gets or sets the collection of <see cref="PropertyItem"/> objects to display. Changes in an observable collection update rows automatically.</summary>
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Identifies the <see cref="FilterText"/> dependency property.</summary>
    public static readonly DependencyProperty FilterTextProperty = DependencyProperty.Register(
        nameof(FilterText), typeof(string), typeof(PropertyGrid),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFilterTextChanged));

    /// <summary>Gets or sets the case-insensitive substring filter applied to property names.</summary>
    public string? FilterText
    {
        get => (string?)GetValue(FilterTextProperty);
        set => SetValue(FilterTextProperty, value);
    }

    /// <summary>Identifies the <see cref="ShowFilterBox"/> dependency property.</summary>
    public static readonly DependencyProperty ShowFilterBoxProperty = DependencyProperty.Register(
        nameof(ShowFilterBox), typeof(bool), typeof(PropertyGrid), new PropertyMetadata(true));

    /// <summary>Gets or sets a value indicating whether the filter box is displayed.</summary>
    public bool ShowFilterBox
    {
        get => (bool)GetValue(ShowFilterBoxProperty);
        set => SetValue(ShowFilterBoxProperty, value);
    }

    // Built-in strings are exposed as dependency properties so callers can replace or localize them.
    /// <summary>Identifies the <see cref="FilterPlaceholder"/> dependency property.</summary>
    public static readonly DependencyProperty FilterPlaceholderProperty = DependencyProperty.Register(
        nameof(FilterPlaceholder), typeof(string), typeof(PropertyGrid), new PropertyMetadata("Filter..."));

    /// <summary>Gets or sets the placeholder text displayed in the filter box.</summary>
    public string FilterPlaceholder
    {
        get => (string)GetValue(FilterPlaceholderProperty);
        set => SetValue(FilterPlaceholderProperty, value);
    }

    private static readonly DependencyPropertyKey ItemsViewPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ItemsView), typeof(ICollectionView), typeof(PropertyGrid), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="ItemsView"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsViewProperty = ItemsViewPropertyKey.DependencyProperty;

    /// <summary>Gets the grouped and filtered collection view consumed by the control template.</summary>
    public ICollectionView? ItemsView => (ICollectionView?)GetValue(ItemsViewProperty);

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((PropertyGrid)d).CreateView();

    private static void OnFilterTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((PropertyGrid)d).ItemsView?.Refresh();

    private void CreateView()
    {
        var source = ItemsSource;
        if (source is null)
        {
            SetValue(ItemsViewPropertyKey, null);
            return;
        }

        // Create a dedicated view instead of CollectionViewSource.GetDefaultView so filtering does not leak to other controls bound to the same collection.
        var list = source as IList ?? source.Cast<object>().ToList();
        var view = new ListCollectionView(list);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PropertyItem.Category)));
        view.Filter = FilterPredicate;

        SetValue(ItemsViewPropertyKey, view);
    }

    private bool FilterPredicate(object item)
    {
        var filter = FilterText;
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        return item is PropertyItem property
            && property.Name.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
