using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Notifications.Core;

/// <summary>The game's own words, as the adapter reads them. Each lookup is null when the game has no text for it.</summary>
public interface IGameWords
{
    bool IsChinese { get; }

    /// <summary>A text from the game's constant text table, by its ID, in the game's language.</summary>
    string? Constant(string id);

    string? ItemName(int configId);
}

/// <summary>One number a popup showed, such as "Kills 38" or "Faith +2". Up is a gain.</summary>
public sealed record NoteStat(string Label, string Value, bool IsUp);

/// <summary>
/// What each notice says. The game's own localized text comes first, so a toast reads like the popup it stands for;
/// the fallbacks here are used only when the game has no text for a line.
/// </summary>
public static class NoteTexts
{
    public const int MostItemsListed = 6;

    private static readonly Regex Tag = new("<[^<>]*>", RegexOptions.Compiled);
    private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);

    public static string Source(bool isChinese) => isChinese ? "游戏" : "Game";

    public static Note Wish(IGameWords words, Reopen.Wish wish, string? buffName)
    {
        var parts = new List<string?>();

        if (wish.ItemConfigId > 0)
        {
            parts.Add(Fill(Word(words, "WishText3", "Finally had the {0} you longed for", "终于吃到了盼望已久的【{0}】"), Name(words, wish.ItemConfigId)));
        }
        else
        {
            parts.Add(Word(words, "WishPool_FinishedText", "Finally did what you always wanted to do.", "终于做了一直想做的事。"));
        }

        if (wish.FaithValue > 0) parts.Add(Fill(Word(words, "SR_UI_WishFinished_0", "Faith +{0}", "信念点+{0}"), Number(wish.FaithValue)));

        if (wish.SurvivalValue > 0) parts.Add(Word(words, "SR_Web_WishFinished_2", "Survival points", "生存点") + " +" + Number(wish.SurvivalValue));

        parts.Add(buffName);

        return new Note(Source(words.IsChinese), Word(words, "SR_Web_WishFinished_0", "Wish fulfilled", "愿望达成"), Join(words, parts), NoteKind.Success);
    }

    public static Note Items(IGameWords words, Reopen.Items items)
    {
        var title = items.Presentation == "newDish"
            ? Word(words, "SR_RewardList_NewDishTitle", "First time cooked", "初次烹成")
            : items.BumperCount > 0
                ? Fill(Word(words, "SR_RewardList_BumperTitle", "Bumper harvest x{0}", "丰收 x{0}"), Number(items.BumperCount))
                : Word(words, "UI_AddItemPop_0", "Items received", "获得物品");

        return new Note(Source(words.IsChinese), title, Join(words, new[] { items.Desc, ItemLines(words, items.Stacks) }), NoteKind.Success);
    }

    public static Note Wave(IGameWords words, Reopen.Wave wave)
    {
        var parts = new List<string?>
        {
            Word(words, "SR_Web_TowerWaveReport_2", "Kills", "击杀") + " " + Number(wave.KillCount),
            Word(words, "SR_Web_TowerWaveReport_3", "Device wear", "装置损耗") + " " + Number(wave.DeviceDamage),
            Word(words, "SR_Web_TowerWaveReport_4", "Door and window wear", "门窗损耗") + " " + Number(wave.StructureDamage),
        };

        if (Plain(wave.RewardText) is { Length: > 0 } reward) parts.Add(Word(words, "SR_Web_TowerWaveReport_5", "Supplies", "补给") + " " + reward);

        var title = Fill(Word(words, "SR_Web_TowerWaveReport_0", "Wave {0} repelled", "第{0}波已击退"), Number(wave.WaveIndex));

        return new Note(Source(words.IsChinese), title, Join(words, parts), NoteKind.Success);
    }

    /// <summary>The level-up popup's own heading and lines: the skill, its new level, then what the level unlocks.</summary>
    public static Note Skill(IGameWords words, Reopen.Skill skill)
    {
        var name = Plain(Field(skill, "SystemName"));
        var level = Plain(Field(skill, "NewLevel"));
        var heading = Word(words, "SR_Web_SkillLevelUpPopup_0", "Skill up", "熟练度提升");
        var title = string.Join(" ", new[] { heading, name, string.IsNullOrEmpty(level) ? null : "Lv." + level }.Where(part => !string.IsNullOrEmpty(part)));
        var lines = (Field(skill, "UnlockedAbility") ?? "").Split('\n').Select(Plain).ToList();

        return new Note(Source(words.IsChinese), title, Join(words, lines), NoteKind.Success);
    }

    public static Note Drone(IGameWords words, IReadOnlyList<ItemStack> cargo) =>
        new(Source(words.IsChinese), words.IsChinese ? "无人机送货到了" : "Drone delivery arrived", ItemLines(words, cargo), NoteKind.Info);

    /// <summary>A game toast's text as one line, or null when nothing is left to show.</summary>
    public static Note? PopText(bool isChinese, string? text) =>
        Plain(text) is { Length: > 0 } line ? new Note(Source(isChinese), line, null, NoteKind.Info) : null;

    public static Note SystemUnlock(IGameWords words, string? systemName, string? tips) =>
        new(Source(words.IsChinese), Word(words, "SR_Web_CoreUI_SystemUnlockCap", "System unlocked", "系统解锁"), Join(words, new[] { systemName, tips }), NoteKind.Success);

    public static Note CharacterUnlock(IGameWords words, string? survivorName) =>
        new(Source(words.IsChinese), Word(words, "SR_Web_CoreUI1_83", "New survivor unlocked", "新幸存者已解锁"),
            Join(words, new[] { survivorName, Word(words, "SR_Web_CoreUI1_84", "Choose them from the main menu when you pick a survivor", "可在主菜单「选择幸存者」中开启") }),
            NoteKind.Success);

    /// <summary>The wish popup's first line: the item longed for, or the thing done.</summary>
    public static string WishLine(IGameWords words, Reopen.Wish wish) =>
        wish.ItemConfigId > 0
            ? Fill(Word(words, "WishText3", "Finally had the {0} you longed for", "终于吃到了盼望已久的【{0}】"), Name(words, wish.ItemConfigId))
            : Word(words, "WishPool_FinishedText", "Finally did what you always wanted to do.", "终于做了一直想做的事。");

    public static IReadOnlyList<NoteStat> WishStats(IGameWords words, Reopen.Wish wish)
    {
        var stats = new List<NoteStat>();

        if (wish.FaithValue > 0)
        {
            var faith = Fill(Word(words, "SR_UI_WishFinished_0", "Faith +{0}", "信念点+{0}"), "").Trim().TrimEnd('+').Trim();

            stats.Add(new NoteStat(faith, "+" + Number(wish.FaithValue), true));
        }

        if (wish.SurvivalValue > 0) stats.Add(new NoteStat(Word(words, "SR_Web_WishFinished_2", "Survival points", "生存点"), "+" + Number(wish.SurvivalValue), true));

        return stats;
    }

    public static IReadOnlyList<NoteStat> WaveStats(IGameWords words, Reopen.Wave wave) => new[]
    {
        new NoteStat(Word(words, "SR_Web_TowerWaveReport_2", "Kills", "击杀"), Number(wave.KillCount), false),
        new NoteStat(Word(words, "SR_Web_TowerWaveReport_3", "Device wear", "装置损耗"), Number(wave.DeviceDamage), false),
        new NoteStat(Word(words, "SR_Web_TowerWaveReport_4", "Door and window wear", "门窗损耗"), Number(wave.StructureDamage), false),
    };

    /// <summary>The supplies line of a wave report, or null when the wave gave none.</summary>
    public static string? WaveSupplies(IGameWords words, Reopen.Wave wave) =>
        Plain(wave.RewardText) is { Length: > 0 } reward ? Word(words, "SR_Web_TowerWaveReport_5", "Supplies", "补给") + " " + reward : null;

    /// <summary>An item's name in the game's language, or its number when the game has no name for it.</summary>
    public static string ItemName(IGameWords words, int configId) => Name(words, configId);

    /// <summary>Up to <see cref="MostItemsListed"/> items as "name xN", then how many more there are.</summary>
    public static string ItemLines(IGameWords words, IReadOnlyList<ItemStack> stacks)
    {
        var shown = stacks.Take(MostItemsListed).Select(stack => Name(words, stack.ConfigId) + " x" + Number(stack.Count)).ToList();
        var hidden = stacks.Count - shown.Count;

        if (hidden > 0) shown.Add(words.IsChinese ? "另有 " + Number(hidden) + " 种" : "+" + Number(hidden) + " more");

        return string.Join(words.IsChinese ? "，" : ", ", shown);
    }

    /// <summary>Text without the game's rich-text tags, on one line, trimmed.</summary>
    public static string? Plain(string? text) => text == null ? null : Spaces.Replace(Tag.Replace(text, ""), " ").Trim();

    /// <summary>Puts values in "{0}", "{1}" places. Unlike string.Format it never throws on a stray brace in game text.</summary>
    public static string Fill(string format, params string?[] values)
    {
        var filled = new StringBuilder(format);

        for (var i = 0; i < values.Length; i++)
        {
            filled.Replace("{" + i.ToString(CultureInfo.InvariantCulture) + "}", values[i] ?? "");
        }

        return filled.ToString();
    }

    private static string Word(IGameWords words, string id, string english, string chinese) =>
        Plain(words.Constant(id)) is { Length: > 0 } text ? text : words.IsChinese ? chinese : english;

    private static string Name(IGameWords words, int configId) =>
        Plain(words.ItemName(configId)) is { Length: > 0 } name ? name : "#" + Number(configId);

    private static string? Field(Reopen.Skill skill, string name) => skill.Fields.TryGetValue(name, out var value) ? value : null;

    private static string? Join(IGameWords words, IEnumerable<string?> parts)
    {
        var kept = parts.Select(Plain).Where(part => !string.IsNullOrEmpty(part)).ToList();

        return kept.Count == 0 ? null : string.Join(words.IsChinese ? "，" : ", ", kept);
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
