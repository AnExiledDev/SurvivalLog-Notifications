using System;
using System.Linq;
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
    private readonly ToastWait _waiting = new();
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
        var id = Toasts.Show(Request(note, reopen) with { OnClick = onClick });

        if (id.IsNone) return 0;

        _waiting.Add(id.Value, reopen, DateTime.UtcNow);

        return id.Value;
    }

    /// <summary>
    /// The toast for a notice. Received items show as chips with their pictures instead of a line of text, and a wish
    /// shows the item wished for.
    /// </summary>
    private ToastRequest Request(Note note, Reopen? reopen)
    {
        var request = new ToastRequest(note.Source, note.Title) { Body = note.Body, Kind = KindOf(note.Kind) };

        switch (reopen)
        {
            case Reopen.Items { Stacks.Count: > 0 } items:
            {
                var words = new GameWords(WebIcons.Shared(_log));
                var chips = items.Stacks.Take(ToastItem.Max).Select(stack => new ToastItem(NoteTexts.ItemName(words, stack.ConfigId))
                {
                    Icon = words.ItemIcon(stack.ConfigId),
                    Count = stack.Count,
                }).ToList();

                return request with { Body = NoteTexts.Plain(items.Desc) is { Length: > 0 } desc ? desc : null, Items = chips, Icon = chips[0].Icon };
            }
            case Reopen.Wish { ItemConfigId: > 0 } wish:
                return request with { Icon = new GameWords(WebIcons.Shared(_log)).ItemIcon(wish.ItemConfigId) };
            default:
                return request;
        }
    }

    /// <summary>Gives a toast already raised the popup it stands for, which came after it.</summary>
    public void Attach(long toastId, Reopen reopen)
    {
        if (!_waiting.Attach(toastId, reopen)) _ledger.Attach(toastId, reopen);
    }

    /// <summary>Hears every toast trigger from the Toolkit.</summary>
    public void OnRaised(ToastRecord record)
    {
        var id = record.Id.Value;
        var reopen = _waiting.Take(id);
        var note = new ToastNote(id, new Note(record.Source, record.Title, record.Body, KindOf(record.Kind)), record.RaisedAtUtc, record.HasClickAction, record.IsMerge)
        {
            Icon = record.Icon,
        };

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
