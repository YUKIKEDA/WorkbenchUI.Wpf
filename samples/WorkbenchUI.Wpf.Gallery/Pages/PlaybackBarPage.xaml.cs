using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class PlaybackBarPage : UserControl
{
    public PlaybackBarPage()
    {
        InitializeComponent();

        ModeCanvas.SizeChanged += (_, _) => DrawModeShape(ModePlayback.CurrentFrame);
        Loaded += (_, _) => DrawModeShape(ModePlayback.CurrentFrame);

        PlaybackExample.Xaml = """
            <wb:PlaybackBar x:Name="ModePlayback"
                            FrameCount="60"
                            FramesPerSecond="30"
                            IsLooping="True"
                            CurrentFrameChanged="ModePlayback_CurrentFrameChanged" />
            """;

        PlaybackExample.CSharp = """
            private void ModePlayback_CurrentFrameChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
            {
                var phase = 2.0 * Math.PI * e.NewValue / ModePlayback.FrameCount;
                ModePlayback.FrameLabel = $"Phase {phase * 180.0 / Math.PI:F0}°";
                UpdateDeformedMesh(Math.Sin(phase));
            }
            """;

        EmptyExample.Xaml = """
            <wb:PlaybackBar Width="340" FrameCount="0" />
            """;
    }

    private void ModePlayback_CurrentFrameChanged(object sender, RoutedPropertyChangedEventArgs<int> e) =>
        DrawModeShape(e.NewValue);

    private void DrawModeShape(int frame)
    {
        var width = ModeCanvas.ActualWidth;
        var height = ModeCanvas.ActualHeight;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var frameCount = ModePlayback.FrameCount;
        var phase = frameCount > 0 ? 2.0 * Math.PI * frame / frameCount : 0.0;
        var amplitude = Math.Sin(phase);

        ModePlayback.FrameLabel = $"Phase {phase * 180.0 / Math.PI:F0}°";

        var midY = height / 2.0;
        BaseLine.X1 = 0;
        BaseLine.X2 = width;
        BaseLine.Y1 = BaseLine.Y2 = midY;

        var points = new PointCollection();
        const int segments = 48;
        for (var i = 0; i <= segments; i++)
        {
            var x = (double)i / segments;
            var y = amplitude * Math.Sin(Math.PI * x) * (height * 0.42);
            points.Add(new Point(x * width, midY - y));
        }

        points.Freeze();
        ModeShape.Points = points;
    }

    private void SpeedCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModePlayback is not null && SpeedCombo.SelectedItem is ComboBoxItem { Tag: string raw } && double.TryParse(raw, out var fps))
        {
            ModePlayback.FramesPerSecond = fps;
        }
    }
}
