using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using GameCore.HotUpdate;
using GameCore.HotUpdate.ReduxUI;
using Il2CppInterop.Runtime;
using Notifications.Core;
using GameItemList = Il2CppSystem.Collections.Generic.List<Il2CppSystem.Collections.Generic.KeyValuePair<int, int>>;
using GameItemPair = Il2CppSystem.Collections.Generic.KeyValuePair<int, int>;
using GameIntList = Il2CppSystem.Collections.Generic.List<int>;

namespace Notifications.Adapters;

/// <summary>
/// Opens a game popup again with the data it first had, through the same call the game made. The popup hooks let
/// these calls through while <see cref="IsReopening"/> is set, so a reopened popup is never turned into a toast again.
/// </summary>
internal static class Reopener
{
    public static bool IsReopening { get; private set; }

    /// <summary>Null when the popup was asked for, else why it could not be.</summary>
    public static string? Open(Reopen reopen)
    {
        IsReopening = true;

        try
        {
            return Send(reopen);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            IsReopening = false;
        }
    }

    /// <summary>The level-up info's properties as text, which is how the history keeps them.</summary>
    public static Reopen.Skill SkillOf(ProficiencyLevelUpInfo info) => new(new Dictionary<string, string>
    {
        ["SystemType"] = Text((int)info.SystemType),
        ["OldLevel"] = Text(info.OldLevel),
        ["NewLevel"] = Text(info.NewLevel),
        ["CurrentExp"] = Text(info.CurrentExp),
        ["NextLevelExp"] = Text(info.NextLevelExp),
        ["MaxLevel"] = Text(info.MaxLevel),
        ["SystemName"] = info.SystemName ?? "",
        ["UnlockedAbility"] = info.UnlockedAbility ?? "",
    });

    private static string? Send(Reopen reopen)
    {
        switch (reopen)
        {
            case Reopen.Wish wish:
                Ac_WishFinished_WebOpen.SendAction(wish.ItemConfigId, wish.FaithValue, wish.SurvivalValue, wish.BuffId, wish.WishPoolId);

                return null;
            case Reopen.Items items:
                var gameItems = GameItems(items.Stacks);

                if (Mismatch(gameItems, items.Stacks) is { } mismatch) return mismatch;

                Ac_RewardList_OpenAddItem.SendAction(gameItems, GameKinds(items.Kinds), items.BumperCount, items.SuperCount, items.Presentation, items.Desc);

                return null;
            case Reopen.Wave wave:
                if (LogicAdapter() is not { } waveAdapter) return "the game's UI is not ready";

                waveAdapter.OnTowerWaveReportReady(wave.WaveIndex, wave.KillCount, wave.DeviceDamage, wave.StructureDamage, wave.RewardText);

                return null;
            case Reopen.Skill skill:
                if (LogicAdapter() is not { } skillAdapter) return "the game's UI is not ready";

                skillAdapter.OnSkillLevelUpPopupOpen(SkillInfo(skill));

                return null;
            default:
                return "no popup is known for " + reopen.GetType().Name;
        }
    }

    private static LogicAdapter? LogicAdapter() => GameSingleton.Existing<ReduxUISystem>()?._logicAdapter;

    /// <summary>
    /// The list is written straight into the game's memory: its array of id and amount pairs, then its size. Built
    /// through the interop wrappers (the pair's constructor, then Add), the game read every pair as garbage, so the
    /// popup came up with its title and no items.
    /// </summary>
    private static GameItemList GameItems(IReadOnlyList<ItemStack> stacks)
    {
        var list = new GameItemList();
        var listPtr = IL2CPP.Il2CppObjectBaseToPtrNotNull(list);
        var pairClass = Il2CppClassPointerStore<GameItemPair>.NativeClassPtr;

        if (pairClass == IntPtr.Zero) throw new InvalidOperationException("the game's item pair type was not found");

        var array = IL2CPP.il2cpp_array_new(pairClass, (ulong)stacks.Count);
        var size = IL2CPP.il2cpp_array_element_size(IL2CPP.il2cpp_object_get_class(array));

        if (size != PairSize) throw new InvalidOperationException($"an item pair takes {size} bytes, not {PairSize}");

        var data = ArrayData(array);

        for (var i = 0; i < stacks.Count; i++)
        {
            Marshal.WriteInt32(data, i * PairSize, stacks[i].ConfigId);
            Marshal.WriteInt32(data, i * PairSize + 4, stacks[i].Count);
        }

        IL2CPP.il2cpp_gc_wbarrier_set_field(listPtr, listPtr + ListField(listPtr, "_items"), array);
        Marshal.WriteInt32(listPtr + ListField(listPtr, "_size"), stacks.Count);

        return list;
    }

    /// <summary>The list as the game will read it, checked against the history, so an empty popup is never opened.</summary>
    private static string? Mismatch(GameItemList list, IReadOnlyList<ItemStack> stacks)
    {
        var listPtr = IL2CPP.Il2CppObjectBaseToPtrNotNull(list);
        var count = Marshal.ReadInt32(listPtr + ListField(listPtr, "_size"));

        if (count != stacks.Count) return $"the item list holds {count} of {stacks.Count} items";

        var data = ArrayData(Marshal.ReadIntPtr(listPtr + ListField(listPtr, "_items")));

        for (var i = 0; i < stacks.Count; i++)
        {
            var id = Marshal.ReadInt32(data, i * PairSize);
            var amount = Marshal.ReadInt32(data, i * PairSize + 4);

            if (id != stacks[i].ConfigId || amount != stacks[i].Count)
                return $"item {i + 1} reads as {id} x{amount}, not {stacks[i].ConfigId} x{stacks[i].Count}";
        }

        return null;
    }

    /// <summary>A pair of two ints: the item id, then the amount.</summary>
    private const int PairSize = 8;

    /// <summary>An array's elements start after its object header, bounds and length.</summary>
    private static IntPtr ArrayData(IntPtr array) => array + 4 * IntPtr.Size;

    private static int ListField(IntPtr listPtr, string name)
    {
        var field = IL2CPP.GetIl2CppField(IL2CPP.il2cpp_object_get_class(listPtr), name);

        if (field == IntPtr.Zero) throw new InvalidOperationException($"the game's item list has no {name}");

        return (int)IL2CPP.il2cpp_field_get_offset(field);
    }

    private static GameIntList? GameKinds(IReadOnlyList<int>? kinds)
    {
        if (kinds == null) return null;

        var list = new GameIntList();

        foreach (var kind in kinds) list.Add(kind);

        return list;
    }

    private static ProficiencyLevelUpInfo SkillInfo(Reopen.Skill skill)
    {
        var info = new ProficiencyLevelUpInfo
        {
            SystemType = (ProficiencyType)Number(skill, "SystemType"),
            OldLevel = Number(skill, "OldLevel"),
            NewLevel = Number(skill, "NewLevel"),
            CurrentExp = Number(skill, "CurrentExp"),
            NextLevelExp = Number(skill, "NextLevelExp"),
            MaxLevel = Number(skill, "MaxLevel"),
            SystemName = skill.Fields.TryGetValue("SystemName", out var name) ? name : "",
            UnlockedAbility = skill.Fields.TryGetValue("UnlockedAbility", out var ability) ? ability : "",
        };

        return info;
    }

    private static int Number(Reopen.Skill skill, string field) =>
        skill.Fields.TryGetValue(field, out var text) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);
}
