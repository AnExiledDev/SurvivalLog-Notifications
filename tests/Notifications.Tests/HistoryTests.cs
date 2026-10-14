using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

public class HistoryTests
{
    [Fact]
    public void NewestNoticeIsListedFirst()
    {
        var history = new History();

        history.Record(Make.Toast(1, "first"), null, null);
        history.Record(Make.Toast(2, "second"), null, null);

        Assert.Equal(new[] { "second", "first" }, Make.Titles(history));
    }

    [Fact]
    public void AToastMergedOnScreenCountsIntoItsEntryAndComesBackToTheTop()
    {
        var history = new History();

        history.Record(Make.Toast(1, "Not enough stamina"), null, null);
        history.Record(Make.Toast(2, "other"), null, null);
        history.Record(Make.Toast(1, "Not enough stamina", secondsLater: 3, isMerge: true), new GameStamp(4, 9), null);

        var top = history.Entries[0];

        Assert.Equal(new[] { "Not enough stamina", "other" }, Make.Titles(history));
        Assert.Equal(2, top.Count);
        Assert.Equal(Make.T0.AddSeconds(3), top.LastUtc);
        Assert.Equal(Make.T0, top.FirstUtc);
        Assert.Equal(new GameStamp(4, 9), top.Stamp);
    }

    [Fact]
    public void AMergeMakesASeenEntryUnreadAgain()
    {
        var history = new History();

        history.Record(Make.Toast(1, "stamina"), null, null);
        history.MarkSeen(history.LatestMark);
        history.Record(Make.Toast(1, "stamina", isMerge: true), null, null);

        Assert.Equal(1, history.UnreadCount);
    }

    [Fact]
    public void AMergeForAToastNoLongerListedStartsANewEntry()
    {
        var history = new History();

        history.Record(Make.Toast(9, "gone", isMerge: true), null, null);

        Assert.Equal(1, history.Entries[0].Count);
    }

    [Fact]
    public void AMergeWithNewPopupDataOpensTheNewestPopupAndOneWithoutKeepsTheOld()
    {
        var history = new History();

        history.Record(Make.Toast(1, "wish"), null, Make.Wish(1));
        history.Record(Make.Toast(1, "wish", isMerge: true), null, Make.Wish(2));

        Assert.Equal(Make.Wish(2), history.Entries[0].Reopen);

        history.Record(Make.Toast(1, "wish", isMerge: true), null, null);

        Assert.Equal(Make.Wish(2), history.Entries[0].Reopen);
    }

    [Fact]
    public void TheHistoryKeepsOnlyTheNewest500()
    {
        var history = new History();

        for (var i = 1; i <= History.Cap + 1; i++) history.Record(Make.Toast(i, "n" + i), null, null);

        Assert.Equal(History.Cap, history.Entries.Count);
        Assert.Equal("n" + (History.Cap + 1), history.Entries[0].Content.Title);
        Assert.Equal("n2", history.Entries[^1].Content.Title);
    }

    [Fact]
    public void AGameToastRepeatedWithinTheWindowCountsIntoTheNewestEntry()
    {
        var history = new History();

        history.RecordLocal(Make.Notice("stamina"), Make.T0, null, null);
        history.RecordLocal(Make.Notice("stamina"), Make.T0 + History.LocalMergeWindow, null, null);

        Assert.Single(history.Entries);
        Assert.Equal(2, history.Entries[0].Count);
    }

    [Fact]
    public void AGameToastRepeatedAfterTheWindowOrWithOtherTextIsANewEntry()
    {
        var history = new History();

        history.RecordLocal(Make.Notice("stamina"), Make.T0, null, null);
        history.RecordLocal(Make.Notice("stamina"), Make.T0 + History.LocalMergeWindow + TimeSpan.FromSeconds(1), null, null);
        history.RecordLocal(Make.Notice("hunger"), Make.T0 + History.LocalMergeWindow + TimeSpan.FromSeconds(2), null, null);

        Assert.Equal(new[] { "hunger", "stamina", "stamina" }, Make.Titles(history));
    }

    [Fact]
    public void AGameToastDoesNotCountIntoAToolkitToastsEntry()
    {
        var history = new History();

        history.Record(Make.Toast(1, "stamina"), null, null);
        history.RecordLocal(Make.Notice("stamina"), Make.T0, null, null);

        Assert.Equal(2, history.Entries.Count);
    }

    [Fact]
    public void SeeingTheListClearsUnreadUpToItsMarkAndAMarkFromTheFutureCountsAsTheLatest()
    {
        var history = new History();

        history.Record(Make.Toast(1, "a"), null, null);
        history.Record(Make.Toast(2, "b"), null, null);

        Assert.Equal(2, history.UnreadCount);
        Assert.True(history.MarkSeen(1));
        Assert.Equal(1, history.UnreadCount);
        Assert.True(history.MarkSeen(1000));
        Assert.Equal(0, history.UnreadCount);
        Assert.Equal(history.LatestMark, history.SeenMark);
        Assert.False(history.MarkSeen(1));
    }

    [Fact]
    public void APopupThatCameAfterItsToastIsGivenToTheToastsEntryOnce()
    {
        var history = new History();

        history.Record(Make.Toast(7, "drone"), null, null);

        Assert.False(history.Entries[0].IsClickable);
        Assert.True(history.Attach(7, Make.Items("unload", (1, 2))));
        Assert.True(history.Entries[0].IsClickable);
        Assert.False(history.Attach(7, Make.Items("other", (3, 4))));
        Assert.Equal(Make.Items("unload", (1, 2)), history.Entries[0].Reopen);
        Assert.False(history.Attach(8, Make.Wish()));
        Assert.False(history.Attach(0, Make.Wish()));
    }

    [Fact]
    public void AToastWhoseClickActionTheToolkitForgotIsNoLongerClickable()
    {
        var history = new History();

        history.Record(Make.Toast(3, "mod notice", hasClickAction: true), null, null);

        var seq = history.Entries[0].Seq;

        Assert.True(history.Entries[0].IsClickable);

        history.ForgetToast(seq);

        Assert.False(history.Entries[0].IsClickable);
    }

    [Fact]
    public void AToastWithoutAClickActionOrPopupCannotBeClicked()
    {
        var history = new History();

        history.Record(Make.Toast(3, "plain"), null, null);

        Assert.False(history.Entries[0].IsClickable);
    }

    [Fact]
    public void NoticesFromBeforeTheSaveWasKnownGoOnTopInTheirOrderWithFreshNumbers()
    {
        var saved = new History();
        var early = new History();

        saved.Record(Make.Toast(1, "old"), null, null);
        early.Record(Make.Toast(2, "early 1"), null, null);
        early.Record(Make.Toast(3, "early 2"), null, null);
        saved.Absorb(early);

        Assert.Equal(new[] { "early 2", "early 1", "old" }, Make.Titles(saved));
        Assert.Equal(saved.Entries.Count, saved.Entries.Select(entry => entry.Seq).Distinct().Count());
        Assert.True(saved.Entries[0].Seq > saved.Entries[2].Seq);
    }

    [Fact]
    public void AHistoryReadBackNeverReusesANumberAndCannotHaveSeenMoreThanItHas()
    {
        var entry = new HistoryEntry(40, 41, Make.Notice("x"), 1, Make.T0, Make.T0, null, null);
        var history = new History(new[] { entry }, next: 3, seenMark: 99);

        Assert.Equal(42, history.Next);
        Assert.Equal(41, history.SeenMark);
    }
}
