using System;
using System.Collections.Generic;
using System.Linq;

namespace Notifications.Core;

/// <summary>
/// Popups waiting under their toast's id for the Toolkit to report the toast, which it does the frame after the toast
/// is raised. A toast the Toolkit never reports would hold its popups for the whole session, so a toast that has had
/// nothing new for <see cref="Limit"/> is forgotten.
/// </summary>
public sealed class ToastWait
{
    public static readonly TimeSpan Limit = TimeSpan.FromSeconds(60);

    private readonly Dictionary<long, Waiting> _toasts = new();

    /// <summary>A toast was raised for this popup. A repeat merged into a toast still waiting queues behind the first.</summary>
    public void Add(long toastId, Reopen? reopen, DateTime nowUtc)
    {
        Forget(nowUtc);

        if (!_toasts.TryGetValue(toastId, out var waiting)) _toasts[toastId] = waiting = new Waiting();

        waiting.Popups.Enqueue(reopen);
        waiting.LastRaisedUtc = nowUtc;
    }

    /// <summary>Gives a waiting toast the popup that came after it. False when the toast is not waiting.</summary>
    public bool Attach(long toastId, Reopen reopen)
    {
        if (!_toasts.TryGetValue(toastId, out var waiting)) return false;

        waiting.Late = reopen;

        return true;
    }

    /// <summary>
    /// The popup for one report of the toast, in the order they were raised. The last report stops the wait, and takes
    /// the popup attached late when it has none of its own.
    /// </summary>
    public Reopen? Take(long toastId)
    {
        if (!_toasts.TryGetValue(toastId, out var waiting)) return null;

        var reopen = waiting.Popups.Count > 0 ? waiting.Popups.Dequeue() : null;

        if (waiting.Popups.Count > 0) return reopen;

        _toasts.Remove(toastId);

        return reopen ?? waiting.Late;
    }

    private void Forget(DateTime nowUtc)
    {
        var stale = _toasts.Where(toast => nowUtc - toast.Value.LastRaisedUtc > Limit).Select(toast => toast.Key).ToList();

        foreach (var toastId in stale) _toasts.Remove(toastId);
    }

    private sealed class Waiting
    {
        public Queue<Reopen?> Popups { get; } = new();

        public DateTime LastRaisedUtc { get; set; }

        public Reopen? Late { get; set; }
    }
}
