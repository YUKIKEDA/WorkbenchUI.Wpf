using System.Collections.Specialized;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>Progress state of a step (used as the visual state of <see cref="StepIndicatorItem"/>).</summary>
public enum StepState
{
    /// <summary>Completed step (before the current step).</summary>
    Completed,

    /// <summary>Currently active step.</summary>
    Current,

    /// <summary>Not yet reached (after the current step).</summary>
    Upcoming,
}

/// <summary>
/// Display-only step progress indicator control.
/// Used internally by <see cref="Wizard"/> and suitable standalone for multi-stage progress displays.
/// Populate <see cref="ItemsControl.Items"/> with step headers (such as strings).
/// </summary>
public class StepIndicator : ItemsControl
{
    static StepIndicator()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(StepIndicator), new FrameworkPropertyMetadata(typeof(StepIndicator)));
    }

    /// <summary>Identifies the <see cref="CurrentIndex"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentIndexProperty = DependencyProperty.Register(
        nameof(CurrentIndex), typeof(int), typeof(StepIndicator),
        new FrameworkPropertyMetadata(0,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCurrentIndexChanged));

    /// <summary>Gets or sets the zero-based index of the current step.</summary>
    public int CurrentIndex
    {
        get => (int)GetValue(CurrentIndexProperty);
        set => SetValue(CurrentIndexProperty, value);
    }

    // Use a plain FrameworkElementAutomationPeer instead of the default ItemsControl peer so step badges and headers are exposed to UIA.
    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new FrameworkElementAutomationPeer(this);

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() => new StepIndicatorItem();

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) => item is StepIndicatorItem;

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is StepIndicatorItem container)
        {
            var index = ItemContainerGenerator.IndexFromContainer(container);
            container.SetStep(index + 1, index == 0, StateFor(index));
        }
    }

    /// <inheritdoc />
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        UpdateStates();
    }

    private static void OnCurrentIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((StepIndicator)d).UpdateStates();

    private StepState StateFor(int index) =>
        index < CurrentIndex ? StepState.Completed
        : index == CurrentIndex ? StepState.Current
        : StepState.Upcoming;

    private void UpdateStates()
    {
        for (var i = 0; i < Items.Count; i++)
        {
            if (ItemContainerGenerator.ContainerFromIndex(i) is StepIndicatorItem container)
            {
                container.SetStep(i + 1, i == 0, StateFor(i));
            }
        }
    }
}

/// <summary>
/// Item container for <see cref="StepIndicator"/>, representing a single step with a numbered badge, header, and connector line.
/// </summary>
public class StepIndicatorItem : ContentControl
{
    static StepIndicatorItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(StepIndicatorItem), new FrameworkPropertyMetadata(typeof(StepIndicatorItem)));
    }

    private static readonly DependencyPropertyKey StepNumberPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(StepNumber), typeof(int), typeof(StepIndicatorItem), new PropertyMetadata(1));

    /// <summary>Identifies the <see cref="StepNumber"/> dependency property.</summary>
    public static readonly DependencyProperty StepNumberProperty = StepNumberPropertyKey.DependencyProperty;

    /// <summary>Gets the one-based step number.</summary>
    public int StepNumber => (int)GetValue(StepNumberProperty);

    private static readonly DependencyPropertyKey IsFirstPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsFirst), typeof(bool), typeof(StepIndicatorItem), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsFirst"/> dependency property.</summary>
    public static readonly DependencyProperty IsFirstProperty = IsFirstPropertyKey.DependencyProperty;

    /// <summary>Gets a value indicating whether this is the first step (used to hide the leading connector line).</summary>
    public bool IsFirst => (bool)GetValue(IsFirstProperty);

    private static readonly DependencyPropertyKey StatePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(State), typeof(StepState), typeof(StepIndicatorItem), new PropertyMetadata(StepState.Upcoming));

    /// <summary>Identifies the <see cref="State"/> dependency property.</summary>
    public static readonly DependencyProperty StateProperty = StatePropertyKey.DependencyProperty;

    /// <summary>Gets the progress state of this step.</summary>
    public StepState State => (StepState)GetValue(StateProperty);

    internal void SetStep(int number, bool isFirst, StepState state)
    {
        SetValue(StepNumberPropertyKey, number);
        SetValue(IsFirstPropertyKey, isFirst);
        SetValue(StatePropertyKey, state);
    }
}
