using System.Text.Json;
using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

public class PhoneViewTests
{
    [Fact]
    public void TheListGetsEveryRowNewestFirstWithItsCountTimeAndWhetherItCanBeClicked()
    {
        var history = new History();

        history.Record(Make.Toast(1, "older"), new GameStamp(3, 7), Make.Wish());
        history.Record(Make.Toast(2, "stamina"), null, null);
        history.Record(Make.Toast(2, "stamina", isMerge: true), null, null);

        using var view = JsonDocument.Parse(PhoneView.Json(history, false));
        var rows = view.RootElement.GetProperty("rows").EnumerateArray().ToList();

        Assert.Equal(new[] { "stamina", "older" }, rows.Select(row => row.GetProperty("title").GetString()));
        Assert.Equal(2, rows[0].GetProperty("count").GetInt32());
        Assert.False(rows[0].GetProperty("click").GetBoolean());
        Assert.False(rows[0].TryGetProperty("day", out _));
        Assert.True(rows[1].GetProperty("click").GetBoolean());
        Assert.Equal(3, rows[1].GetProperty("day").GetInt32());
        Assert.Equal(7, rows[1].GetProperty("hour").GetInt32());
    }

    [Fact]
    public void RowsRaisedAfterTheListWasLastSeenAreUnread()
    {
        var history = new History();

        history.Record(Make.Toast(1, "seen"), null, null);
        history.MarkSeen(history.LatestMark);
        history.Record(Make.Toast(2, "new"), null, null);

        using var view = JsonDocument.Parse(PhoneView.Json(history, false));
        var rows = view.RootElement.GetProperty("rows").EnumerateArray().ToList();

        Assert.Equal(1, view.RootElement.GetProperty("unread").GetInt32());
        Assert.Equal(history.LatestMark, view.RootElement.GetProperty("mark").GetInt64());
        Assert.True(rows[0].GetProperty("unread").GetBoolean());
        Assert.False(rows[1].GetProperty("unread").GetBoolean());
    }

    [Fact]
    public void TheAppIsNamedInThePlayersLanguage()
    {
        using var english = JsonDocument.Parse(PhoneView.Json(null, false));
        using var chinese = JsonDocument.Parse(PhoneView.Json(null, true));

        Assert.Equal("Notifications", english.RootElement.GetProperty("label").GetString());
        Assert.Equal("通知", chinese.RootElement.GetProperty("label").GetString());
    }

    [Fact]
    public void TheViewCanBePastedIntoScriptAsItIs()
    {
        var history = new History();

        history.Record(new ToastNote(1, new Note("Mod", "</script><b>'x'</b>", "无人机" + (char)0x2028, NoteKind.Info), Make.T0, false, false), null, null);

        var json = PhoneView.Json(history, true);

        Assert.All(json, character => Assert.InRange(character, (char)32, (char)126));
        Assert.DoesNotContain("<", json);
        Assert.DoesNotContain("'", json);
    }

    [Fact]
    public void OutOfAGameTheListIsEmpty()
    {
        using var view = JsonDocument.Parse(PhoneView.Json(null, false));

        Assert.Equal(0, view.RootElement.GetProperty("rows").GetArrayLength());
    }
}

public class PhoneSyncTests
{
    [Fact]
    public void TheScriptGoesInFirst() => Assert.Equal(PhoneStep.Install, new PhoneSync().Next(0, 1));

    [Fact]
    public void AChangedHistoryIsSentButNotMoreOftenThanTheUpdateInterval()
    {
        var sync = new PhoneSync();

        sync.Next(0, 1);

        Assert.Equal(PhoneStep.Wait, sync.Next(PhoneSync.UpdateSeconds / 2, 2));
        Assert.Equal(PhoneStep.Update, sync.Next(PhoneSync.UpdateSeconds, 2));
    }

    [Fact]
    public void AnUnchangedHistoryOnlyProbesForTheScriptNowAndThen()
    {
        var sync = new PhoneSync();

        sync.Next(0, 1);

        Assert.Equal(PhoneStep.Wait, sync.Next(PhoneSync.ProbeSeconds - 0.1, 1));
        Assert.Equal(PhoneStep.Probe, sync.Next(PhoneSync.ProbeSeconds, 1));
        Assert.Equal(PhoneStep.Wait, sync.Next(PhoneSync.ProbeSeconds + 0.1, 1));
    }

    [Fact]
    public void AScriptLostToAReloadIsPutBack()
    {
        var sync = new PhoneSync();

        sync.Next(0, 1);
        sync.ScriptMissing();

        Assert.Equal(PhoneStep.Install, sync.Next(PhoneSync.UpdateSeconds, 1));
    }

    [Fact]
    public void WithNoWebViewTheScriptIsTriedAgainAfterAWait()
    {
        var sync = new PhoneSync();

        sync.Next(0, 1);
        sync.NotSent(0);

        Assert.Equal(PhoneStep.Wait, sync.Next(PhoneSync.ProbeSeconds - 0.1, 1));
        Assert.Equal(PhoneStep.Install, sync.Next(PhoneSync.ProbeSeconds, 1));
    }
}

public class PageMessageTests
{
    [Fact]
    public void OnlyMessagesWithThisModsPrefixAreTaken()
    {
        Assert.True(PageMessage.IsOurs(PageMessage.Prefix + "noscript"));
        Assert.False(PageMessage.IsOurs("slmod|toolkit|click|1"));
        Assert.False(PageMessage.IsOurs(null));
    }

    [Theory]
    [InlineData("open|12")]
    [InlineData("seen|5")]
    [InlineData("noscript")]
    [InlineData("diag|frame gone")]
    public void ThePageScriptsMessagesAreUnderstood(string message) =>
        Assert.IsNotType<PageMessage.Unknown>(PageMessage.Parse(PageMessage.Prefix + message));

    [Fact]
    public void ARowClickNamesItsRowAndSeeingTheListNamesItsMark()
    {
        Assert.Equal(new PageMessage.Open(12), PageMessage.Parse(PageMessage.Prefix + "open|12"));
        Assert.Equal(new PageMessage.Seen(5), PageMessage.Parse(PageMessage.Prefix + "seen|5"));
    }

    [Theory]
    [InlineData("open|-1")]
    [InlineData("open|abc")]
    [InlineData("seen|")]
    [InlineData("dance")]
    public void AMalformedMessageIsUnknown(string message) => Assert.IsType<PageMessage.Unknown>(PageMessage.Parse(PageMessage.Prefix + message));

    [Fact]
    public void SettingTheViewAsksForTheScriptWhenAReloadLostIt()
    {
        Assert.Contains("noscript", PageCalls.Set("{}"));
        Assert.StartsWith("window.__slnotify?window.__slnotify.set({})", PageCalls.Set("{}"));
    }
}
