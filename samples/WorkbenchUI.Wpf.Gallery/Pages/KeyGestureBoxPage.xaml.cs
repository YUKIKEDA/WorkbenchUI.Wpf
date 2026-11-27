using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class KeyGestureBoxPage : UserControl
{
    private readonly EventHandler _gestureChangedHandler;
    private bool _isHooked;

    public KeyGestureBoxPage()
    {
        InitializeComponent();

        _gestureChangedHandler = (_, _) => UpdateGestureInfo();
        ResetGestures();

        GestureExample.Xaml = """
            <wb:KeyGestureBox x:Name="RunGestureBox"
                              Width="260"
                              PlaceholderText="Press a key to assign..."
                              Gesture="{Binding RunShortcut, Mode=TwoWay}" />
            """;

        GestureExample.CSharp = """
            RunGestureBox.Gesture = new KeyGesture(Key.F5, ModifierKeys.Control);

            // Round-trip to/from config string
            var text = new KeyGestureConverter().ConvertToString(RunGestureBox.Gesture);
            """;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_isHooked)
        {
            DependencyPropertyDescriptor
                .FromProperty(KeyGestureBox.GestureProperty, typeof(KeyGestureBox))
                .AddValueChanged(RunGestureBox, _gestureChangedHandler);
            DependencyPropertyDescriptor
                .FromProperty(KeyGestureBox.GestureProperty, typeof(KeyGestureBox))
                .AddValueChanged(FitGestureBox, _gestureChangedHandler);
            _isHooked = true;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_isHooked)
        {
            DependencyPropertyDescriptor
                .FromProperty(KeyGestureBox.GestureProperty, typeof(KeyGestureBox))
                .RemoveValueChanged(RunGestureBox, _gestureChangedHandler);
            DependencyPropertyDescriptor
                .FromProperty(KeyGestureBox.GestureProperty, typeof(KeyGestureBox))
                .RemoveValueChanged(FitGestureBox, _gestureChangedHandler);
            _isHooked = false;
        }
    }

    private void ResetGestures()
    {
        RunGestureBox.Gesture = new KeyGesture(Key.F5, ModifierKeys.Control);
        FitGestureBox.Gesture = new KeyGesture(Key.F, ModifierKeys.None);
        UpdateGestureInfo();
    }

    private void ResetGestures_Click(object sender, RoutedEventArgs e) => ResetGestures();

    private void ClearGestures_Click(object sender, RoutedEventArgs e)
    {
        RunGestureBox.Gesture = null;
        FitGestureBox.Gesture = null;
        UpdateGestureInfo();
    }

    private void UpdateGestureInfo()
    {
        var conv = new KeyGestureConverter();
        var runStr = RunGestureBox.Gesture is { } r ? conv.ConvertToString(r) : "(Unassigned)";
        var fitStr = FitGestureBox.Gesture is { } f ? conv.ConvertToString(f) : "(Unassigned)";
        GestureInfo.Text = $"Run Solver = {runStr}   |   Fit Viewport = {fitStr}";
    }
}
