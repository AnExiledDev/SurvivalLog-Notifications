namespace Notifications.Core;

/// <summary>Where a notice can go: a Toolkit toast, or straight into the history.</summary>
public interface INoticeOut
{
    /// <summary>Raises a toast that opens the popup again when clicked. False when the Toolkit did not take it.</summary>
    bool Raise(Note note, Reopen? reopen);

    /// <summary>Puts a notice the game shows itself into the history.</summary>
    void Keep(Note note, Reopen? reopen);
}

/// <summary>Sends one game popup or game toast where the player's switches say.</summary>
public static class NoticeRoute
{
    /// <summary>
    /// Whether the game must still show its own popup or toast. It is held back only when a toast stood in for it;
    /// a toast that could not be raised leaves it to the game, and the history keeps a copy so nothing is lost.
    /// </summary>
    public static bool IsGameShowing(GameToastHandling handling, Note note, Reopen? reopen, INoticeOut notices)
    {
        if (handling == GameToastHandling.Leave) return true;

        if (handling == GameToastHandling.Replace && notices.Raise(note, reopen)) return false;

        notices.Keep(note, reopen);

        return true;
    }
}

/// <summary>What clicking a history row does.</summary>
public abstract record RowClick
{
    private RowClick()
    {
    }

    /// <summary>Opens the game popup again with the data it first had.</summary>
    public sealed record Popup(Reopen Reopen) : RowClick;

    /// <summary>Runs the toast's own click action, which the Toolkit keeps for a while.</summary>
    public sealed record Toast(long ToastId) : RowClick;

    public sealed record Nothing : RowClick;

    /// <summary>A popup that can be rebuilt wins over the toast's action, as it works after a restart too.</summary>
    public static RowClick For(HistoryEntry? entry)
    {
        if (entry?.Reopen is { } reopen) return new Popup(reopen);

        return entry is { ToastId: not 0, HasToastAction: true } ? new Toast(entry.ToastId) : new Nothing();
    }
}
