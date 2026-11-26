using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Playback transport bar for stepping through animation or simulation result frames.
/// <list type="bullet">
/// <item>Uses an index-based frame model: <see cref="FrameCount"/> and <see cref="CurrentFrame"/> (two-way by default).</item>
/// <item>Includes a built-in playback timer (<see cref="IsPlaying"/>, <see cref="FramesPerSecond"/>, <see cref="IsLooping"/>); applications driving playback externally can update <see cref="CurrentFrame"/> directly.</item>
/// <item>Focuses purely on playback transport UI, leaving 3D or canvas rendering to the application.</item>
/// <item>Keyboard shortcuts: Space (play/pause), Left/Right (step), Home/End (first/last frame).</item>
/// </list>
/// </summary>
public class PlaybackBar : Control
{
    private readonly DispatcherTimer _timer;

    static PlaybackBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(PlaybackBar), new FrameworkPropertyMetadata(typeof(PlaybackBar)));

        // Handle clicks from step buttons (PART_StepBackButton / PART_StepForwardButton) in the template
        EventManager.RegisterClassHandler(
            typeof(PlaybackBar), ButtonBase.ClickEvent, new RoutedEventHandler(OnButtonClick));
    }

    /// <summary>Initializes a new instance of the <see cref="PlaybackBar"/> class.</summary>
    public PlaybackBar()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = IntervalFor(10.0) };
        _timer.Tick += OnTimerTick;

        // Stop the timer while unloaded to prevent leaks (retain IsPlaying so playback resumes on reload)
        Loaded += (_, _) => UpdateTimer();
        Unloaded += (_, _) => _timer.Stop();
    }

    #region Dependency properties

    /// <summary>Identifies the <see cref="FrameCount"/> dependency property.</summary>
    public static readonly DependencyProperty FrameCountProperty = DependencyProperty.Register(
        nameof(FrameCount), typeof(int), typeof(PlaybackBar),
        new PropertyMetadata(0, OnFrameCountChanged), v => (int)v >= 0);

    /// <summary>Gets or sets the total number of frames. When 0, the control appears disabled.</summary>
    public int FrameCount
    {
        get => (int)GetValue(FrameCountProperty);
        set => SetValue(FrameCountProperty, value);
    }

    /// <summary>Identifies the <see cref="CurrentFrame"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentFrameProperty = DependencyProperty.Register(
        nameof(CurrentFrame), typeof(int), typeof(PlaybackBar),
        new FrameworkPropertyMetadata(0,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnCurrentFrameChanged, CoerceCurrentFrame));

    /// <summary>Gets or sets the zero-based current frame index (coerced to <c>0..FrameCount-1</c>).</summary>
    public int CurrentFrame
    {
        get => (int)GetValue(CurrentFrameProperty);
        set => SetValue(CurrentFrameProperty, value);
    }

    /// <summary>Identifies the <see cref="IsPlaying"/> dependency property.</summary>
    public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(
        nameof(IsPlaying), typeof(bool), typeof(PlaybackBar),
        new FrameworkPropertyMetadata(false,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsPlayingChanged));

    /// <summary>Gets or sets whether playback is active. While <see langword="true"/>, the internal timer advances <see cref="CurrentFrame"/>.</summary>
    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    /// <summary>Identifies the <see cref="FramesPerSecond"/> dependency property.</summary>
    public static readonly DependencyProperty FramesPerSecondProperty = DependencyProperty.Register(
        nameof(FramesPerSecond), typeof(double), typeof(PlaybackBar),
        new PropertyMetadata(10.0, OnFramesPerSecondChanged),
        v => (double)v > 0 && !double.IsInfinity((double)v));

    /// <summary>Gets or sets the playback rate in frames per second (speed selection UI is left to the host application).</summary>
    public double FramesPerSecond
    {
        get => (double)GetValue(FramesPerSecondProperty);
        set => SetValue(FramesPerSecondProperty, value);
    }

    /// <summary>Identifies the <see cref="IsLooping"/> dependency property.</summary>
    public static readonly DependencyProperty IsLoopingProperty = DependencyProperty.Register(
        nameof(IsLooping), typeof(bool), typeof(PlaybackBar), new PropertyMetadata(true));

    /// <summary>Gets or sets whether playback wraps back to the first frame at the end (defaults to <see langword="true"/>). When <see langword="false"/>, playback stops at the last frame.</summary>
    public bool IsLooping
    {
        get => (bool)GetValue(IsLoopingProperty);
        set => SetValue(IsLoopingProperty, value);
    }

    /// <summary>Identifies the <see cref="FrameLabel"/> dependency property.</summary>
    public static readonly DependencyProperty FrameLabelProperty = DependencyProperty.Register(
        nameof(FrameLabel), typeof(string), typeof(PlaybackBar), new PropertyMetadata(null));

    /// <summary>Gets or sets the descriptive label for the current frame (e.g., "t = 0.24 s"); formatting is supplied by the application.</summary>
    public string? FrameLabel
    {
        get => (string?)GetValue(FrameLabelProperty);
        set => SetValue(FrameLabelProperty, value);
    }

    private static readonly DependencyPropertyKey MaxFrameIndexPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(MaxFrameIndex), typeof(int), typeof(PlaybackBar), new PropertyMetadata(0));

    /// <summary>Identifies the <see cref="MaxFrameIndex"/> dependency property.</summary>
    public static readonly DependencyProperty MaxFrameIndexProperty = MaxFrameIndexPropertyKey.DependencyProperty;

    /// <summary>Gets the maximum frame index (<c>FrameCount - 1</c>) for slider binding (read-only, used by the template).</summary>
    public int MaxFrameIndex => (int)GetValue(MaxFrameIndexProperty);

    private static readonly DependencyPropertyKey PositionTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(PositionText), typeof(string), typeof(PlaybackBar), new PropertyMetadata("0 / 0"));

    /// <summary>Identifies the <see cref="PositionText"/> dependency property.</summary>
    public static readonly DependencyProperty PositionTextProperty = PositionTextPropertyKey.DependencyProperty;

    /// <summary>Gets the one-based position text formatted as <c>"13 / 50"</c> (read-only, used by the template).</summary>
    public string PositionText => (string)GetValue(PositionTextProperty);

    #endregion

    /// <summary>Identifies the <see cref="CurrentFrameChanged"/> routed event.</summary>
    public static readonly RoutedEvent CurrentFrameChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(CurrentFrameChanged), RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventHandler<int>), typeof(PlaybackBar));

    /// <summary>Occurs when <see cref="CurrentFrame"/> changes (useful for updating 3D or canvas views in code-behind).</summary>
    public event RoutedPropertyChangedEventHandler<int> CurrentFrameChanged
    {
        add => AddHandler(CurrentFrameChangedEvent, value);
        remove => RemoveHandler(CurrentFrameChangedEvent, value);
    }

    /// <summary>Advances playback by one frame, wrapping if <see cref="IsLooping"/> is enabled.</summary>
    public void StepForward()
    {
        if (FrameCount <= 0)
        {
            return;
        }

        var next = CurrentFrame + 1;
        SetCurrentValue(CurrentFrameProperty, next >= FrameCount && IsLooping ? 0 : next);
    }

    /// <summary>Moves playback back by one frame, wrapping if <see cref="IsLooping"/> is enabled.</summary>
    public void StepBackward()
    {
        if (FrameCount <= 0)
        {
            return;
        }

        var prev = CurrentFrame - 1;
        SetCurrentValue(CurrentFrameProperty, prev < 0 && IsLooping ? FrameCount - 1 : prev);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Space:
                SetCurrentValue(IsPlayingProperty, !IsPlaying);
                e.Handled = true;
                break;
            case Key.Left:
                StepBackward();
                e.Handled = true;
                break;
            case Key.Right:
                StepForward();
                e.Handled = true;
                break;
            case Key.Home:
                SetCurrentValue(CurrentFrameProperty, 0);
                e.Handled = true;
                break;
            case Key.End:
                SetCurrentValue(CurrentFrameProperty, Math.Max(0, FrameCount - 1));
                e.Handled = true;
                break;
        }
    }

    private static void OnButtonClick(object sender, RoutedEventArgs e)
    {
        var bar = (PlaybackBar)sender;
        switch ((e.OriginalSource as FrameworkElement)?.Name)
        {
            case "PART_StepBackButton":
                e.Handled = true;
                bar.StepBackward();
                break;
            case "PART_StepForwardButton":
                e.Handled = true;
                bar.StepForward();
                break;
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (FrameCount <= 0)
        {
            return;
        }

        var next = CurrentFrame + 1;
        if (next >= FrameCount)
        {
            if (IsLooping)
            {
                SetCurrentValue(CurrentFrameProperty, 0);
            }
            else
            {
                // Automatically stop at the last frame while keeping the position at the end
                SetCurrentValue(IsPlayingProperty, false);
            }

            return;
        }

        SetCurrentValue(CurrentFrameProperty, next);
    }

    private static void OnFrameCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var bar = (PlaybackBar)d;
        bar.SetValue(MaxFrameIndexPropertyKey, Math.Max(0, (int)e.NewValue - 1));
        bar.CoerceValue(CurrentFrameProperty);
        bar.UpdatePositionText();
    }

    private static object CoerceCurrentFrame(DependencyObject d, object baseValue)
    {
        var bar = (PlaybackBar)d;
        return Math.Clamp((int)baseValue, 0, Math.Max(0, bar.FrameCount - 1));
    }

    private static void OnCurrentFrameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var bar = (PlaybackBar)d;
        bar.UpdatePositionText();
        bar.RaiseEvent(new RoutedPropertyChangedEventArgs<int>(
            (int)e.OldValue, (int)e.NewValue, CurrentFrameChangedEvent));
    }

    private static void OnIsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((PlaybackBar)d).UpdateTimer();

    private static void OnFramesPerSecondChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((PlaybackBar)d)._timer.Interval = IntervalFor((double)e.NewValue);

    private static TimeSpan IntervalFor(double fps) => TimeSpan.FromSeconds(1.0 / fps);

    private void UpdateTimer()
    {
        if (IsPlaying && IsLoaded)
        {
            _timer.Start();
        }
        else
        {
            _timer.Stop();
        }
    }

    private void UpdatePositionText() =>
        SetValue(PositionTextPropertyKey, FrameCount > 0
            ? $"{CurrentFrame + 1} / {FrameCount}"
            : "0 / 0");
}
