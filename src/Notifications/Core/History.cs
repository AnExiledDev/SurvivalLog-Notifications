using System;
using System.Collections.Generic;
using System.Linq;

namespace Notifications.Core;

/// <summary>
/// One line of the history. Seq names it for good; Mark grows each time it is raised again, which is how the phone
/// knows it is unread. ToastId is the Toolkit's id for it, known only this session.
/// </summary>
public sealed record HistoryEntry(
    long Seq,
    long Mark,
    Note Content,
    int Count,
    DateTime FirstUtc,
    DateTime LastUtc,
    GameStamp? Stamp,
    Reopen? Reopen)
{
    public long ToastId { get; init; }

    public bool HasToastAction { get; init; }

    /// <summary>The picture its toast showed, as a path from the game's WebUI folder, or null.</summary>
    public string? Icon { get; init; }

    /// <summary>The mark this entry had when the player opened it in the list; raised again, it is unread again.</summary>
    public long ReadMark { get; init; }

    /// <summary>A popup can be rebuilt, or the Toolkit still holds the toast's click action.</summary>
    public bool IsClickable => Reopen != null || (ToastId != 0 && HasToastAction);
}

/// <summary>
/// One save's notices, newest first, at most <see cref="Cap"/>. Identical toasts the Toolkit merged on screen stay one
/// entry with a count.
/// </summary>
public sealed class History
{
    public const int Cap = 500;

    /// <summary>A notice the game draws itself is counted into the newest entry when it repeats within this time.</summary>
    public static readonly TimeSpan LocalMergeWindow = TimeSpan.FromSeconds(10);

    private readonly List<HistoryEntry> _entries = new();

    public History()
    {
    }

    /// <summary>A history read back from its file: entries newest first, the next free number, and what was seen.</summary>
    public History(IEnumerable<HistoryEntry> newestFirst, long next, long seenMark)
    {
        _entries.AddRange(newestFirst.Take(Cap));

        var highest = _entries.Count == 0 ? 0 : _entries.Max(entry => Math.Max(entry.Seq, entry.Mark));

        Next = Math.Max(next, highest + 1);
        SeenMark = Math.Min(Math.Max(0, seenMark), highest);
    }

    public IReadOnlyList<HistoryEntry> Entries => _entries;

    /// <summary>The number the next entry or raise takes.</summary>
    public long Next { get; private set; } = 1;

    /// <summary>Entries raised after this mark are unread.</summary>
    public long SeenMark { get; private set; }

    /// <summary>The highest mark given out so far.</summary>
    public long LatestMark => Next - 1;

    public int UnreadCount => _entries.Count(IsUnread);

    /// <summary>Raised since the player last marked everything read, and not opened in the list since.</summary>
    public bool IsUnread(HistoryEntry entry) => entry.Mark > Math.Max(SeenMark, entry.ReadMark);

    /// <summary>Set by every change, cleared by whoever saves the history.</summary>
    public bool IsDirty { get; set; }

    /// <summary>
    /// Takes one toast trigger. A merge counts into the entry for that toast and brings it to the top; anything else is
    /// a new entry. A rebuild payload, when given, replaces the one the entry had.
    /// </summary>
    public HistoryEntry Record(ToastNote note, GameStamp? stamp, Reopen? reopen)
    {
        var at = note.IsMerge ? _entries.FindIndex(entry => entry.ToastId == note.Id) : -1;

        if (at >= 0) return Raise(at, note.RaisedAtUtc, stamp, reopen);

        return Add(new HistoryEntry(Next, Next, note.Content, 1, note.RaisedAtUtc, note.RaisedAtUtc, stamp, reopen)
        {
            ToastId = note.Id,
            HasToastAction = note.HasClickAction,
            Icon = note.Icon,
        });
    }

    /// <summary>
    /// Takes a notice that never went through the Toolkit, such as a game toast left to the game. It counts into the
    /// newest entry when that one says the same and came in within <see cref="LocalMergeWindow"/>.
    /// </summary>
    public HistoryEntry RecordLocal(Note content, DateTime nowUtc, GameStamp? stamp, Reopen? reopen)
    {
        var newest = _entries.Count == 0 ? null : _entries[0];

        if (newest != null && newest.ToastId == 0 && newest.Content == content && nowUtc - newest.LastUtc <= LocalMergeWindow)
        {
            return Raise(0, nowUtc, stamp, reopen);
        }

        return Add(new HistoryEntry(Next, Next, content, 1, nowUtc, nowUtc, stamp, reopen));
    }

    /// <summary>The player has seen everything up to this mark. A mark from the future counts as the latest.</summary>
    public bool MarkSeen(long mark)
    {
        var seen = Math.Min(mark, LatestMark);

        if (seen <= SeenMark) return false;

        SeenMark = seen;
        IsDirty = true;

        return true;
    }

    /// <summary>The player opened this row in the list. False when it was already read or is not here.</summary>
    public bool MarkRead(long seq)
    {
        var at = _entries.FindIndex(entry => entry.Seq == seq);

        if (at < 0 || !IsUnread(_entries[at])) return false;

        _entries[at] = _entries[at] with { ReadMark = _entries[at].Mark };
        IsDirty = true;

        return true;
    }

    /// <summary>
    /// Empties the list up to this mark. An entry raised after it (the player had not seen it when they cleared) stays.
    /// Returns how many entries went.
    /// </summary>
    public int Clear(long mark)
    {
        var removed = _entries.RemoveAll(entry => entry.Mark <= mark);

        if (removed > 0) IsDirty = true;

        return removed;
    }

    public HistoryEntry? Find(long seq) => _entries.Find(entry => entry.Seq == seq);

    /// <summary>
    /// Gives a toast's entry the popup it stands for, when the popup came after the toast. An entry that already has
    /// one keeps it. False when no entry has that toast.
    /// </summary>
    public bool Attach(long toastId, Reopen reopen)
    {
        var at = toastId == 0 ? -1 : _entries.FindIndex(entry => entry.ToastId == toastId);

        if (at < 0 || _entries[at].Reopen != null) return false;

        _entries[at] = _entries[at] with { Reopen = reopen };
        IsDirty = true;

        return true;
    }

    /// <summary>The Toolkit no longer knows this entry's toast, so it cannot be clicked through the Toolkit again.</summary>
    public void ForgetToast(long seq)
    {
        var at = _entries.FindIndex(entry => entry.Seq == seq);

        if (at < 0 || _entries[at].ToastId == 0) return;

        _entries[at] = _entries[at] with { ToastId = 0 };
        IsDirty = true;
    }

    /// <summary>
    /// Takes in the notices of a history kept while the save was not yet known; they are newer than everything here,
    /// so they go on top in their order and take fresh numbers.
    /// </summary>
    public void Absorb(History other)
    {
        foreach (var entry in other._entries.AsEnumerable().Reverse())
        {
            Add(entry with { Seq = Next, Mark = Next });
        }
    }

    private HistoryEntry Raise(int at, DateTime nowUtc, GameStamp? stamp, Reopen? reopen)
    {
        var old = _entries[at];
        var raised = old with
        {
            Mark = Next,
            Count = old.Count + 1,
            LastUtc = nowUtc,
            Stamp = stamp ?? old.Stamp,
            Reopen = reopen ?? old.Reopen,
        };

        _entries.RemoveAt(at);

        return Add(raised);
    }

    private HistoryEntry Add(HistoryEntry entry)
    {
        _entries.Insert(0, entry);

        if (_entries.Count > Cap) _entries.RemoveRange(Cap, _entries.Count - Cap);

        Next++;
        IsDirty = true;

        return entry;
    }
}
