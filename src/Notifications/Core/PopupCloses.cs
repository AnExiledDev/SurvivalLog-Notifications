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

/// <summary>What is due for one popup a toast stood in for.</summary>
public abstract record CloseStep(PopupKind Kind)
{
    /// <summary>The popup is up: close it the way its own Close or OK button does.</summary>
    public sealed record Close(PopupKind Kind, int FramesWaited) : CloseStep(Kind)
    {
        public string Line => $"{PopupKinds.Name(Kind)}: toast shown, popup closed by the game's close action after {FramesWaited} frame(s)";
    }

    /// <summary>The popup is still up a few frames after its close, and the game did not open it again: send the close again.</summary>
    public sealed record CloseAgain(PopupKind Kind, int Closes) : CloseStep(Kind)
    {
        public string Line => $"{PopupKinds.Name(Kind)}: still up {PendingCloses.SettleFrames} frame(s) after its close, so the game's close action was sent again (close {Closes} of {PendingCloses.MostCloses})";
    }

    /// <summary>Its close has had its frames to settle. Up again here means the game opened it again, and a later close is pending.</summary>
    public sealed record Settled(PopupKind Kind, bool IsUpAgain) : CloseStep(Kind)
    {
        public string Line => IsUpAgain
            ? $"{PopupKinds.Name(Kind)}: up again {PendingCloses.SettleFrames} frame(s) after its close because the game opened it again; that open gets its own close"
            : $"{PopupKinds.Name(Kind)}: gone {PendingCloses.SettleFrames} frame(s) after its close";
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
}

/// <summary>
/// Popups a toast stood in for, which the game still opens and the mod then closes. Skipping the game's open left the
/// game waiting for a popup that never closed, so the popup opens as usual and is closed from a later frame, never from
/// inside the game's own dispatch.
/// <para>
/// Closes go one at a time: a close is sent only when no other close is settling, and a settling close is checked
/// <see cref="SettleFrames"/> frames later. A popup still up then, which the game did not open again, gets its close
/// again, up to <see cref="MostCloses"/> closes, and is then left visible. A popup the game opens again (closing the
/// received-items popup hands it any items held back while it was up) is a fresh popup to close. A popup that does not
/// come up in time is given up on, and a popup the player opens again from its toast is never closed.
/// </para>
/// </summary>
public sealed class PendingCloses
{
    public const double GiveUpSeconds = 5;
    public const int SettleFrames = 3;
    public const int MostCloses = 2;

    private readonly Dictionary<PopupKind, Pending> _pending = new();
    private Sent? _settling;

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

    /// <summary>The steps due in this frame: any give-ups, then at most one close or one settle check.</summary>
    public IReadOnlyList<CloseStep> Due(int frame, double now, Func<PopupKind, bool> isUp)
    {
        var steps = GiveUps(frame, now, isUp);

        if (NextClose(frame, isUp) is { } step) steps.Add(step);

        return steps;
    }

    /// <summary>A popup still up past the time is closed when its turn comes rather than given up on.</summary>
    private List<CloseStep> GiveUps(int frame, double now, Func<PopupKind, bool> isUp)
    {
        var steps = new List<CloseStep>();

        foreach (var (kind, pending) in _pending.ToList())
        {
            if (frame <= pending.Frame || now - pending.Since < GiveUpSeconds || isUp(kind)) continue;

            _pending.Remove(kind);
            steps.Add(new CloseStep.GiveUp(kind));
        }

        return steps;
    }

    private CloseStep? NextClose(int frame, Func<PopupKind, bool> isUp)
    {
        if (_settling is { } settling) return frame - settling.Frame < SettleFrames ? null : Check(settling, frame, isUp);

        return FirstUp(frame, isUp) is { } kind ? Send(kind, frame) : null;
    }

    /// <summary>The oldest popup that is up, never in the frame the game opened it, as the game is still inside that open.</summary>
    private PopupKind? FirstUp(int frame, Func<PopupKind, bool> isUp)
    {
        foreach (var (kind, pending) in _pending.OrderBy(entry => entry.Value.Frame).ThenBy(entry => entry.Key))
        {
            if (frame > pending.Frame && isUp(kind)) return kind;
        }

        return null;
    }

    private CloseStep Send(PopupKind kind, int frame)
    {
        var waited = frame - _pending[kind].Frame;

        _pending.Remove(kind);
        _settling = new Sent(kind, frame, 1);

        return new CloseStep.Close(kind, waited);
    }

    private CloseStep Check(Sent settling, int frame, Func<PopupKind, bool> isUp)
    {
        var isStillUp = isUp(settling.Kind);
        var isOpenedAgain = _pending.ContainsKey(settling.Kind);

        _settling = null;

        if (!isStillUp || isOpenedAgain) return new CloseStep.Settled(settling.Kind, isStillUp);

        if (settling.Closes >= MostCloses) return new CloseStep.LeftVisible(settling.Kind);

        _settling = new Sent(settling.Kind, frame, settling.Closes + 1);

        return new CloseStep.CloseAgain(settling.Kind, settling.Closes + 1);
    }

    private readonly record struct Pending(int Frame, double Since);

    private readonly record struct Sent(PopupKind Kind, int Frame, int Closes);
}
