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

    /// <summary>The popup never came up, so there is nothing of ours to close.</summary>
    public sealed record GiveUp(PopupKind Kind) : CloseStep(Kind)
    {
        public string Line => $"{PopupKinds.Name(Kind)}: toast shown, but the popup was not seen open within {PendingCloses.GiveUpSeconds} seconds; nothing was closed";
    }
}

/// <summary>
/// Popups a toast stood in for, which the game still opens and the mod then closes. Skipping the game's open left the
/// game waiting for a popup that never closed, so the popup opens as usual and is closed from a later frame, never from
/// inside the game's own dispatch. Each kind has at most one close pending; a popup that does not come up in time is
/// given up on, and a popup the player opens again from its toast is never closed.
/// </summary>
public sealed class PendingCloses
{
    public const double GiveUpSeconds = 5;

    private readonly Dictionary<PopupKind, Pending> _pending = new();

    /// <summary>A toast stood in for this popup, which the game is opening in this frame.</summary>
    public void Expect(PopupKind kind, int frame, double now) => _pending[kind] = new Pending(frame, now);

    /// <summary>The popup is the player's to see after all. Whether a close was pending.</summary>
    public bool Cancel(PopupKind kind) => _pending.Remove(kind);

    /// <summary>The closes and give-ups due in this frame; each pending popup is answered once.</summary>
    public IReadOnlyList<CloseStep> Due(int frame, double now, Func<PopupKind, bool> isUp)
    {
        var steps = new List<CloseStep>();

        foreach (var (kind, pending) in _pending.ToList())
        {
            if (StepFor(kind, pending, frame, now, isUp) is not { } step) continue;

            _pending.Remove(kind);
            steps.Add(step);
        }

        return steps;
    }

    /// <summary>Never in the frame the game opened it, as the game is still inside that open.</summary>
    private static CloseStep? StepFor(PopupKind kind, Pending pending, int frame, double now, Func<PopupKind, bool> isUp)
    {
        if (frame <= pending.Frame) return null;

        if (isUp(kind)) return new CloseStep.Close(kind, frame - pending.Frame);

        return now - pending.Since >= GiveUpSeconds ? new CloseStep.GiveUp(kind) : null;
    }

    private readonly record struct Pending(int Frame, double Since);
}
