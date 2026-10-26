using System;
using System.Collections.Generic;
using System.Linq;

namespace Notifications.Core;

/// <summary>The game popups a toast can stand in for.</summary>
public enum PopupKind
{
    Wish,
    Items,
    Wave,
    Skill,
}

public static class PopupKinds
{
    public static IReadOnlyList<PopupKind> All { get; } = new[] { PopupKind.Wish, PopupKind.Items, PopupKind.Wave, PopupKind.Skill };

    public static PopupKind Of(Reopen reopen) => reopen switch
    {
        Reopen.Wish => PopupKind.Wish,
        Reopen.Items => PopupKind.Items,
        Reopen.Wave => PopupKind.Wave,
        Reopen.Skill => PopupKind.Skill,
        _ => throw new ArgumentOutOfRangeException(nameof(reopen), reopen, "a popup with no kind"),
    };

    public static string Name(PopupKind kind) => kind switch
    {
        PopupKind.Wish => "wish popup",
        PopupKind.Items => "received-items popup",
        PopupKind.Wave => "zombie wave report",
        PopupKind.Skill => "skill level-up popup",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}

/// <summary>What a popup's window shows right now.</summary>
public enum PopupSight
{
    Down,

    /// <summary>The full popup a toast stands in for.</summary>
    Full,

    /// <summary>Something the mod leaves to the game, such as a small pickup pop, or a window that could not be read.</summary>
    Other,
}

/// <summary>How a popup's close turned out once it had settled.</summary>
public enum AfterClose
{
    Gone,

    /// <summary>The game opened it again with a toast standing in, so that open is closed in its own turn.</summary>
    OpenedAgainToClose,

    /// <summary>It went down, and its window is up again with something the mod never stood in for.</summary>
    LeftToTheGame,
}

/// <summary>What is due for one popup a toast stood in for.</summary>
public abstract record CloseStep(PopupKind Kind)
{
    /// <summary>The popup is up: close it the way its own Close or OK button does.</summary>
    public sealed record Close(PopupKind Kind, int FramesWaited) : CloseStep(Kind)
    {
        public string Line => $"{PopupKinds.Name(Kind)}: toast shown, popup closed by the game's close action after {FramesWaited} frame(s)";
    }

    /// <summary>The popup was never seen down while its close settled, and the game did not open it again: send the close again.</summary>
    public sealed record CloseAgain(PopupKind Kind, int Closes) : CloseStep(Kind)
    {
        public string Line => $"{PopupKinds.Name(Kind)}: never seen gone after its close, so the game's close action was sent again (close {Closes} of {PendingCloses.MostCloses})";
    }

    /// <summary>Its close has settled and needs nothing more.</summary>
    public sealed record Settled(PopupKind Kind, AfterClose After) : CloseStep(Kind)
    {
        public string Line => After switch
        {
            AfterClose.Gone => $"{PopupKinds.Name(Kind)}: gone after its close",
            AfterClose.OpenedAgainToClose => $"{PopupKinds.Name(Kind)}: the game opened it again with a toast after its close; that open gets its own close",
            AfterClose.LeftToTheGame => $"{PopupKinds.Name(Kind)}: gone after its close, and its window is up again with something the mod leaves to the game",
            _ => throw new ArgumentOutOfRangeException(nameof(After), After, null),
        };
    }

    /// <summary>The popup would not go away, so the mod stops closing it and the player closes it as usual.</summary>
    public sealed record LeftVisible(PopupKind Kind) : CloseStep(Kind)
    {
        public string Line => $"{PopupKinds.Name(Kind)}: still up after {PendingCloses.MostCloses} closes; left visible for the player to close";
    }

    /// <summary>The popup never came up, so there is nothing of ours to close.</summary>
    public sealed record GiveUp(PopupKind Kind) : CloseStep(Kind)
    {
        public string Line => $"{PopupKinds.Name(Kind)}: toast shown, but the popup was not seen open within {PendingCloses.GiveUpSeconds} seconds; nothing was closed";
    }

    /// <summary>Nothing has been left to close for a while since the last close, named by its kind: a last look at the screen.</summary>
    public sealed record Quiet(PopupKind Kind) : CloseStep(Kind)
    {
        public string Line => $"{PendingCloses.QuietSeconds * 1000:0} ms after the last close ({PopupKinds.Name(Kind)}), nothing is left to close";
    }
}

/// <summary>
/// Popups a toast stood in for, which the game still opens and the mod then closes. Skipping the game's open left the
/// game waiting for a popup that never closed, so the popup opens as usual and is closed from a later frame, never from
/// inside the game's own dispatch.
/// <para>
/// Closes go one at a time: a close is sent only when no other close is settling. A close settles for at least
/// <see cref="SettleFrames"/> frames and <see cref="SettleSeconds"/> seconds, and its popup is looked at every frame
/// meanwhile. Once the popup has been seen down, the close worked: a later open is closed only if a toast stood in for
/// it, and anything else (closing the received-items popup lets the game show the small pickup pops it held back) is
/// the game's. A popup never seen down gets its close again, up to <see cref="MostCloses"/> closes, and is then left
/// visible. A popup that does not come up in time is given up on, and a popup the player opens again from its toast is
/// never closed.
/// </para>
/// </summary>
public sealed class PendingCloses
{
    public const double GiveUpSeconds = 5;
    public const int SettleFrames = 3;
    public const double SettleSeconds = 0.05;
    public const int MostCloses = 2;
    public const double QuietSeconds = 0.75;

