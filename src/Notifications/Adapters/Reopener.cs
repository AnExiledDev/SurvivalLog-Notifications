using System;
using System.Collections.Generic;
using System.Globalization;
using GameCore.HotUpdate;
using GameCore.HotUpdate.ReduxUI;
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
                Ac_RewardList_OpenAddItem.SendAction(GameItems(items.Stacks), GameKinds(items.Kinds), items.BumperCount, items.SuperCount, items.Presentation, items.Desc);

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

    private static GameItemList GameItems(IReadOnlyList<ItemStack> stacks)
    {
        var list = new GameItemList();

        foreach (var stack in stacks) list.Add(new GameItemPair(stack.ConfigId, stack.Count));

        return list;
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
