using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using ExiledToolkit;
using Notifications.Core;

namespace Notifications.Adapters;

/// <summary>
/// The mod's one contact with the Toolkit's toasts. It raises toasts, and it hears every toast raised by any mod,
/// which goes into the history with the popup it stands for. A toast is reported the frame after it was raised, so the
/// popup waits here under the toast's id until then. All members run on the game's main thread.
/// </summary>
internal sealed class ToastBridge
{
    private const string ToolkitGuid = "com.dev.survivallog.exiledtoolkit";

    private readonly Ledger _ledger;
    private readonly Action<string> _log;
    private readonly Dictionary<long, Queue<Reopen?>> _waiting = new();
    private readonly Dictionary<long, Reopen> _lateAttach = new();
    private ConfigEntry<bool>? _toastsShown;
    private bool _hasLookedForSetting;

    public ToastBridge(Ledger ledger, Action<string> log)
    {
        _ledger = ledger;
        _log = log;
    }

    /// <summary>Raises a toast; the history takes it with this popup. The toast's id, or 0 when the Toolkit did not take it.</summary>
    public long Show(Note note, Reopen? reopen, Action? onClick)
    {
        var id = Toasts.Show(new ToastRequest(note.Source, note.Title) { Body = note.Body, Kind = KindOf(note.Kind), OnClick = onClick });

        if (id.IsNone) return 0;

        if (!_waiting.TryGetValue(id.Value, out var popups)) _waiting[id.Value] = popups = new Queue<Reopen?>();

        popups.Enqueue(reopen);

        return id.Value;
    }

    /// <summary>Gives a toast already raised the popup it stands for, which came after it.</summary>
    public void Attach(long toastId, Reopen reopen)
    {
        if (_waiting.ContainsKey(toastId)) _lateAttach[toastId] = reopen;
        else _ledger.Attach(toastId, reopen);
    }

    /// <summary>Hears every toast trigger from the Toolkit.</summary>
    public void OnRaised(ToastRecord record)
    {
        var id = record.Id.Value;
        var reopen = TakeWaiting(id);
        var note = new ToastNote(id, new Note(record.Source, record.Title, record.Body, KindOf(record.Kind)), record.RaisedAtUtc, record.HasClickAction, record.IsMerge);

        _ledger.Record(note, GameState.Stamp(), reopen);
    }

    /// <summary>
    /// Whether the Toolkit shows its toasts. With them switched off a popup must not be replaced by a toast nobody
    /// sees. A setting that cannot be read counts as on, the Toolkit's default.
    /// </summary>
    public bool AreShown()
    {
        if (!_hasLookedForSetting)
        {
            _hasLookedForSetting = true;
            _toastsShown = FindToastsSetting();
        }

        return _toastsShown?.Value ?? true;
    }

    private Reopen? TakeWaiting(long id)
    {
        if (!_waiting.TryGetValue(id, out var popups)) return null;

        var reopen = popups.Count > 0 ? popups.Dequeue() : null;

        if (popups.Count > 0) return reopen;

        _waiting.Remove(id);

        if (_lateAttach.Remove(id, out var late)) reopen ??= late;

        return reopen;
    }

    private ConfigEntry<bool>? FindToastsSetting()
    {
        try
        {
            if (IL2CPPChainloader.Instance.Plugins.TryGetValue(ToolkitGuid, out var info) && info.Instance is BasePlugin toolkit &&
                toolkit.Config.TryGetEntry<bool>("Toasts", "Enabled", out var entry))
            {
                return entry;
            }
        }
        catch (Exception ex)
        {
            _log("could not read whether the Toolkit shows toasts, so they count as shown: " + ex.Message);

            return null;
        }

        _log("the Toolkit's Toasts/Enabled setting was not found, so toasts count as shown");

        return null;
    }

    private static ToastKind KindOf(NoteKind kind) => kind switch
    {
        NoteKind.Success => ToastKind.Success,
        NoteKind.Warning => ToastKind.Warning,
        NoteKind.Error => ToastKind.Error,
        _ => ToastKind.Info,
    };

    private static NoteKind KindOf(ToastKind kind) => kind switch
    {
        ToastKind.Success => NoteKind.Success,
        ToastKind.Warning => NoteKind.Warning,
        ToastKind.Error => NoteKind.Error,
        _ => NoteKind.Info,
    };
}
