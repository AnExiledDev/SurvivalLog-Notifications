using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using Notifications.Core;
using GameItemList = Il2CppSystem.Collections.Generic.List<Il2CppSystem.Collections.Generic.KeyValuePair<int, int>>;
using GameIntList = Il2CppSystem.Collections.Generic.List<int>;

namespace Notifications.Adapters;

/// <summary>
/// The game popups that only show something and close: each becomes a toast that opens it again when clicked. The game
/// always opens its popup, as skipping it left the game waiting on a popup that never closed (an eaten craved food never
/// finished); once the toast is up, the popup is closed again through the game's own close action. Anything else, a
/// failure included, leaves the popup up. The popups that ask the player to choose (events, and picking items up with
/// Pick All) are never hooked. Static because a Harmony patch has nowhere else to look.
/// </summary>
internal static class PopupHooks
{
    private static NoticeDesk? _desk;
    private static SwitchedHook? _wish;
    private static SwitchedHook? _items;
    private static SwitchedHook? _wave;
    private static SwitchedHook? _skill;

    public static void Apply(Harmony harmony, NoticeDesk desk, Action<string> log)
    {
        _desk = desk;

        // 0x280c850; the game's own OnWishCompleted (0x2825300) jumps within its first 14 bytes and cannot be hooked.
        _wish = new SwitchedHook(() => HookPatch.Apply(harmony, AccessTools.Method(typeof(Ac_WishFinished_WebOpen), nameof(Ac_WishFinished_WebOpen.SendAction)), typeof(PopupHooks),
            nameof(BeforeWish), null, "wish popups become toasts", "they stay popups", log));

        // 0x27f7750, the overload the others call; OnOpenAddItemPop (0x281f2e0) and its bumper twin jump too early.
        _items = new SwitchedHook(() => HookPatch.Apply(harmony, AccessTools.Method(typeof(Ac_RewardList_OpenAddItem), nameof(Ac_RewardList_OpenAddItem.SendAction),
                new[] { typeof(GameItemList), typeof(GameIntList), typeof(int), typeof(int), typeof(string), typeof(string) }), typeof(PopupHooks),
            nameof(BeforeItems), null, "received-items popups become toasts", "they stay popups", log));

        // 0x2823e60
        _wave = new SwitchedHook(() => HookPatch.Apply(harmony, AccessTools.Method(typeof(LogicAdapter), nameof(LogicAdapter.OnTowerWaveReportReady)), typeof(PopupHooks),
            nameof(BeforeWave), null, "zombie wave reports become toasts", "they stay popups", log));

        // 0x2822280
        _skill = new SwitchedHook(() => HookPatch.Apply(harmony, AccessTools.Method(typeof(LogicAdapter), nameof(LogicAdapter.OnSkillLevelUpPopupOpen)), typeof(PopupHooks),
            nameof(BeforeSkill), null, "skill level-up popups become toasts", "they stay popups", log));
    }

    /// <summary>Called on the main thread at start and after each change of a switch.</summary>
    public static void Use(Switches switches)
    {
        _wish?.Use(switches.WishFulfilled);
        _items?.Use(switches.ItemsReceived || switches.DroneDelivery);
        _wave?.Use(switches.WaveReport);
        _skill?.Use(switches.SkillLevelUp);
    }

    private static void BeforeWish(int ItemConfigId, int FaithValue, int SurvivalValue, int BuffId, int WishPoolId)
    {
        if (Reopener.IsReopening || _desk is not { } desk) return;

        StandIn(desk, PopupKind.Wish, desk.Switches.WishFulfilled, () =>
        {
            var wish = new Reopen.Wish(ItemConfigId, FaithValue, SurvivalValue, BuffId, WishPoolId);
            var words = new GameWords();

            return (NoteTexts.Wish(words, wish, words.BuffName(BuffId)), wish);
        });
    }

    /// <summary>
    /// Only full popups are taken; the small pickup pops stay the game's. A drone's unload goes to the drone's toast
    /// first; any other received-items popup follows its own switch.
    /// </summary>
    private static void BeforeItems(GameItemList ItemGetList, GameIntList ItemKinds, int BumperCount, int SuperCount, string Presentation, string Desc)
    {
        if (Reopener.IsReopening || _desk is not { } desk || !ItemPopup.IsFull(Presentation)) return;

        Reopen.Items items;

        try
        {
            items = ReadItems(ItemGetList, ItemKinds, BumperCount, SuperCount, Presentation, Desc);
        }
        catch (Exception ex)
        {
            desk.Fail("reading a received-items popup", ex);

            return;
        }

        if (items.Stacks.Count == 0) return;

        if (DroneHooks.TakeUnload(items)) desk.CloseOnceOpen(PopupKind.Items);
        else StandIn(desk, PopupKind.Items, desk.Switches.ItemsReceived, () => (NoteTexts.Items(new GameWords(), items), items));
    }

    private static void BeforeWave(int waveIndex, int killCount, int deviceDamage, int structureDamage, string rewardText)
    {
        if (Reopener.IsReopening || _desk is not { } desk) return;

        StandIn(desk, PopupKind.Wave, desk.Switches.WaveReport, () =>
        {
            var wave = new Reopen.Wave(waveIndex, killCount, deviceDamage, structureDamage, rewardText);

            return (NoteTexts.Wave(new GameWords(), wave), wave);
        });
    }

    private static void BeforeSkill(ProficiencyLevelUpInfo info)
    {
        if (Reopener.IsReopening || _desk is not { } desk) return;

        StandIn(desk, PopupKind.Skill, desk.Switches.SkillLevelUp, () =>
        {
            if (info == null) return null;

            var skill = Reopener.SkillOf(info);

            return (NoteTexts.Skill(new GameWords(), skill), skill);
        });
    }

    /// <summary>Raises the toast; when it stood in for the popup the game is about to open, that popup is closed once up.</summary>
    private static void StandIn(NoticeDesk desk, PopupKind kind, bool isSwitchOn, Func<(Note Note, Reopen? Reopen)?> build)
    {
        if (!desk.IsGameShowing(PopupKinds.Name(kind), desk.ForPopup(isSwitchOn), build)) desk.CloseOnceOpen(kind);
    }

    private static Reopen.Items ReadItems(GameItemList? list, GameIntList? kinds, int bumperCount, int superCount, string? presentation, string? desc)
    {
        var stacks = new List<ItemStack>();

        if (list != null)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var pair = list[i];

                stacks.Add(new ItemStack(pair.Key, pair.Value));
            }
        }

        List<int>? kindList = null;

        if (kinds != null)
        {
            kindList = new List<int>();

            for (var i = 0; i < kinds.Count; i++) kindList.Add(kinds[i]);
        }

        return new Reopen.Items(stacks, kindList, bumperCount, superCount, presentation, desc);
    }
}
