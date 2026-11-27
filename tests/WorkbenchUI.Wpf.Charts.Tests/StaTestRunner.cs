using System.Windows;
using System.Windows.Threading;

namespace WorkbenchUI.Wpf.Charts.Tests;

/// <summary>Executes WPF chart tests on a single shared background STA dispatcher thread.</summary>
internal static class StaTestRunner
{
    private static readonly Lazy<Dispatcher> UiDispatcher =
        new(StartStaDispatcher, LazyThreadSafetyMode.ExecutionAndPublication);

    public static void Run(Action action)
    {
        var dispatcher = UiDispatcher.Value;
        if (dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }

    private static Dispatcher StartStaDispatcher()
    {
        Dispatcher? dispatcher = null;
        using var ready = new ManualResetEventSlim(false);
        var thread = new Thread(() =>
        {
            if (Application.Current is null)
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }

            dispatcher = Dispatcher.CurrentDispatcher;
            ready.Set();
            Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = "WorkbenchUI.Wpf.Charts.Tests STA Runner",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        ready.Wait();
        return dispatcher!;
    }
}
