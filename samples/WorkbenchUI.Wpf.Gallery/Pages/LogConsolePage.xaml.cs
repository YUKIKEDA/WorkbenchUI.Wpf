using System.Windows;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class LogConsolePage : UserControl
{
    private readonly LogBuffer _buffer = new(capacity: 5000);
    private CancellationTokenSource? _cts;

    public LogConsolePage()
    {
        InitializeComponent();
        Console.Source = _buffer;
        Progress.CancelCommand = new RelayCommand(() => _cts?.Cancel());
        _buffer.Append(LogLevel.Info, "LogConsole initialized (5,000-line ring buffer).");
        Unloaded += (_, _) => _cts?.Cancel();

        ConsoleExample.Xaml = """
            <wb:LogConsole x:Name="Console"
                           Height="340"
                           ShowTimestamps="True" />
            """;

        ConsoleExample.CSharp = """
            var buffer = new LogBuffer(capacity: 5000);
            Console.Source = buffer;

            // Safe to call from background solver threads
            buffer.Append(LogLevel.Info, "Analysis started.");
            buffer.Append(LogLevel.Warning, "Slow convergence detected.");
            """;
    }

    private async void OnStart(object sender, RoutedEventArgs e)
    {
        if (_cts is not null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        StartButton.IsEnabled = false;
        Progress.Value = 0;
        Progress.Text = "Running solver...";
        Progress.IsRunning = true;
        _buffer.Append(LogLevel.Info, "Analysis started: Static (Case 1)");

        var progress = new Progress<double>(v => Progress.Value = v);
        try
        {
            await Task.Run(() => RunSolverSimulation(progress, token), token);
            _buffer.Append(LogLevel.Info, "Analysis completed successfully.");
            Progress.Text = "Completed";
            Progress.Value = 100;
        }
        catch (OperationCanceledException)
        {
            _buffer.Append(LogLevel.Error, "Analysis cancelled by user.");
            Progress.Text = "Cancelled";
        }
        finally
        {
            Progress.IsRunning = false;
            _cts.Dispose();
            _cts = null;
            StartButton.IsEnabled = true;
        }
    }

    private void RunSolverSimulation(IProgress<double> progress, CancellationToken token)
    {
        const int iterations = 200;
        for (var i = 1; i <= iterations; i++)
        {
            token.ThrowIfCancellationRequested();
            Thread.Sleep(25);

            var residual = Math.Exp(-i / 25.0) * (1.0 + 0.1 * Math.Sin(i));
            _buffer.Append(LogLevel.Debug, $"iteration {i,4}: residual = {residual:E3}");

            if (i % 50 == 0)
            {
                _buffer.Append(LogLevel.Info, $"Saved checkpoint {i}/{iterations}");
            }

            if (i == 120)
            {
                _buffer.Append(LogLevel.Warning, "Slow convergence detected (adjusting relaxation factor)");
            }

            progress.Report(100.0 * i / iterations);
        }
    }

    private void OnBurst(object sender, RoutedEventArgs e)
    {
        _ = Task.Run(() =>
        {
            Parallel.For(0, 10000, i =>
                _buffer.Append(LogLevel.Debug, $"Burst entry {i:D5} (thread {Environment.CurrentManagedThreadId})"));
            _buffer.Append(LogLevel.Info, "Burst complete: appended 10,000 lines.");
        });
    }

    private void OnClear(object sender, RoutedEventArgs e) => _buffer.Clear();
}
