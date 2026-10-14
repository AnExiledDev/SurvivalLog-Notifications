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
/// The game popups that only show something and close: each becomes a toast that opens it again when clicked. A prefix
/// skips the game's popup only once the toast is up; anything else, a failure included, lets the game open it. The
/// popups that ask the player to choose (events, and picking items up with Pick All) are never hooked. Static because a
/// Harmony patch has nowhere else to look.
/// </summary>
internal static class PopupHooks
{
    private static NoticeDesk? _desk;

    public static void Apply(Harmony harmony, NoticeDesk desk, Action<string> log)
    {
        _desk = desk;

        var switches = desk.Switches;

        if (switches.WishFulfilled)
        {
            // 0x280c850; the game's own OnWishCompleted (0x2825300) jumps within its first 14 bytes and cannot be hooked.
            HookPatch.Apply(harmony, AccessTools.Method(typeof(Ac_WishFinished_WebOpen), nameof(Ac_WishFinished_WebOpen.SendAction)), typeof(PopupHooks), nameof(BeforeWish), null,
                "wish popups become toasts", "they stay popups", log);
        }

        if (switches.ItemsReceived || switches.DroneDelivery)
        {
            // 0x27f7750, the overload the others call; OnOpenAddItemPop (0x281f2e0) and its bumper twin jump too early.
            var addItem = AccessTools.Method(typeof(Ac_RewardList_OpenAddItem), nameof(Ac_RewardList_OpenAddItem.SendAction),
                new[] { typeof(GameItemList), typeof(GameIntList), typeof(int), typeof(int), typeof(string), typeof(string) });

            HookPatch.Apply(harmony, addItem, typeof(PopupHooks), nameof(BeforeItems), null, "received-items popups become toasts", "they stay popups", log);
        }

        if (switches.WaveReport)
        {
            // 0x2823e60
            HookPatch.Apply(harmony, AccessTools.Method(typeof(LogicAdapter), nameof(LogicAdapter.OnTowerWaveReportReady)), typeof(PopupHooks), nameof(BeforeWave), null,
                "zombie wave reports become toasts", "they stay popups", log);
        }

        if (switches.SkillLevelUp)
        {
            // 0x2822280
            HookPatch.Apply(harmony, AccessTools.Method(typeof(LogicAdapter), nameof(LogicAdapter.OnSkillLevelUpPopupOpen)), typeof(PopupHooks), nameof(BeforeSkill), null,
                "skill level-up popups become toasts", "they stay popups", log);
        }
    }

    private static bool BeforeWish(int ItemConfigId, int FaithValue, int SurvivalValue, int BuffId, int WishPoolId)
    {
        if (Reopener.IsReopening || _desk is not { } desk) return true;

        return desk.IsGameShowing("wish popup", desk.ForPopup(desk.Switches.WishFulfilled), () =>
        {
            var wish = new Reopen.Wish(ItemConfigId, FaithValue, SurvivalValue, BuffId, WishPoolId);
            var words = new GameWords();

            return (NoteTexts.Wish(words, wish, words.BuffName(BuffId)), wish);
        });
    }

    /// <summary>A drone's unload goes to the drone's toast first; any other received-items popup follows its own switch.</summary>
    private static bool BeforeItems(GameItemList ItemGetList, GameIntList ItemKinds, int BumperCount, int SuperCount, string Presentation, string Desc)
    {
        if (Reopener.IsReopening || _desk is not { } desk) return true;

        Reopen.Items items;

        try
        {
            items = ReadItems(ItemGetList, ItemKinds, BumperCount, SuperCount, Presentation, Desc);
        }
        catch (Exception ex)
        {
            desk.Fail("reading a received-items popup", ex);

            return true;
        }

        if (items.Stacks.Count == 0) return true;

        if (DroneHooks.TakeUnload(items)) return false;

        return desk.IsGameShowing("received-items popup", desk.ForPopup(desk.Switches.ItemsReceived), () => (NoteTexts.Items(new GameWords(), items), items));
    }

    private static bool BeforeWave(int waveIndex, int killCount, int deviceDamage, int structureDamage, string rewardText)
    {
        if (Reopener.IsReopening || _desk is not { } desk) return true;

        return desk.IsGameShowing("zombie wave report", desk.ForPopup(desk.Switches.WaveReport), () =>
        {
            var wave = new Reopen.Wave(waveIndex, killCount, deviceDamage, structureDamage, rewardText);

            return (NoteTexts.Wave(new GameWords(), wave), wave);
        });
    }

    private static bool BeforeSkill(ProficiencyLevelUpInfo info)
    {
        if (Reopener.IsReopening || _desk is not { } desk) return true;

        return desk.IsGameShowing("skill level-up popup", desk.ForPopup(desk.Switches.SkillLevelUp), () =>
        {
            if (info == null) return null;

            var skill = Reopener.SkillOf(info);

            return (NoteTexts.Skill(new GameWords(), skill), skill);
        });
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
