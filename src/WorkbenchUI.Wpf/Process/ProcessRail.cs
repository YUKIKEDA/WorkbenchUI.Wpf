using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Workflow process rail control. Completed or available chapters can be selected by clicking;
/// <see cref="ProcessChapterState.Locked"/> chapters cannot be selected.
/// </summary>
public class ProcessRail : Selector
{
    static ProcessRail()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ProcessRail), new FrameworkPropertyMetadata(typeof(ProcessRail)));
    }

    /// <summary>Identifies the <see cref="ShowConnectors"/> dependency property.</summary>
    public static readonly DependencyProperty ShowConnectorsProperty = DependencyProperty.Register(
        nameof(ShowConnectors), typeof(bool), typeof(ProcessRail), new PropertyMetadata(true));

    /// <summary>Gets or sets a value indicating whether connector lines are drawn between chapters.</summary>
    public bool ShowConnectors
    {
        get => (bool)GetValue(ShowConnectorsProperty);
        set => SetValue(ShowConnectorsProperty, value);
    }

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() => new ProcessRailItem();

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) => item is ProcessRailItem;

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (item is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged -= OnStepPropertyChanged;
            npc.PropertyChanged += OnStepPropertyChanged;
        }

        if (element is ProcessRailItem container)
        {
            container.ClickSelected -= OnItemClickSelected;
            container.ClickSelected += OnItemClickSelected;
            var index = ItemContainerGenerator.IndexFromContainer(container);
            container.ApplyVisualState(index, index == 0, ResolveVisualState(item, index));
        }
    }

    /// <inheritdoc />
    protected override void ClearContainerForItemOverride(DependencyObject element, object item)
    {
        if (item is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged -= OnStepPropertyChanged;
        }

        if (element is ProcessRailItem container)
        {
            container.ClickSelected -= OnItemClickSelected;
        }

        base.ClearContainerForItemOverride(element, item);
    }

    private void OnStepPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or nameof(IProcessRailStep.State) or "State" or "Title")
        {
            RefreshContainers();
        }
    }

    /// <inheritdoc />
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        RefreshContainers();
    }

    /// <inheritdoc />
    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectionChanged(e);
        RefreshContainers();
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new FrameworkElementAutomationPeer(this);

    private void OnItemClickSelected(ProcessRailItem item)
    {
        var index = ItemContainerGenerator.IndexFromContainer(item);
        if (index < 0)
        {
            return;
        }

        var sourceState = Items[index] switch
        {
            IProcessRailStep step => step.State,
            ProcessRailItem railItem => railItem.State,
            _ => ProcessChapterState.Available,
        };
        if (sourceState is ProcessChapterState.Locked)
        {
            return;
        }

        SelectedIndex = index;
    }

    private ProcessChapterState ResolveVisualState(object item, int index)
    {
        var baseState = item switch
        {
            ProcessRailItem own => own.State,
            IProcessRailStep step => step.State,
            _ => ProcessChapterState.Available,
        };

        if (baseState is ProcessChapterState.Locked)
        {
            return ProcessChapterState.Locked;
        }

        if (index == SelectedIndex)
        {
            return ProcessChapterState.Current;
        }

        return baseState is ProcessChapterState.Current
            ? ProcessChapterState.Available
            : baseState;
    }

    private void RefreshContainers()
    {
        for (var i = 0; i < Items.Count; i++)
        {
            if (ItemContainerGenerator.ContainerFromIndex(i) is not ProcessRailItem container)
            {
                continue;
            }

            container.ClickSelected -= OnItemClickSelected;
            container.ClickSelected += OnItemClickSelected;
            container.ApplyVisualState(i, i == 0, ResolveVisualState(Items[i]!, i));
        }
    }
}

/// <summary>Contract for bindable items in a <see cref="ProcessRail"/>.</summary>
public interface IProcessRailStep
{
    /// <summary>Gets the chapter state.</summary>
    ProcessChapterState State { get; }
}

/// <summary>Item container for <see cref="ProcessRail"/>.</summary>
public class ProcessRailItem : ContentControl
{
    static ProcessRailItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ProcessRailItem), new FrameworkPropertyMetadata(typeof(ProcessRailItem)));
    }

    /// <summary>Identifies the <see cref="State"/> dependency property.</summary>
    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
        nameof(State), typeof(ProcessChapterState), typeof(ProcessRailItem),
        new PropertyMetadata(ProcessChapterState.Available));

    /// <summary>Gets or sets the displayed chapter state (the rail overrides the selected item to <see cref="ProcessChapterState.Current"/>).</summary>
    public ProcessChapterState State
    {
        get => (ProcessChapterState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    private static readonly DependencyPropertyKey StepNumberPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(StepNumber), typeof(int), typeof(ProcessRailItem), new PropertyMetadata(1));

    /// <summary>Identifies the <see cref="StepNumber"/> dependency property.</summary>
    public static readonly DependencyProperty StepNumberProperty = StepNumberPropertyKey.DependencyProperty;

    /// <summary>Gets the one-based step number.</summary>
    public int StepNumber => (int)GetValue(StepNumberProperty);

    private static readonly DependencyPropertyKey IsFirstPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsFirst), typeof(bool), typeof(ProcessRailItem), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsFirst"/> dependency property.</summary>
    public static readonly DependencyProperty IsFirstProperty = IsFirstPropertyKey.DependencyProperty;

    /// <summary>Gets a value indicating whether this is the first item in the rail.</summary>
    public bool IsFirst => (bool)GetValue(IsFirstProperty);

    internal event Action<ProcessRailItem>? ClickSelected;

    internal void ApplyVisualState(int index, bool isFirst, ProcessChapterState visualState)
    {
        SetValue(StepNumberPropertyKey, index + 1);
        SetValue(IsFirstPropertyKey, isFirst);
        SetValue(StateProperty, visualState);
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (State is ProcessChapterState.Locked)
        {
            return;
        }

        ClickSelected?.Invoke(this);
        e.Handled = true;
    }
}
