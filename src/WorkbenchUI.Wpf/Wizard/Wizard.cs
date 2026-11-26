using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>Provides data for the cancellable <see cref="Wizard.Navigating"/> event.</summary>
public class WizardNavigatingEventArgs(RoutedEvent routedEvent, object source, int fromIndex, int toIndex)
    : RoutedEventArgs(routedEvent, source)
{
    /// <summary>Gets the zero-based index of the step being navigated away from.</summary>
    public int FromIndex { get; } = fromIndex;

    /// <summary>Gets the zero-based index of the destination step.</summary>
    public int ToIndex { get; } = toIndex;

    /// <summary>Gets or sets a value indicating whether navigation should be cancelled.</summary>
    public bool Cancel { get; set; }
}

/// <summary>Represents the method that handles the <see cref="Wizard.Navigating"/> routed event.</summary>
public delegate void WizardNavigatingEventHandler(object sender, WizardNavigatingEventArgs e);

/// <summary>
/// Multi-step wizard page host control.
/// Arranges <see cref="WizardStep"/> items with built-in Back, Next, Finish, and Cancel navigation buttons
/// and a <see cref="StepIndicator"/> header. Typically hosted inside a <see cref="WorkbenchDialogWindow"/>.
/// <list type="bullet">
/// <item>Forward progression can be gated declaratively via <see cref="CanGoNext"/> / <see cref="CanFinish"/> or validated imperatively via the cancellable <see cref="Navigating"/> event.</item>
/// <item>The host application handles <see cref="Finished"/> and <see cref="Cancelled"/> to close the containing dialog.</item>
/// <item>Button labels default to English and can be customized via dependency properties.</item>
/// </list>
/// </summary>
public class Wizard : ItemsControl
{
    private const string PartBackButton = "PART_BackButton";
    private const string PartNextButton = "PART_NextButton";
    private const string PartFinishButton = "PART_FinishButton";
    private const string PartCancelButton = "PART_CancelButton";

