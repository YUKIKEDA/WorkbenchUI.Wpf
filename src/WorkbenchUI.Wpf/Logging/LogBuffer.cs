using System.Collections.ObjectModel;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Thread-safe document model for log output.
/// <list type="bullet">
/// <item><see cref="Append(LogEntry)"/> can be called from any thread (enqueues into an internal queue).</item>
/// <item><see cref="Flush"/> drains queued entries into <see cref="Entries"/> on the UI thread and trims older rows beyond <see cref="Capacity"/>. <see cref="LogConsole"/> invokes this periodically on a timer.</item>
/// </list>
/// UI-independent for unit testing.
/// </summary>
public sealed class LogBuffer
{
    private readonly Lock _sync = new();
    private readonly Queue<LogEntry> _pending = new();
    private bool _clearRequested;

    /// <summary>Initializes a new instance of the <see cref="LogBuffer"/> class.</summary>
    /// <param name="capacity">Maximum number of retained entries; older entries beyond this limit are discarded.</param>
    public LogBuffer(int capacity = 10000)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(capacity, 0);
        Capacity = capacity;
    }

    /// <summary>Gets the maximum number of log entries retained in <see cref="Entries"/>.</summary>
    public int Capacity { get; }

    /// <summary>Gets the flushed log entries. Modified only on the UI thread by callers of <see cref="Flush"/>.</summary>
    public ObservableCollection<LogEntry> Entries { get; } = [];

    /// <summary>Enqueues a log entry. Safe to call from any thread.</summary>
    public void Append(LogEntry entry)
    {
        lock (_sync)
        {
            _pending.Enqueue(entry);
        }
    }

    /// <summary>Enqueues a log entry stamped with the current local time. Safe to call from any thread.</summary>
    public void Append(LogLevel level, string message) =>
        Append(new LogEntry(DateTime.Now, level, message));

    /// <summary>Requests clearing all log entries. The collection is cleared on the next <see cref="Flush"/> call.</summary>
    public void Clear()
    {
        lock (_sync)
        {
            _clearRequested = true;
            _pending.Clear();
        }
    }

    /// <summary>
    /// Transfers pending log entries into <see cref="Entries"/>. Must be called on the UI thread.
    /// </summary>
    /// <returns><see langword="true"/> if <see cref="Entries"/> changed (useful for auto-scroll decisions); otherwise, <see langword="false"/>.</returns>
    public bool Flush()
    {
        bool clearRequested;
        List<LogEntry>? incoming = null;

        lock (_sync)
        {
            clearRequested = _clearRequested;
            _clearRequested = false;

            if (_pending.Count > 0)
            {
                incoming = new List<LogEntry>(_pending.Count);
                while (_pending.Count > 0)
                {
                    incoming.Add(_pending.Dequeue());
                }
            }
        }

        var changed = false;
        if (clearRequested && Entries.Count > 0)
        {
            Entries.Clear();
            changed = true;
        }

        if (incoming is null || incoming.Count == 0)
        {
            return changed;
        }

        if (incoming.Count >= Capacity)
        {
            // Replacing wholesale is faster when incoming entries alone exceed capacity
            Entries.Clear();
            for (var i = incoming.Count - Capacity; i < incoming.Count; i++)
            {
                Entries.Add(incoming[i]);
            }
        }
        else
        {
            foreach (var entry in incoming)
            {
                Entries.Add(entry);
            }

            while (Entries.Count > Capacity)
            {
                Entries.RemoveAt(0);
            }
        }

        return true;
    }
}
