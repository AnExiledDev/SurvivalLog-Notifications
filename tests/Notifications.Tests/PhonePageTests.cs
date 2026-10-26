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
    public void EachRowIsFiledAsAPopupAGameMessageOrAModNotice()
    {
        var history = new History();

        history.Record(Make.Toast(1, "wish"), null, Make.Wish());
        history.Record(Make.Toast(2, "message"), null, null);
        history.Record(new ToastNote(3, new Note("Storage Hub", "mod", null, NoteKind.Warning), Make.T0, false, false), null, null);

        var rows = Rows(PhoneView.Json(history, false));

        Assert.Equal(new[] { "mod", "msg", "popup" }, rows.Select(row => row.GetProperty("cat").GetString()));
        Assert.Equal(new[] { false, true, true }, rows.Select(row => row.GetProperty("game").GetBoolean()));
        Assert.Equal(new[] { "plug", "msg", "star" }, rows.Select(row => row.GetProperty("glyph").GetString()));
        Assert.Equal(new[] { "warning", "info", "info" }, rows.Select(row => row.GetProperty("kind").GetString()));
    }

    [Fact]
    public void ReceivedItemsComeWithTheirNamesCountsAndPictures()
    {
        var history = new History();
        var words = new FakeWords();

        words.Items[10] = "Wood";
        words.Icons[10] = "Res/Item/wood.png";
        history.Record(Make.Toast(1, "Received"), null, Make.Items("<b>From</b> the drone", (10, 4), (11, 1)));

        var row = Rows(PhoneView.Json(history, words, null))[0];
        var items = row.GetProperty("items").EnumerateArray().ToList();

        Assert.Equal(new[] { "Wood", NoteTexts.ItemName(words, 11) }, items.Select(item => item.GetProperty("name").GetString()));
        Assert.Equal(new[] { 4, 1 }, items.Select(item => item.GetProperty("count").GetInt32()));
        Assert.Equal("Res/Item/wood.png", items[0].GetProperty("icon").GetString());
        Assert.False(items[1].TryGetProperty("icon", out _));
        Assert.Equal("Res/Item/wood.png", row.GetProperty("icon").GetString());
        Assert.Equal("From the drone", row.GetProperty("body").GetString());
    }

    [Fact]
    public void AWishShowsWhatWasWishedForAndItsGains()
    {
        var history = new History();
        var words = new FakeWords();

        words.Items[5] = "Hot pot";
        words.Buffs[7] = "Content";
        history.Record(Make.Toast(1, "Wish fulfilled"), null, new Reopen.Wish(5, 3, 2, 7, 1));

        var row = Rows(PhoneView.Json(history, words, null))[0];
        var stats = row.GetProperty("stats").EnumerateArray().ToList();

        Assert.Equal("Finally had the Hot pot you longed for, Content", row.GetProperty("body").GetString());
        Assert.Equal(new[] { "Faith", "Survival points" }, stats.Select(stat => stat.GetProperty("label").GetString()));
        Assert.Equal(new[] { "+3", "+2" }, stats.Select(stat => stat.GetProperty("value").GetString()));
        Assert.All(stats, stat => Assert.True(stat.GetProperty("up").GetBoolean()));
    }

    [Fact]
    public void AModsToastKeepsThePictureItWasShownWith()
    {
        var history = new History();

        history.Record(new ToastNote(1, new Note("Cooking Hub", "Dish ready", null, NoteKind.Success), Make.T0, false, false) { Icon = "Res/Item/soup.png" }, null, null);

        Assert.Equal("Res/Item/soup.png", Rows(PhoneView.Json(history, false))[0].GetProperty("icon").GetString());
    }

    [Fact]
    public void TheListKnowsHowManyNoticesTheSaveKeepsAndWhichDayIsToday()
    {
        var history = new History();

        history.Record(Make.Toast(1, "a"), null, null);

        using var view = JsonDocument.Parse(PhoneView.Json(history, new FakeWords(), 12));

        Assert.Equal(1, view.RootElement.GetProperty("kept").GetInt32());
        Assert.Equal(History.Cap, view.RootElement.GetProperty("cap").GetInt32());
        Assert.Equal(12, view.RootElement.GetProperty("today").GetInt32());
        Assert.Equal("Kept with this save: {0} of {1}", view.RootElement.GetProperty("text").GetProperty("kept").GetString());
    }

    [Fact]
    public void OutOfAGameTheListIsEmpty()
    {
        using var view = JsonDocument.Parse(PhoneView.Json(null, false));

        Assert.Equal(0, view.RootElement.GetProperty("rows").GetArrayLength());
    }

    private static List<JsonElement> Rows(string json) => JsonDocument.Parse(json).RootElement.GetProperty("rows").EnumerateArray().ToList();
}

public class PhoneListTests
{
    private readonly History _history = new();
    private readonly FakeWords _words = new();
    private readonly PhoneList _list = new();

    public PhoneListTests()
    {
        _history.Record(Make.Toast(1, "older"), new GameStamp(3, 7), Make.Wish());
        _history.Record(Make.Toast(2, "newer"), null, null);
    }