    static Wizard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(Wizard), new FrameworkPropertyMetadata(typeof(Wizard)));

        EventManager.RegisterClassHandler(
            typeof(Wizard), ButtonBase.ClickEvent, new RoutedEventHandler(OnButtonClick));
    }

    #region Dependency properties

    /// <summary>Identifies the <see cref="CurrentIndex"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentIndexProperty = DependencyProperty.Register(
        nameof(CurrentIndex), typeof(int), typeof(Wizard),
        new FrameworkPropertyMetadata(0,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnCurrentIndexChanged, CoerceCurrentIndex));

    /// <summary>Gets or sets the zero-based index of the active step (binds two-way by default; coerced to the valid step range).</summary>
    public int CurrentIndex
    {
        get => (int)GetValue(CurrentIndexProperty);
        set => SetValue(CurrentIndexProperty, value);
    }

    /// <summary>Identifies the <see cref="CanGoNext"/> dependency property.</summary>
    public static readonly DependencyProperty CanGoNextProperty = DependencyProperty.Register(
        nameof(CanGoNext), typeof(bool), typeof(Wizard), new PropertyMetadata(true));

    /// <summary>Gets or sets a value indicating whether the Next button is enabled (typically bound to current step validation).</summary>
    public bool CanGoNext
    {
        get => (bool)GetValue(CanGoNextProperty);
        set => SetValue(CanGoNextProperty, value);
    }

    /// <summary>Identifies the <see cref="CanFinish"/> dependency property.</summary>
    public static readonly DependencyProperty CanFinishProperty = DependencyProperty.Register(
        nameof(CanFinish), typeof(bool), typeof(Wizard), new PropertyMetadata(true));

    /// <summary>Gets or sets a value indicating whether the Finish button is enabled.</summary>
    public bool CanFinish
    {
        get => (bool)GetValue(CanFinishProperty);
        set => SetValue(CanFinishProperty, value);
    }

    /// <summary>Identifies the <see cref="BackButtonText"/> dependency property.</summary>
    public static readonly DependencyProperty BackButtonTextProperty = DependencyProperty.Register(
        nameof(BackButtonText), typeof(string), typeof(Wizard), new PropertyMetadata("Back"));

    /// <summary>Gets or sets the label displayed on the Back button.</summary>
    public string BackButtonText
    {
        get => (string)GetValue(BackButtonTextProperty);
        set => SetValue(BackButtonTextProperty, value);
    }

    /// <summary>Identifies the <see cref="NextButtonText"/> dependency property.</summary>
    public static readonly DependencyProperty NextButtonTextProperty = DependencyProperty.Register(
        nameof(NextButtonText), typeof(string), typeof(Wizard), new PropertyMetadata("Next"));

    /// <summary>Gets or sets the label displayed on the Next button.</summary>
    public string NextButtonText
    {
        get => (string)GetValue(NextButtonTextProperty);
        set => SetValue(NextButtonTextProperty, value);
    }

    /// <summary>Identifies the <see cref="FinishButtonText"/> dependency property.</summary>
    public static readonly DependencyProperty FinishButtonTextProperty = DependencyProperty.Register(
        nameof(FinishButtonText), typeof(string), typeof(Wizard), new PropertyMetadata("Finish"));

    /// <summary>Gets or sets the label displayed on the Finish button.</summary>
    public string FinishButtonText
    {
        get => (string)GetValue(FinishButtonTextProperty);
        set => SetValue(FinishButtonTextProperty, value);
    }

    /// <summary>Identifies the <see cref="CancelButtonText"/> dependency property.</summary>
    public static readonly DependencyProperty CancelButtonTextProperty = DependencyProperty.Register(
        nameof(CancelButtonText), typeof(string), typeof(Wizard), new PropertyMetadata("Cancel"));

    /// <summary>Gets or sets the label displayed on the Cancel button.</summary>
    public string CancelButtonText
    {
        get => (string)GetValue(CancelButtonTextProperty);
        set => SetValue(CancelButtonTextProperty, value);
    }

    private static readonly DependencyPropertyKey IsFirstStepPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsFirstStep), typeof(bool), typeof(Wizard), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="IsFirstStep"/> dependency property.</summary>
    public static readonly DependencyProperty IsFirstStepProperty = IsFirstStepPropertyKey.DependencyProperty;

    /// <summary>Gets a value indicating whether the current step is the first step (disables the Back button).</summary>
    public bool IsFirstStep => (bool)GetValue(IsFirstStepProperty);

    private static readonly DependencyPropertyKey IsLastStepPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsLastStep), typeof(bool), typeof(Wizard), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="IsLastStep"/> dependency property.</summary>
    public static readonly DependencyProperty IsLastStepProperty = IsLastStepPropertyKey.DependencyProperty;

    /// <summary>Gets a value indicating whether the current step is the last step (switches Next to Finish).</summary>
    public bool IsLastStep => (bool)GetValue(IsLastStepProperty);

    private static readonly DependencyPropertyKey StepHeadersPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(StepHeaders), typeof(ReadOnlyCollection<object?>), typeof(Wizard), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="StepHeaders"/> dependency property.</summary>
    public static readonly DependencyProperty StepHeadersProperty = StepHeadersPropertyKey.DependencyProperty;

    /// <summary>Gets the read-only list of step headers consumed by the template's <see cref="StepIndicator"/>.</summary>
    public ReadOnlyCollection<object?>? StepHeaders =>
        (ReadOnlyCollection<object?>?)GetValue(StepHeadersProperty);

    #endregion

    #region Events

    /// <summary>Identifies the <see cref="Navigating"/> routed event.</summary>
    public static readonly RoutedEvent NavigatingEvent = EventManager.RegisterRoutedEvent(
        nameof(Navigating), RoutingStrategy.Bubble, typeof(WizardNavigatingEventHandler), typeof(Wizard));

    /// <summary>Occurs before navigating to another step via Back or Next; set <see cref="WizardNavigatingEventArgs.Cancel"/> to <see langword="true"/> to abort.</summary>
    public event WizardNavigatingEventHandler Navigating
    {
        add => AddHandler(NavigatingEvent, value);
        remove => RemoveHandler(NavigatingEvent, value);
    }

    /// <summary>Identifies the <see cref="Finished"/> routed event.</summary>
    public static readonly RoutedEvent FinishedEvent = EventManager.RegisterRoutedEvent(
        nameof(Finished), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Wizard));

    /// <summary>Occurs when the Finish button is clicked. Closing the host dialog is the application's responsibility.</summary>
    public event RoutedEventHandler Finished
    {
        add => AddHandler(FinishedEvent, value);
        remove => RemoveHandler(FinishedEvent, value);
    }

    /// <summary>Identifies the <see cref="Cancelled"/> routed event.</summary>
    public static readonly RoutedEvent CancelledEvent = EventManager.RegisterRoutedEvent(
        nameof(Cancelled), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Wizard));

    /// <summary>Occurs when the Cancel button is clicked.</summary>
    public event RoutedEventHandler Cancelled
    {
        add => AddHandler(CancelledEvent, value);
        remove => RemoveHandler(CancelledEvent, value);
    }

    #endregion

    // Replace the default ItemsControl peer, which exposes only items and hides navigation buttons and StepIndicator from UIA.
    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new FrameworkElementAutomationPeer(this);

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() => new WizardStep();

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) => item is WizardStep;

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is WizardStep step)
        {
            var index = ItemContainerGenerator.IndexFromContainer(step);
            step.Visibility = index == CurrentIndex ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <inheritdoc />
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        CoerceValue(CurrentIndexProperty);
        UpdateState();
    }

    /// <summary>Programmatically advances to the next step after raising <see cref="Navigating"/> for validation.</summary>
    public bool GoNext() => TryNavigate(CurrentIndex + 1);

    /// <summary>Programmatically returns to the previous step after raising <see cref="Navigating"/> for validation.</summary>
    public bool GoBack() => TryNavigate(CurrentIndex - 1);

    private bool TryNavigate(int toIndex)
    {
        if (toIndex < 0 || toIndex >= Items.Count)
        {
            return false;
        }

        var args = new WizardNavigatingEventArgs(NavigatingEvent, this, CurrentIndex, toIndex);
        RaiseEvent(args);
        if (args.Cancel)
        {
            return false;
        }

        SetCurrentValue(CurrentIndexProperty, toIndex);
        return true;
    }

    private static void OnButtonClick(object sender, RoutedEventArgs e)
    {
        var wizard = (Wizard)sender;
        switch ((e.OriginalSource as FrameworkElement)?.Name)
        {
            case PartBackButton:
                e.Handled = true;
                wizard.GoBack();
                break;
            case PartNextButton:
                e.Handled = true;
                if (wizard.CanGoNext)
                {
                    wizard.GoNext();
                }

                break;
            case PartFinishButton:
                e.Handled = true;
                if (wizard.CanFinish)
                {
                    wizard.RaiseEvent(new RoutedEventArgs(FinishedEvent, wizard));
                }

                break;
            case PartCancelButton:
                e.Handled = true;
                wizard.RaiseEvent(new RoutedEventArgs(CancelledEvent, wizard));
                break;
        }
    }

    private static object CoerceCurrentIndex(DependencyObject d, object baseValue)
    {
        var wizard = (Wizard)d;
        return Math.Clamp((int)baseValue, 0, Math.Max(0, wizard.Items.Count - 1));
    }

    private static void OnCurrentIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((Wizard)d).UpdateState();

    private void UpdateState()
    {
        var count = Items.Count;
        var current = CurrentIndex;

        SetValue(IsFirstStepPropertyKey, current <= 0);
        SetValue(IsLastStepPropertyKey, current >= count - 1);
        SetValue(StepHeadersPropertyKey, new ReadOnlyCollection<object?>(
            [.. Items.Cast<object?>().Select(i => i is HeaderedContentControl h ? h.Header : i)]));

        for (var i = 0; i < count; i++)
        {
            if (ItemContainerGenerator.ContainerFromIndex(i) is WizardStep step)
            {
                step.Visibility = i == current ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
