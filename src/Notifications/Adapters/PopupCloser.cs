using System;
using System.Collections.Generic;
using GameCore.HotUpdate.ReduxUI;
using Il2CppInterop.Runtime;
using Notifications.Core;
using UnityEngine;

namespace Notifications.Adapters;

/// <summary>
/// Closes the game popups a toast stood in for, once the game has them up, through the same action their own Close or
/// OK button sends, so the game finishes whatever was waiting on them. Runs on the game's main thread, from the ticker.
/// </summary>
internal sealed class PopupCloser
{
    private readonly PendingCloses _pending = new();
    private readonly Action<string> _log;
    private readonly HashSet<string> _reportedFailures = new();

    public PopupCloser(Action<string> log) => _log = log;

    /// <summary>A toast stood in for this popup, which the game is opening now.</summary>
    public void CloseOnceOpen(PopupKind kind) => _pending.Expect(kind, Time.frameCount, Time.unscaledTime);

    /// <summary>The popup stays up for the player. Whether a close was pending.</summary>
    public bool KeepOpen(PopupKind kind) => _pending.Cancel(kind);

    public void Tick()
    {
        foreach (var step in _pending.Due(Time.frameCount, Time.unscaledTime, IsUp))
        {
            switch (step)
            {
                case CloseStep.Close close:
                    Close(close);

                    break;
                case CloseStep.GiveUp giveUp:
                    _log(giveUp.Line);

                    break;
            }
        }
    }

    private void Close(CloseStep.Close close)
    {
        try
        {
            SendClose(close.Kind);
            _log(close.Line);
        }
        catch (Exception ex)
        {
            Fail("closing the " + PopupKinds.Name(close.Kind) + ", which stays up", ex);
        }
    }

    /// <summary>The wave report gets the close Esc sends: its OK button's action sends that close from inside a reducer.</summary>
    private static void SendClose(PopupKind kind)
    {
        switch (kind)
        {
            case PopupKind.Wish:
                Ac_WishFinished_WebClose.SendAction(); // 0x280c750

                break;
            case PopupKind.Items:
                Ac_RewardList_CloseUI.SendAction(); // 0x27f7510

                break;
            case PopupKind.Wave:
                Ac_TowerWaveReport_WebClose.SendAction(); // 0x2807100

                break;
            case PopupKind.Skill:
                Ac_SkillLevelUpPopup_Close.SendAction(); // 0x2801990

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    /// <summary>A popup that cannot be asked about counts as not up, so nothing is closed blind.</summary>
    private bool IsUp(PopupKind kind)
    {
        try
        {
            return GameSingleton.Existing<ReduxUISystem>()?.IsUIActive(WebUiOf(kind)) == true;
        }
        catch (Exception ex)
        {
            Fail("asking whether the " + PopupKinds.Name(kind) + " is up", ex);

            return false;
        }
    }

    private static Il2CppSystem.Type WebUiOf(PopupKind kind) => kind switch
    {
        PopupKind.Wish => Il2CppType.Of<WebUI_WishFinished>(),
        PopupKind.Items => Il2CppType.Of<WebUI_RewardList>(),
        PopupKind.Wave => Il2CppType.Of<WebUI_TowerWaveReport>(),
        PopupKind.Skill => Il2CppType.Of<WebUI_SkillLevelUpPopup>(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    private void Fail(string doing, Exception ex)
    {
        if (_reportedFailures.Add(doing)) _log($"failed {doing} (logged once): {ex}");
    }
}