    private readonly Dictionary<PopupKind, Pending> _pending = new();
    private Sent? _settling;
    private PopupKind? _lastClosed;
    private double _lastBusy;

    /// <summary>
    /// A toast stood in for this popup, which the game is opening in this frame. A popup is one page, so a second open
    /// of the same kind before its close is the same page and replaces the first.
    /// </summary>
    public void Expect(PopupKind kind, int frame, double now) => _pending[kind] = new Pending(frame, now);

    /// <summary>
    /// The popup is the player's to see after all, so nothing more is sent for it. Whether a close was still to be sent;
    /// a close already sent has closed the popup.
    /// </summary>
    public bool Cancel(PopupKind kind)
    {
        if (_settling?.Kind == kind) _settling = null;

        return _pending.Remove(kind);
    }

    /// <summary>The steps due in this frame: any give-ups, then at most one close or one settled close, then any last look.</summary>
    public IReadOnlyList<CloseStep> Due(int frame, double now, Func<PopupKind, PopupSight> sight)
    {
        // Asked every frame and idle nearly always: then nothing can be due, and nothing is allocated.
        if (_settling == null && _pending.Count == 0 && _lastClosed == null) return Array.Empty<CloseStep>();

        var steps = GiveUps(frame, now, sight);

        if (NextClose(frame, now, sight) is { } step) steps.Add(step);

        if (Quiet(now) is { } quiet) steps.Add(quiet);

        return steps;
    }

    /// <summary>A popup still up past the time is closed when its turn comes rather than given up on.</summary>
    private List<CloseStep> GiveUps(int frame, double now, Func<PopupKind, PopupSight> sight)
    {
        var steps = new List<CloseStep>();

        foreach (var (kind, pending) in _pending.ToList())
        {
            if (frame <= pending.Frame || now - pending.Since < GiveUpSeconds || sight(kind) == PopupSight.Full) continue;

            _pending.Remove(kind);
            steps.Add(new CloseStep.GiveUp(kind));
        }

        return steps;
    }

    private CloseStep? NextClose(int frame, double now, Func<PopupKind, PopupSight> sight)
    {
        if (_settling is { } settling) return Watch(settling, frame, now, sight(settling.Kind));

        return FirstUp(frame, sight) is { } kind ? Send(kind, frame, now) : null;
    }

    /// <summary>The oldest popup that is up, never in the frame the game opened it, as the game is still inside that open.</summary>
    private PopupKind? FirstUp(int frame, Func<PopupKind, PopupSight> sight)
    {
        foreach (var (kind, pending) in _pending.OrderBy(entry => entry.Value.Frame).ThenBy(entry => entry.Key))
        {
            if (frame > pending.Frame && sight(kind) == PopupSight.Full) return kind;
        }

        return null;
    }

    private CloseStep Send(PopupKind kind, int frame, double now)
    {
        var waited = frame - _pending[kind].Frame;

        _pending.Remove(kind);
        _settling = new Sent(kind, frame, now, 1, false);

        return new CloseStep.Close(kind, waited);
    }

    /// <summary>Looks at a settling close's popup every frame, and answers for it once the close has settled.</summary>
    private CloseStep? Watch(Sent settling, int frame, double now, PopupSight seen)
    {
        var watched = seen == PopupSight.Full ? settling : settling with { WasSeenDown = true };
        var isSettling = frame - settling.Frame < SettleFrames || now - settling.Since < SettleSeconds;
        _settling = watched;

        return isSettling ? null : Answer(watched, frame, now, seen);
    }

    private CloseStep Answer(Sent settled, int frame, double now, PopupSight seen)
    {
        _settling = null;
        _lastClosed = settled.Kind;
        _lastBusy = now;

        if (_pending.ContainsKey(settled.Kind)) return new CloseStep.Settled(settled.Kind, AfterClose.OpenedAgainToClose);

        if (settled.WasSeenDown) return new CloseStep.Settled(settled.Kind, seen == PopupSight.Down ? AfterClose.Gone : AfterClose.LeftToTheGame);

        if (settled.Closes >= MostCloses) return new CloseStep.LeftVisible(settled.Kind);

        _settling = new Sent(settled.Kind, frame, now, settled.Closes + 1, false);

        return new CloseStep.CloseAgain(settled.Kind, settled.Closes + 1);
    }

    /// <summary>Once per run of closes, when nothing has been left to close for <see cref="QuietSeconds"/>.</summary>
    private CloseStep? Quiet(double now)
    {
        if (_settling != null || _pending.Count > 0)
        {
            _lastBusy = now;

            return null;
        }

        if (_lastClosed is not { } kind || now - _lastBusy < QuietSeconds) return null;

        _lastClosed = null;

        return new CloseStep.Quiet(kind);
    }

    private readonly record struct Pending(int Frame, double Since);

    private readonly record struct Sent(PopupKind Kind, int Frame, double Since, int Closes, bool WasSeenDown);
}
