using System;
using System.Collections.Generic;
using ExiledToolkit;
using Notifications.Core;

namespace Notifications.Adapters;

/// <summary>
/// Where the hooks hand their notices: it decides by the player's switches whether a toast stands in for the game's
/// popup, raises it, keeps the history's copy, and opens popups again when a toast or a history row is clicked. All
/// members run on the game's main thread.
/// </summary>
internal sealed class NoticeDesk : INoticeOut
{
    private readonly Ledger _ledger;
    private readonly ToastBridge _toasts;
    private readonly Action<string> _log;
    private readonly HashSet<string> _reportedFailures = new();

    public NoticeDesk(Switches switches, Ledger ledger, ToastBridge toasts, Action<string> log)
    {
        Switches = switches;
        _ledger = ledger;
        _toasts = toasts;
        _log = log;
    }

    public Switches Switches { get; }

    public GameToastHandling ForPopup(bool isSwitchOn) => Handling.Popup(isSwitchOn, _toasts.AreShown());

    public GameToastHandling ForGameToast(bool isGroupOn) => Handling.GameToast(isGroupOn, Switches.KeepGameToastLook, _toasts.AreShown());

    /// <summary>
    /// Sends one popup or game toast where the handling says, and answers whether the game must still show its own.
    /// A notice that cannot be read is left to the game, so a failure here never loses one.
    /// </summary>
    public bool IsGameShowing(string what, GameToastHandling handling, Func<(Note Note, Reopen? Reopen)?> build)
    {
        if (handling == GameToastHandling.Leave) return true;

        try
        {
            return build() is not { } notice || NoticeRoute.IsGameShowing(handling, notice.Note, notice.Reopen, this);
        }
        catch (Exception ex)
        {
            Fail("reading the " + what, ex);

            return true;
        }
    }

    public bool Raise(Note note, Reopen? reopen) => _toasts.Show(note, reopen, reopen == null ? null : () => OpenPopup(reopen)) != 0;

    public void Keep(Note note, Reopen? reopen) => _ledger.RecordLocal(note, DateTime.UtcNow, GameState.Stamp(), reopen);

    /// <summary>A toast whose click does what it is given. Its id, or 0 when the Toolkit did not take it.</summary>
    public long Raise(Note note, Reopen? reopen, Action onClick) => _toasts.Show(note, reopen, onClick);

    public void Attach(long toastId, Reopen reopen) => _toasts.Attach(toastId, reopen);

    public void OpenPopup(Reopen reopen)
    {
        if (Reopener.Open(reopen) is { } failure) _log($"could not open the {reopen.GetType().Name} popup again: {failure}");
    }

    /// <summary>The player clicked a row of the phone's list.</summary>
    public void OpenRow(long seq)
    {
        switch (RowClick.For(_ledger.Current?.Find(seq)))
        {
            case RowClick.Popup popup:
                OpenPopup(popup.Reopen);

                break;
            case RowClick.Toast toast:
                // The Toolkit keeps a toast's click action only for so long; past that the row stops offering it.
                if (!Toasts.Activate(new ToastId(toast.ToastId))) _ledger.ForgetToast(seq);

                break;
        }
    }

    /// <summary>Each kind of failure is logged the first time only, so a hook that fails every call cannot flood the log.</summary>
    public void Fail(string doing, Exception ex)
    {
        if (_reportedFailures.Add(doing)) _log($"failed {doing}, the game shows it as usual (logged once): {ex}");
    }
}
