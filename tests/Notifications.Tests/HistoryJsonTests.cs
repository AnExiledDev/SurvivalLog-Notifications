using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

public class HistoryJsonTests
{
    [Fact]
    public void EveryKindOfPopupComesBackFromTheFileAsItWasSaved()
    {
        var skill = new Reopen.Skill(new Dictionary<string, string> { ["SystemType"] = "3", ["NewLevel"] = "4", ["SystemName"] = "烹饪" });
        var items = new Reopen.Items(Make.Cargo((10, 2), (11, 1)), new[] { 1, 2 }, 1, 0, "newDish", "Unloaded");
        var wave = new Reopen.Wave(3, 40, 5, 6, "<b>food</b>");
        var history = new History();

        history.Record(Make.Toast(1, "wish"), new GameStamp(2, 7), Make.Wish(9));
        history.Record(Make.Toast(2, "items"), null, items);
        history.Record(Make.Toast(3, "wave"), null, wave);
        history.Record(Make.Toast(4, "skill"), null, skill);

        var loaded = Load(HistoryJson.Write(history));

        Assert.Equal(new[] { Make.Wish(9), items, wave, (Reopen)skill }, loaded.Entries.Reverse().Select(entry => entry.Reopen));
        Assert.Equal(new GameStamp(2, 7), loaded.Entries[^1].Stamp);
    }

    [Fact]
    public void WhatTheListShowsSurvivesARestart()
    {
        var history = new History();

        history.RecordLocal(new Note("游戏", "体力不足", "body", NoteKind.Warning), Make.T0, null, null);
        history.RecordLocal(new Note("游戏", "体力不足", "body", NoteKind.Warning), Make.T0.AddSeconds(2), null, null);
        history.Record(Make.Toast(5, "later"), null, null);
        history.MarkSeen(1);

        var loaded = Load(HistoryJson.Write(history));
        var stamina = loaded.Entries[1];

        Assert.Equal(new Note("游戏", "体力不足", "body", NoteKind.Warning), stamina.Content);
        Assert.Equal(2, stamina.Count);
        Assert.Equal(Make.T0, stamina.FirstUtc);
        Assert.Equal(Make.T0.AddSeconds(2), stamina.LastUtc);
        Assert.Equal(history.SeenMark, loaded.SeenMark);
        Assert.Equal(history.Next, loaded.Next);
        Assert.Equal(history.UnreadCount, loaded.UnreadCount);
    }

    [Fact]
    public void AToastsClickActionDoesNotSurviveARestartButItsPopupDoes()
    {
        var history = new History();

        history.Record(Make.Toast(5, "mod notice", hasClickAction: true), null, null);
        history.Record(Make.Toast(6, "wish", hasClickAction: true), null, Make.Wish());

        var loaded = Load(HistoryJson.Write(history));

        Assert.True(loaded.Entries[0].IsClickable);
        Assert.False(loaded.Entries[1].IsClickable);
        Assert.All(loaded.Entries, entry => Assert.Equal(0, entry.ToastId));
    }

    [Fact]
    public void TheFileIsPlainAscii()
    {
        var history = new History();

        history.Record(new ToastNote(1, new Note("游戏", "无人机送货到了", "「木头」", NoteKind.Info), Make.T0, false, false), null, null);

        Assert.All(HistoryJson.Write(history), character => Assert.InRange(character, (char)0, (char)127));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    [InlineData("{\"version\":2,\"entries\":[]}")]
    [InlineData("{\"version\":1,\"entries\":{}}")]
    public void AFileThisVersionCannotReadIsCorrupt(string text) => Assert.IsType<HistoryRead.Corrupt>(HistoryJson.Read(text));

    [Fact]
    public void AnUnreadableEntryIsSkippedAndTheRestLoad()
    {
        const string text = "{\"version\":1,\"next\":5,\"seen\":0,\"entries\":[{\"seq\":2,\"source\":\"Game\",\"title\":\"kept\"},{\"seq\":\"x\"},7]}";

        var loaded = Assert.IsType<HistoryRead.Loaded>(HistoryJson.Read(text));

        Assert.Equal(new[] { "kept" }, Make.Titles(loaded.History));
        Assert.Equal(2, loaded.SkippedEntries);
    }

    [Fact]
    public void APopupOfAnUnknownKindLeavesTheEntryReadableButNotClickable()
    {
        const string text = "{\"version\":1,\"entries\":[{\"seq\":1,\"source\":\"Game\",\"title\":\"t\",\"reopen\":{\"type\":\"future\"}}]}";

        var entry = Load(text).Entries.Single();

        Assert.Equal("t", entry.Content.Title);
        Assert.False(entry.IsClickable);
    }

    private static History Load(string text) => Assert.IsType<HistoryRead.Loaded>(HistoryJson.Read(text)).History;

    [Fact]
    public void ARowsReadStateAndPictureSurviveARestart()
    {
        var history = new History();

        history.Record(new ToastNote(1, Make.Notice("a"), Make.T0, false, false) { Icon = "Res/Item/wood.png" }, null, null);
        history.Record(Make.Toast(2, "b"), null, null);
        history.MarkRead(history.Entries[1].Seq);

        var loaded = Load(HistoryJson.Write(history));

        Assert.Equal("Res/Item/wood.png", loaded.Entries[1].Icon);
        Assert.Null(loaded.Entries[0].Icon);
        Assert.False(loaded.IsUnread(loaded.Entries[1]));
        Assert.True(loaded.IsUnread(loaded.Entries[0]));
    }
}
