using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

public class PendingClosesTests
{
    private const int OpenFrame = 100;
    private const double OpenTime = 10;

    private readonly PendingCloses _closes = new();

    [Fact]
    public void APopupIsNeverClosedInTheFrameTheGameOpensIt()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);

        Assert.Empty(_closes.Due(OpenFrame, OpenTime, Up(PopupKind.Wish)));
    }

    [Fact]
    public void APopupThatIsUpOnTheNextFrameIsClosed()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);

        var step = Assert.Single(_closes.Due(OpenFrame + 1, OpenTime, Up(PopupKind.Wish)));

        Assert.Equal(new CloseStep.Close(PopupKind.Wish, 1), step);
    }

    [Fact]
    public void APopupIsClosedOnlyOnce()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);
        _closes.Due(OpenFrame + 1, OpenTime, Up(PopupKind.Wish));

        Assert.Empty(_closes.Due(OpenFrame + 2, OpenTime, Up(PopupKind.Wish)));
    }

    [Fact]
    public void APopupNotUpYetIsWaitedForAndClosedOnceItIs()
    {
        _closes.Expect(PopupKind.Items, OpenFrame, OpenTime);

        Assert.Empty(_closes.Due(OpenFrame + 1, OpenTime + 1, Up()));

        var step = Assert.Single(_closes.Due(OpenFrame + 3, OpenTime + 2, Up(PopupKind.Items)));

        Assert.Equal(new CloseStep.Close(PopupKind.Items, 3), step);
    }

    [Fact]
    public void APopupThatNeverComesUpIsGivenUpOnOnceTheTimeHasPassed()
    {
        _closes.Expect(PopupKind.Wave, OpenFrame, OpenTime);

        Assert.Empty(_closes.Due(OpenFrame + 1, OpenTime + PendingCloses.GiveUpSeconds - 0.1, Up()));
        Assert.Equal(new CloseStep.GiveUp(PopupKind.Wave), Assert.Single(_closes.Due(OpenFrame + 2, OpenTime + PendingCloses.GiveUpSeconds, Up())));
        Assert.Empty(_closes.Due(OpenFrame + 3, OpenTime + PendingCloses.GiveUpSeconds + 1, Up()));
    }

    [Fact]
    public void APopupGivenUpOnIsNotClosedWhenItComesUpLater()
    {
        _closes.Expect(PopupKind.Wave, OpenFrame, OpenTime);
        _closes.Due(OpenFrame + 1, OpenTime + PendingCloses.GiveUpSeconds, Up());

        Assert.Empty(_closes.Due(OpenFrame + 2, OpenTime + PendingCloses.GiveUpSeconds, Up(PopupKind.Wave)));
    }

    [Fact]
    public void APopupStillUpPastTheTimeIsClosedRatherThanGivenUpOn()
    {
        _closes.Expect(PopupKind.Skill, OpenFrame, OpenTime);

        var step = Assert.Single(_closes.Due(OpenFrame + 1, OpenTime + PendingCloses.GiveUpSeconds + 1, Up(PopupKind.Skill)));

        Assert.IsType<CloseStep.Close>(step);
    }

    [Fact]
    public void APopupThePlayerOpensAgainIsNotClosed()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);

        Assert.True(_closes.Cancel(PopupKind.Wish));
        Assert.Empty(_closes.Due(OpenFrame + 1, OpenTime, Up(PopupKind.Wish)));
    }

    [Fact]
    public void CancellingWithNoClosePendingSaysSo() => Assert.False(_closes.Cancel(PopupKind.Items));

    [Fact]
    public void EachKindOfPopupIsClosedOnItsOwn()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);
        _closes.Expect(PopupKind.Items, OpenFrame, OpenTime);
        _closes.Cancel(PopupKind.Wish);

        var step = Assert.Single(_closes.Due(OpenFrame + 1, OpenTime, Up(PopupKind.Wish, PopupKind.Items)));

        Assert.Equal(PopupKind.Items, step.Kind);
    }

    [Fact]
    public void OnlyThePopupThatIsUpIsClosedWhileTheOtherWaits()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);
        _closes.Expect(PopupKind.Skill, OpenFrame, OpenTime);

        Assert.Equal(PopupKind.Skill, Assert.Single(_closes.Due(OpenFrame + 1, OpenTime, Up(PopupKind.Skill))).Kind);
        Assert.Equal(PopupKind.Wish, Assert.Single(_closes.Due(OpenFrame + 2, OpenTime, Up(PopupKind.Wish))).Kind);
    }

    [Fact]
    public void TwoOpensOfOnePopupInARowGiveOneCloseAfterTheLaterOpen()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);
        _closes.Expect(PopupKind.Wish, OpenFrame + 1, OpenTime);

        Assert.Empty(_closes.Due(OpenFrame + 1, OpenTime, Up(PopupKind.Wish)));
        Assert.Single(_closes.Due(OpenFrame + 2, OpenTime, Up(PopupKind.Wish)));
    }

    [Fact]
    public void ALaterOpenGivesThePopupItsFullTimeAgain()
    {
        _closes.Expect(PopupKind.Wave, OpenFrame, OpenTime);
        _closes.Expect(PopupKind.Wave, OpenFrame + 1, OpenTime + 4);

        Assert.Empty(_closes.Due(OpenFrame + 2, OpenTime + PendingCloses.GiveUpSeconds, Up()));
    }

    [Fact]
    public void EveryPopupAToastCanReopenHasItsKind()
    {
        Assert.Equal(PopupKind.Wish, PopupKinds.Of(Make.Wish()));
        Assert.Equal(PopupKind.Items, PopupKinds.Of(Make.Items(null, (1, 1))));
        Assert.Equal(PopupKind.Wave, PopupKinds.Of(new Reopen.Wave(1, 2, 3, 4, null)));
        Assert.Equal(PopupKind.Skill, PopupKinds.Of(new Reopen.Skill(new Dictionary<string, string>())));
    }

    [Fact]
    public void TheLogLineNamesThePopupAndHowLongItWasUp()
    {
        Assert.Equal("wish popup: toast shown, popup closed by the game's close action after 2 frame(s)", new CloseStep.Close(PopupKind.Wish, 2).Line);
        Assert.StartsWith("received-items popup: toast shown, but the popup was not seen open", new CloseStep.GiveUp(PopupKind.Items).Line);
    }

    private static Func<PopupKind, bool> Up(params PopupKind[] upKinds) => kind => upKinds.Contains(kind);
}
