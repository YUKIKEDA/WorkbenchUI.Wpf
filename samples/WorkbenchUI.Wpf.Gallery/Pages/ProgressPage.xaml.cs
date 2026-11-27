using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class ProgressPage : UserControl
{
    private CancellationTokenSource? _jobCts;
    private DispatcherTimer? _busyTimer;

    public ProgressPage()
    {
        InitializeComponent();
        JobProgress.CancelCommand = new RelayCommand(() => _jobCts?.Cancel());
        Unloaded += OnUnloaded;

        BasicProgressExample.Xaml = """
            <ProgressBar Value="62" />
            <ProgressBar IsIndeterminate="True" />
            <wb:ProgressRing Width="32" Height="32" IsActive="True" />
            <wb:ProgressRing Width="44" Height="44" Foreground="{DynamicResource Wb.Brush.Warning}" />
            """;

        DisplayExample.Xaml = """
            <wb:ProgressDisplay x:Name="JobProgress"
                                Text="Running solver..."
                                Value="{Binding ProgressPercent}"
                                IsRunning="{Binding IsSolving}"
                                CancelCommand="{Binding CancelCommand}" />
            """;

        BusyExample.Xaml = """
            <wb:BusyOverlay IsBusy="{Binding IsMeshing}" BusyText="Generating mesh...">
                <!-- Blocked content -->
            </wb:BusyOverlay>
            """;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _jobCts?.Cancel();
        _busyTimer?.Stop();
        _busyTimer = null;
    }

    private async void StartJobButton_Click(object sender, RoutedEventArgs e)
    {
        if (_jobCts is not null)
        {
            return;
        }

        _jobCts = new CancellationTokenSource();
        var token = _jobCts.Token;

        StartJobButton.IsEnabled = false;
        JobProgress.Value = 0;
        JobProgress.Text = "Running solver...";
        JobProgress.IsRunning = true;

        try
        {
            for (var i = 1; i <= 100; i++)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(30, token);
                JobProgress.Value = i;
            }

            JobProgress.Text = "Completed";
        }
        catch (OperationCanceledException)
        {
            JobProgress.Text = "Cancelled";
        }
        finally
        {
            JobProgress.IsRunning = false;
            _jobCts.Dispose();
            _jobCts = null;
            StartJobButton.IsEnabled = true;
        }
    }

    private void StartBusy_Click(object sender, RoutedEventArgs e)
    {
        _busyTimer?.Stop();
        DefaultBusyOverlay.IsBusy = true;
        ProgressBusyOverlay.IsBusy = true;
        BusyProgress.IsRunning = true;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (ReferenceEquals(_busyTimer, timer))
            {
                _busyTimer = null;
            }

            DefaultBusyOverlay.IsBusy = false;
            ProgressBusyOverlay.IsBusy = false;
            BusyProgress.IsRunning = false;
        };
        _busyTimer = timer;
        timer.Start();
    }
}
