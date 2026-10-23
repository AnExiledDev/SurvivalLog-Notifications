using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using Notifications.Core;

namespace Notifications.Adapters;

/// <summary>
/// The neighbour's or companion's basket coming back raises "The basket is back". The game shows no popup for it,
/// only a text message on the phone, so this is a new toast and not a replacement. Static because a Harmony patch has
/// nowhere else to look.
/// </summary>
internal static class BasketHooks
{
    private static NoticeDesk? _desk;
    private static int _scheduledBefore = -1;
    private static SwitchedHook? _settle;

    public static void Apply(Harmony harmony, NoticeDesk desk, Action<string> log)
    {
        _desk = desk;

        // 0x27260f0; the settle that puts her return gifts in the basket, run when the return timeline ends or on the
        // next hour. Private, so it is found by name.
        _settle = new SwitchedHook(() => HookPatch.Apply(harmony, AccessTools.Method(typeof(NeighborBasketReturnManager), "SettleReturn"),
            typeof(BasketHooks), nameof(BeforeSettle), nameof(AfterSettle), "basket returns raise a toast", "a returning basket shows no toast", log));
    }

    /// <summary>Called on the main thread at start and after each change of a switch.</summary>
    public static void Use(Switches switches) => _settle?.Use(switches.BasketBack);

    private static void BeforeSettle(NeighborBasketReturnManager __instance)
    {
        _scheduledBefore = -1;

        if (_desk is not { } desk || !desk.Switches.BasketBack) return;

        try
        {
            if (!IsLive(__instance)) return;

            _scheduledBefore = __instance.pendingReturns?.Count ?? 0;
        }
        catch (Exception ex)
        {
            desk.Fail("reading a returning basket", ex);
        }
    }

    private static void AfterSettle(NeighborBasketReturnManager __instance)
    {
        var before = _scheduledBefore;

        _scheduledBefore = -1;

        if (before < 0 || _desk is not { } desk) return;

        try
        {
            if (!BasketReturn.CameBack(before, __instance.pendingReturns?.Count ?? 0)) return;

            desk.Raise(NoteTexts.BasketBack(new GameWords().IsChinese), null);
        }
        catch (Exception ex)
        {
            desk.Fail("raising a basket toast", ex);
        }
    }

    /// <summary>Whether this basket manager is the live world's, not a leftover one still answering the game's events.</summary>
    private static bool IsLive(NeighborBasketReturnManager manager) =>
        GameSingleton.Existing<BattleLogicWorld>()?._NeighborBasketReturnManager?.Pointer == manager.Pointer;
}
