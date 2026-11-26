namespace WorkbenchUI.Wpf.Controls;

/// <summary>Specifies the severity level of a <see cref="LogEntry"/>.</summary>
public enum LogLevel
{
    /// <summary>Diagnostic debug message.</summary>
    Debug,

    /// <summary>Informational message.</summary>
    Info,

    /// <summary>Warning condition.</summary>
    Warning,

    /// <summary>Error condition.</summary>
    Error,
}

/// <summary>Represents a single log entry.</summary>
/// <param name="Timestamp">The time the log entry was recorded.</param>
/// <param name="Level">The severity level of the entry.</param>
/// <param name="Message">The log message text.</param>
public readonly record struct LogEntry(DateTime Timestamp, LogLevel Level, string Message);
