namespace WorkbenchUI.Wpf.Controls;

/// <summary>Reachability and completion state of a chapter on a <see cref="ProcessRail"/>.</summary>
public enum ProcessChapterState
{
    /// <summary>Not yet ready; cannot be selected.</summary>
    Locked,

    /// <summary>Reachable, but not yet completed.</summary>
    Available,

    /// <summary>Currently active chapter.</summary>
    Current,

    /// <summary>Completed and available to revisit.</summary>
    Completed,

    /// <summary>Invalidated by an upstream change after completion.</summary>
    Stale,
}
