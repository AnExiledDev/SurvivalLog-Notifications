using System;

namespace Notifications.Core;

/// <summary>
/// Keeps the history of the save being played. Out of a game there is none. In a game whose save is not named yet,
/// notices go to a history of their own that joins the save's once the name is known. Changes are written at most
/// every <see cref="FlushSeconds"/>, and at once when the save is left or changed.
/// </summary>
public sealed class Ledger
{
    public const double FlushSeconds = 2;

    private readonly IHistoryStore _store;
    private readonly Action<string> _log;
    private string? _fileName;
    private bool _canWrite;
    private bool _hasReportedWriteFailure;
    private double _nextFlushAt;

    public Ledger(IHistoryStore store, Action<string> log)
    {
        _store = store;
        _log = log;
    }

    /// <summary>The history being kept, or null out of a game.</summary>
    public History? Current { get; private set; }

    /// <summary>Changes whenever what the phone shows would change.</summary>
    public long Revision { get; private set; }

    /// <summary>Called every frame with where the game is.</summary>
    public void Follow(bool isInGame, string? saveKey, double now)
    {
        if (!isInGame)
        {
            Leave(now);

            return;
        }

        if (saveKey == null)
        {
            if (Current == null) Swap(new History(), null, false);

            return;
        }

        var fileName = SaveFileName.For(saveKey);

        if (fileName == _fileName && Current != null) return;

        if (_fileName != null) Flush(now, true);

        var (loaded, canWrite) = Load(fileName);

        if (Current != null && _fileName == null) loaded.Absorb(Current);

        Swap(loaded, fileName, canWrite);
    }

    public HistoryEntry? Record(ToastNote note, GameStamp? stamp, Reopen? reopen) => Changed(Current?.Record(note, stamp, reopen));

    public HistoryEntry? RecordLocal(Note content, DateTime nowUtc, GameStamp? stamp, Reopen? reopen) =>
        Changed(Current?.RecordLocal(content, nowUtc, stamp, reopen));

    public void MarkSeen(long mark)
    {
        if (Current != null && Current.MarkSeen(mark)) Revision++;
    }

    public void MarkRead(long seq)
    {
        if (Current != null && Current.MarkRead(seq)) Revision++;
    }

    public void Clear(long mark)
    {
        if (Current != null && Current.Clear(mark) > 0) Revision++;
    }

    public void Attach(long toastId, Reopen reopen)
    {
        if (Current != null && Current.Attach(toastId, reopen)) Revision++;
    }

    public void ForgetToast(long seq)
    {
        if (Current == null) return;

        Current.ForgetToast(seq);
        Revision++;
    }

    /// <summary>Writes the history if it changed and the last write was long enough ago, or now when forced.</summary>
    public void Flush(double now, bool isForced = false)
    {
        if (Current is not { IsDirty: true } history || _fileName == null || !_canWrite) return;

        if (!isForced && now < _nextFlushAt) return;

        _nextFlushAt = now + FlushSeconds;

        var failure = _store.Write(_fileName, HistoryJson.Write(history));

        if (failure == null)
        {
            history.IsDirty = false;

            return;
        }

        if (!_hasReportedWriteFailure) _log($"could not save the notification history {_fileName} (reported once): {failure}");

        _hasReportedWriteFailure = true;
    }

    private void Leave(double now)
    {
        if (Current == null) return;

        Flush(now, true);
        Swap(null, null, false);
    }

    private (History History, bool CanWrite) Load(string fileName)
    {
        switch (_store.Read(fileName))
        {
            case StoredText.Found found:
                return Parse(fileName, found.Text);
            case StoredText.Unreadable unreadable:
                _log($"could not read the notification history {fileName}, so this session's notices are not saved over it: {unreadable.Reason}");

                return (new History(), false);
            default:
                return (new History(), true);
        }
    }

    private (History History, bool CanWrite) Parse(string fileName, string text)
    {
        switch (HistoryJson.Read(text))
        {
            case HistoryRead.Loaded loaded:
                if (loaded.SkippedEntries > 0) _log($"skipped {loaded.SkippedEntries} unreadable entries in the notification history {fileName}");

                return (loaded.History, true);
            case HistoryRead.Corrupt corrupt:
                var failure = _store.SetAside(fileName);

                _log(failure == null
                    ? $"the notification history {fileName} could not be read ({corrupt.Reason}); it was kept aside and a new one started"
                    : $"the notification history {fileName} could not be read ({corrupt.Reason}) or moved aside ({failure}), so it is left as it is and not saved over");

                return (new History(), failure == null);
            default:
                return (new History(), false);
        }
    }

    private void Swap(History? history, string? fileName, bool canWrite)
    {
        Current = history;
        _fileName = fileName;
        _canWrite = canWrite;
        _nextFlushAt = 0;
        Revision++;
    }

    private HistoryEntry? Changed(HistoryEntry? entry)
    {
        if (entry != null) Revision++;

        return entry;
    }
}
