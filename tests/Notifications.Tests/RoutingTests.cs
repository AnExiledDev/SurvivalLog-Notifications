using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

public class HandlingTests
{
    [Theory]
    [InlineData(false, true, GameToastHandling.Leave)]
    [InlineData(false, false, GameToastHandling.Leave)]
    [InlineData(true, true, GameToastHandling.Replace)]
    [InlineData(true, false, GameToastHandling.HistoryOnly)]
    public void APopupBecomesAToastOnlyWhenItsSwitchIsOnAndToastsAreShown(bool isSwitchOn, bool areToastsShown, GameToastHandling expected) =>
        Assert.Equal(expected, Handling.Popup(isSwitchOn, areToastsShown));

    [Theory]
    [InlineData(false, false, true, GameToastHandling.Leave)]
    [InlineData(true, false, true, GameToastHandling.Replace)]
    [InlineData(true, true, true, GameToastHandling.HistoryOnly)]
    [InlineData(true, false, false, GameToastHandling.HistoryOnly)]
    public void AGameToastIsReplacedUnlessTheGamesLookIsKeptOrToastsAreHidden(bool isGroupOn, bool isKeepingGameLook, bool areToastsShown, GameToastHandling expected) =>
        Assert.Equal(expected, Handling.GameToast(isGroupOn, isKeepingGameLook, areToastsShown));
}

public class NoticeRouteTests
{
    private readonly FakeNotices _notices = new();

    [Fact]
    public void ALeftNoticeIsTheGamesAloneAndNotInTheHistory()
    {
        Assert.True(NoticeRoute.IsGameShowing(GameToastHandling.Leave, Make.Notice("a"), null, _notices));
        Assert.Empty(_notices.Raised);
        Assert.Empty(_notices.Kept);
    }

    [Fact]
    public void AReplacedNoticeIsHeldBackFromTheGameOnceItsToastIsUp()
    {
        Assert.False(NoticeRoute.IsGameShowing(GameToastHandling.Replace, Make.Notice("a"), Make.Wish(), _notices));
        Assert.Equal(new[] { "a" }, _notices.Raised);
        Assert.Empty(_notices.Kept);
    }

    [Fact]
    public void AToastThatCouldNotBeRaisedLeavesThePopupToTheGameAndTheHistoryKeepsIt()
    {
        _notices.CanRaise = false;

        Assert.True(NoticeRoute.IsGameShowing(GameToastHandling.Replace, Make.Notice("a"), Make.Wish(), _notices));
        Assert.Equal(new[] { "a" }, _notices.Kept);
    }

    [Fact]
    public void AHistoryOnlyNoticeIsShownByTheGameAndKept()
    {
        Assert.True(NoticeRoute.IsGameShowing(GameToastHandling.HistoryOnly, Make.Notice("a"), null, _notices));
        Assert.Empty(_notices.Raised);
        Assert.Equal(new[] { "a" }, _notices.Kept);
    }

    private sealed class FakeNotices : INoticeOut
    {
        public bool CanRaise { get; set; } = true;

        public List<string> Raised { get; } = new();

        public List<string> Kept { get; } = new();

        public bool Raise(Note note, Reopen? reopen)
        {
            if (CanRaise) Raised.Add(note.Title);

            return CanRaise;
        }

        public void Keep(Note note, Reopen? reopen) => Kept.Add(note.Title);
    }
}

public class RowClickTests
{
    [Fact]
    public void APopupThatCanBeRebuiltWinsOverTheToastsAction()
    {
        var entry = Entry(Make.Wish()) with { ToastId = 4, HasToastAction = true };

        Assert.Equal(new RowClick.Popup(Make.Wish()), RowClick.For(entry));
    }

    [Fact]
    public void WithoutAPopupTheToastsOwnActionRuns() =>
        Assert.Equal(new RowClick.Toast(4), RowClick.For(Entry(null) with { ToastId = 4, HasToastAction = true }));

    [Fact]
    public void ARowWithNeitherDoesNothing()
    {
        Assert.IsType<RowClick.Nothing>(RowClick.For(Entry(null) with { ToastId = 4 }));
        Assert.IsType<RowClick.Nothing>(RowClick.For(null));
    }

    private static HistoryEntry Entry(Reopen? reopen) => new(1, 1, Make.Notice("x"), 1, Make.T0, Make.T0, null, reopen);
}
