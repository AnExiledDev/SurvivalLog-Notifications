using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.HotUpdate;
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
        foreach (var step in _pending.Due(Time.frameCount, Time.unscaledTime, SightOf))
        {
            switch (step)
            {
                case CloseStep.Close close:
                    Close(close.Kind, close.Line);

                    break;
                case CloseStep.CloseAgain again:
                    Close(again.Kind, again.Line);

                    break;
                case CloseStep.Settled settled:
                    _log($"{settled.Line}; {Afterwards()}");

                    break;
                case CloseStep.LeftVisible left:
                    _log($"{left.Line}; {Afterwards()}");

                    break;
                case CloseStep.Quiet quiet:
                    _log($"{quiet.Line}; {Afterwards()}");

                    break;
                case CloseStep.GiveUp giveUp:
                    _log(giveUp.Line);

                    break;
            }
        }
    }

    private void Close(PopupKind kind, string line)
    {
        var openBefore = OpenUiNames();

        try
        {
            SendClose(kind);
            _log($"{line} (open when sent: {openBefore})");
        }
        catch (Exception ex)
        {
            Fail("closing the " + PopupKinds.Name(kind) + ", which stays up", ex);
        }
    }

    /// <summary>What is up after a close, to tell a stuck screen from a clean one in the log.</summary>
    private string Afterwards()
    {
        var upKinds = PopupKinds.All.Where(kind => SightOf(kind) == PopupSight.Full).Select(PopupKinds.Name).ToList();
        var popups = upKinds.Count == 0 ? "none" : string.Join(", ", upKinds);

        return $"popups a toast stands in for still up: {popups}; received-items window: {ItemsWindow()}; " +
            $"a window that blocks hotkeys is up: {IsModalUp()}; open: {OpenUiNames()}";
    }

    /// <summary>
    /// The wave report does what its OK button does: opening it paused the battle, so the battle is resumed, then the
    /// report closed. The close alone left the game paused with no report showing.
    /// </summary>
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
                EventDispatcherTools.NotifyGlobal(EnumNotify_Lifecycle.ResumeBattleLogic); // as Reducer_Web_TowerWaveReport.RA_Confirm
                Ac_TowerWaveReport_WebClose.SendAction(); // 0x2807100

                break;
            case PopupKind.Skill:
                Ac_SkillLevelUpPopup_Close.SendAction(); // 0x2801990

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    /// <summary>
    /// A window that cannot be asked about counts as down, so nothing is closed blind. The received-items window is the
    /// full popup only while it shows a presentation a toast stands in for: the small pickup pops are the game's, and a
    /// presentation that cannot be read counts as one of them.
    /// </summary>
    private PopupSight SightOf(PopupKind kind)
    {
        try
        {
            if (GameSingleton.Existing<ReduxUISystem>() is not { } ui || !ui.IsUIActive(WebUiOf(kind))) return PopupSight.Down;

            if (kind != PopupKind.Items) return PopupSight.Full;

            return ItemsPresentation(ui) is { } presentation && ItemPopup.IsFull(presentation) ? PopupSight.Full : PopupSight.Other;
        }
        catch (Exception ex)
        {
            Fail("asking whether the " + PopupKinds.Name(kind) + " is up", ex);

            return PopupSight.Down;
        }
    }

    /// <summary>What the received-items window shows: "closed", the game's presentation name, or "unreadable".</summary>
    private string ItemsWindow()
    {
        try
        {
            if (GameSingleton.Existing<ReduxUISystem>() is not { } ui) return "unknown";

            if (!ui.IsUIActive(WebUiOf(PopupKind.Items))) return "closed";

            return ItemsPresentation(ui) ?? "unreadable";
        }
        catch (Exception ex)
        {
            Fail("reading the received-items window", ex);

            return "unreadable";
        }
    }

    /// <summary>The game stores "modal" for an open sent with no presentation, so an open window always names one.</summary>
    private static string? ItemsPresentation(ReduxUISystem ui) =>
        ui.GetStateByUI(WebUiOf(PopupKind.Items))?.TryCast<State_Web_RewardList>()?.Presentation?.Value;

    /// <summary>The game's own test for a window that keeps hotkeys from working: any open window Esc can close.</summary>
    private string IsModalUp()
    {
        try
        {
            return GameSingleton.Existing<ReduxUISystem>()?.HasModalUIOpenExcept(null) switch
            {
                true => "yes",
                false => "no",
                null => "unknown",
            };
        }
        catch (Exception ex)
        {
            Fail("asking whether a window that blocks hotkeys is up", ex);

            return "unknown";
        }
    }

    /// <summary>The game's UI names for what is open.</summary>
    private string OpenUiNames()
    {
        try
        {
            if (GameSingleton.Existing<ReduxUISystem>() is not { } ui) return "unknown";

            var names = new Il2CppSystem.Collections.Generic.List<string>();
            var listed = new List<string>();

            ui.GetOpenUITypeNames(names);

            for (var i = 0; i < names.Count; i++) listed.Add(names[i]);

            return listed.Count == 0 ? "none" : string.Join(", ", listed);
        }
        catch (Exception ex)
        {
            Fail("listing the open windows", ex);

            return "unreadable";
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