    [Fact]
    public void TheFirstSendIsTheWholeView() => Assert.Equal(PhoneView.Json(_history, _words, 4), _list.Json(_history, _words, 4));

    [Fact]
    public void AnUnchangedListGoesAsRowNumbersWithoutTheWords()
    {
        _list.Json(_history, _words, 4);

        using var again = JsonDocument.Parse(_list.Json(_history, _words, 4));

        Assert.False(again.RootElement.TryGetProperty("text", out _));
        Assert.Equal(_history.Entries.Select(entry => entry.Seq), again.RootElement.GetProperty("rows").EnumerateArray().Select(row => row.GetInt64()));
    }

    [Fact]
    public void ANewNoticeGoesWholeAndTheRestAsNumbers()
    {
        _list.Json(_history, _words, 4);
        _history.Record(Make.Toast(3, "newest"), null, null);

        using var sent = JsonDocument.Parse(_list.Json(_history, _words, 4));
        var rows = sent.RootElement.GetProperty("rows").EnumerateArray().ToList();

        Assert.Equal(3, rows.Count);
        Assert.Equal("newest", rows[0].GetProperty("title").GetString());
        Assert.All(rows.Skip(1), row => Assert.Equal(JsonValueKind.Number, row.ValueKind));
    }

    [Fact]
    public void RowsThatWereReadGoWholeWithTheirNewState()
    {
        _list.Json(_history, _words, 4);
        _history.MarkSeen(_history.LatestMark);

        using var sent = JsonDocument.Parse(_list.Json(_history, _words, 4));
        var rows = sent.RootElement.GetProperty("rows").EnumerateArray().ToList();

        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => Assert.False(row.GetProperty("unread").GetBoolean()));
    }

    [Fact]
    public void APageThatHasNothingGetsTheWholeViewAgain()
    {
        _list.Json(_history, _words, 4);
        _list.Forget();

        Assert.Equal(PhoneView.Json(_history, _words, 4), _list.Json(_history, _words, 4));
    }

    [Fact]
    public void ALanguageChangeSendsTheRowsAndWordsAgainInTheNewLanguage()
    {
        var chinese = new FakeWords(true);

        _list.Json(_history, _words, 4);

        Assert.Equal(PhoneView.Json(_history, chinese, 4), _list.Json(_history, chinese, 4));
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
    public void ABurstOfNoticesWithinTheUpdateIntervalGoesToThePageOnce()
    {
        var sync = new PhoneSync();

        sync.Next(0, 1);

        var steps = Enumerable.Range(0, 6).Select(i => sync.Next(10 + i * PhoneSync.UpdateSeconds * 0.15, 2 + i)).ToList();

        Assert.Single(steps, step => step == PhoneStep.Update);
    }

    [Fact]
    public void ANewNoticeReachesThePageWithinASecond()
    {
        var sync = new PhoneSync();

        sync.Next(0, 1);

        Assert.Equal(PhoneStep.Update, sync.Next(1, 2));
    }

    [Fact]
    public void APageThatAsksForTheWholeListGetsItWithTheNextUpdate()
    {
        var sync = new PhoneSync();

        sync.Next(0, 1);
        sync.Resend();

        Assert.Equal(PhoneStep.Wait, sync.Next(PhoneSync.UpdateSeconds / 2, 1));
        Assert.Equal(PhoneStep.Update, sync.Next(PhoneSync.UpdateSeconds, 1));
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
    [InlineData("read|7")]
    [InlineData("clear|9")]
    [InlineData("noscript")]
    [InlineData("resync")]
    [InlineData("diag|frame gone")]
    public void ThePageScriptsMessagesAreUnderstood(string message) =>
        Assert.IsNotType<PageMessage.Unknown>(PageMessage.Parse(PageMessage.Prefix + message));

    [Fact]
    public void ARowClickNamesItsRowAndSeeingTheListNamesItsMark()
    {
        Assert.Equal(new PageMessage.Open(12), PageMessage.Parse(PageMessage.Prefix + "open|12"));
        Assert.Equal(new PageMessage.Seen(5), PageMessage.Parse(PageMessage.Prefix + "seen|5"));
        Assert.Equal(new PageMessage.Read(7), PageMessage.Parse(PageMessage.Prefix + "read|7"));
        Assert.Equal(new PageMessage.Clear(9), PageMessage.Parse(PageMessage.Prefix + "clear|9"));
    }

    [Theory]
    [InlineData("open|-1")]
    [InlineData("open|abc")]
    [InlineData("seen|")]
    [InlineData("read|x")]
    [InlineData("clear|-2")]
    [InlineData("dance")]
    public void AMalformedMessageIsUnknown(string message) => Assert.IsType<PageMessage.Unknown>(PageMessage.Parse(PageMessage.Prefix + message));

    [Fact]
    public void SettingTheViewAsksForTheScriptWhenAReloadLostIt()
    {
        Assert.Contains("noscript", PageCalls.Set("{}"));
        Assert.StartsWith("window.__slnotify?window.__slnotify.set({})", PageCalls.Set("{}"));
    }
}
