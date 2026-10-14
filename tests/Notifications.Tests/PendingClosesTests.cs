using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

public class PendingClosesTests
{
    private const int OpenFrame = 100;
    private const double OpenTime = 10;
    private const int Settled = PendingCloses.SettleFrames;

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
    public void APopupIsNotClosedAgainWhileItsCloseSettles()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);
        _closes.Due(OpenFrame + 1, OpenTime, Up(PopupKind.Wish));

        for (var frame = OpenFrame + 2; frame < OpenFrame + 1 + Settled; frame++) Assert.Empty(_closes.Due(frame, OpenTime, Up(PopupKind.Wish)));
    }

    [Fact]
    public void APopupGoneOnceItsCloseSettlesIsDoneWith()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);
        _closes.Due(OpenFrame + 1, OpenTime, Up(PopupKind.Wish));

        Assert.Equal(new CloseStep.Settled(PopupKind.Wish, false), Assert.Single(_closes.Due(OpenFrame + 1 + Settled, OpenTime, Up())));
        Assert.Empty(_closes.Due(OpenFrame + 2 + Settled, OpenTime + PendingCloses.GiveUpSeconds + 1, Up(PopupKind.Wish)));
    }

    [Fact]
    public void APopupStillUpOnceItsCloseSettlesIsClosedAgain()
    {
        _closes.Expect(PopupKind.Wave, OpenFrame, OpenTime);
        _closes.Due(OpenFrame + 1, OpenTime, Up(PopupKind.Wave));

        Assert.Equal(new CloseStep.CloseAgain(PopupKind.Wave, 2), Assert.Single(_closes.Due(OpenFrame + 1 + Settled, OpenTime, Up(PopupKind.Wave))));
    }

    [Fact]
    public void APopupThatWillNotCloseIsLeftVisibleForThePlayer()
    {
        var steps = new List<CloseStep>();

        _closes.Expect(PopupKind.Wave, OpenFrame, OpenTime);

        for (var frame = OpenFrame + 1; frame < OpenFrame + 40; frame++) steps.AddRange(_closes.Due(frame, OpenTime, Up(PopupKind.Wave)));

        Assert.Equal(PendingCloses.MostCloses, steps.Count(step => step is CloseStep.Close or CloseStep.CloseAgain));
        Assert.Equal(new CloseStep.LeftVisible(PopupKind.Wave), steps.Last());
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
    public void APopupWaitingBehindAnotherClosePastTheTimeIsStillClosed()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);
        _closes.Expect(PopupKind.Wave, OpenFrame, OpenTime);
        _closes.Due(OpenFrame + 1, OpenTime, Up(PopupKind.Wish, PopupKind.Wave));

        var late = OpenTime + PendingCloses.GiveUpSeconds + 1;

        Assert.Equal(new CloseStep.Settled(PopupKind.Wish, false), Assert.Single(_closes.Due(OpenFrame + 1 + Settled, late, Up(PopupKind.Wave))));
        Assert.Equal(new CloseStep.Close(PopupKind.Wave, 2 + Settled), Assert.Single(_closes.Due(OpenFrame + 2 + Settled, late, Up(PopupKind.Wave))));
    }

    [Fact]
    public void APopupThatNeverComesUpIsGivenUpOnWhileAnotherCloseSettles()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);
        _closes.Expect(PopupKind.Skill, OpenFrame, OpenTime);
        _closes.Due(OpenFrame + 1, OpenTime + PendingCloses.GiveUpSeconds - 0.1, Up(PopupKind.Wish));

        var step = Assert.Single(_closes.Due(OpenFrame + 2, OpenTime + PendingCloses.GiveUpSeconds, Up(PopupKind.Wish)));

        Assert.Equal(new CloseStep.GiveUp(PopupKind.Skill), step);
    }

    [Fact]
    public void APopupThePlayerOpensAgainIsNotClosed()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);

        Assert.True(_closes.Cancel(PopupKind.Wish));
        Assert.Empty(_closes.Due(OpenFrame + 1, OpenTime, Up(PopupKind.Wish)));
    }

    [Fact]
    public void APopupThePlayerOpensAgainWhileItsCloseSettlesIsNotClosedAgain()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);
        _closes.Due(OpenFrame + 1, OpenTime, Up(PopupKind.Wish));

        Assert.False(_closes.Cancel(PopupKind.Wish));

        for (var frame = OpenFrame + 2; frame < OpenFrame + 40; frame++) Assert.Empty(_closes.Due(frame, OpenTime, Up(PopupKind.Wish)));
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

        Assert.Equal(new CloseStep.Close(PopupKind.Skill, 1), Assert.Single(_closes.Due(OpenFrame + 1, OpenTime, Up(PopupKind.Skill))));
        Assert.Equal(new CloseStep.Settled(PopupKind.Skill, false), Assert.Single(_closes.Due(OpenFrame + 1 + Settled, OpenTime, Up(PopupKind.Wish))));
        Assert.Equal(new CloseStep.Close(PopupKind.Wish, 2 + Settled), Assert.Single(_closes.Due(OpenFrame + 2 + Settled, OpenTime, Up(PopupKind.Wish))));
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
    public void ABurstOfPopupsIsClosedOneAtATimeAndLeavesNothingUp()
    {
        var game = new FakeGame(_closes);

        game.Open(PopupKind.Items, OpenFrame);
        game.Open(PopupKind.Wave, OpenFrame);
        game.Open(PopupKind.Items, OpenFrame);
        game.Open(PopupKind.Skill, OpenFrame);
        game.Open(PopupKind.Wish, OpenFrame);
        game.RunTo(OpenFrame + 60);

        Assert.Empty(game.UpKinds);
        Assert.Equal(new[] { PopupKind.Wish, PopupKind.Items, PopupKind.Wave, PopupKind.Skill }, game.ClosesSent);
        Assert.All(game.CloseFrames.Zip(game.CloseFrames.Skip(1)), pair => Assert.True(pair.Second - pair.First > Settled));
    }

    /// <summary>The burst the log showed: closing the received-items popup made the game open it again with the items it held back.</summary>
    [Fact]
    public void ReceivedItemsTheGameOpensAgainOnCloseAreClosedAfterTheWaveReport()
    {
        var game = new FakeGame(_closes) { ReopensItemsOnClose = 1 };

        game.Open(PopupKind.Items, OpenFrame);
        game.Open(PopupKind.Wave, OpenFrame);
        game.RunTo(OpenFrame + 60);

        Assert.Empty(game.UpKinds);
        Assert.Equal(new[] { PopupKind.Items, PopupKind.Wave, PopupKind.Items }, game.ClosesSent);
        Assert.Contains(new CloseStep.Settled(PopupKind.Items, true), game.Steps);
        Assert.DoesNotContain(game.Steps, step => step is CloseStep.CloseAgain or CloseStep.LeftVisible);
    }

    [Fact]
    public void APopupWhoseCloseIsLostIsClosedAgainAndTheOthersStillClose()
    {
        var game = new FakeGame(_closes) { LosesCloses = 1 };

        game.Open(PopupKind.Wave, OpenFrame);
        game.Open(PopupKind.Items, OpenFrame);
        game.RunTo(OpenFrame + 60);

        Assert.Empty(game.UpKinds);
        Assert.Contains(new CloseStep.CloseAgain(PopupKind.Items, 2), game.Steps);
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
    public void EveryKindIsCheckedAfterAClose() => Assert.Equal(Enum.GetValues<PopupKind>(), PopupKinds.All.AsEnumerable());

    [Fact]
    public void TheLogLinesNameThePopupAndWhatHappened()
    {
        Assert.Equal("wish popup: toast shown, popup closed by the game's close action after 2 frame(s)", new CloseStep.Close(PopupKind.Wish, 2).Line);
        Assert.StartsWith("zombie wave report: still up 3 frame(s) after its close, so the game's close action was sent again", new CloseStep.CloseAgain(PopupKind.Wave, 2).Line);
        Assert.Equal("skill level-up popup: gone 3 frame(s) after its close", new CloseStep.Settled(PopupKind.Skill, false).Line);
        Assert.StartsWith("received-items popup: up again 3 frame(s) after its close because the game opened it again", new CloseStep.Settled(PopupKind.Items, true).Line);
        Assert.Equal("zombie wave report: still up after 2 closes; left visible for the player to close", new CloseStep.LeftVisible(PopupKind.Wave).Line);
        Assert.StartsWith("received-items popup: toast shown, but the popup was not seen open", new CloseStep.GiveUp(PopupKind.Items).Line);
    }

    private static Func<PopupKind, bool> Up(params PopupKind[] upKinds) => kind => upKinds.Contains(kind);

    /// <summary>
    /// The game's side, one frame at a time: a popup it opens is up from the next frame, and a close takes it down a
    /// frame later. It can open the received-items popup again inside that close, as the game does with items it held
    /// back, and it can lose a close.
    /// </summary>
    private sealed class FakeGame
    {
        private readonly PendingCloses _closes;
        private readonly Dictionary<PopupKind, int> _upFrom = new();
        private readonly Dictionary<PopupKind, int> _downFrom = new();
        private int _frame;

        public FakeGame(PendingCloses closes) => _closes = closes;

        public int ReopensItemsOnClose { get; init; }

        public int LosesCloses { get; init; }

        public List<CloseStep> Steps { get; } = new();

        public List<PopupKind> ClosesSent { get; } = new();

        public List<int> CloseFrames { get; } = new();

        public IEnumerable<PopupKind> UpKinds => _upFrom.Keys.Where(IsUp);

        private int ReopensLeft { get; set; }

        private int LostLeft { get; set; }

        public void Open(PopupKind kind, int frame)
        {
            _frame = frame;
            _upFrom[kind] = frame + 1;
            _downFrom.Remove(kind);
            _closes.Expect(kind, frame, frame / 60.0);
        }

        public void RunTo(int lastFrame)
        {
            ReopensLeft = ReopensItemsOnClose;
            LostLeft = LosesCloses;

            for (var frame = _frame + 1; frame <= lastFrame; frame++) Tick(frame);
        }

        private void Tick(int frame)
        {
            _frame = frame;

            foreach (var step in _closes.Due(frame, frame / 60.0, IsUp))
            {
                Steps.Add(step);

                if (step is CloseStep.Close or CloseStep.CloseAgain) Close(step.Kind);
            }
        }

        private void Close(PopupKind kind)
        {
            ClosesSent.Add(kind);
            CloseFrames.Add(_frame);

            if (LostLeft > 0)
            {
                LostLeft--;

                return;
            }

            _downFrom[kind] = _frame + 1;

            if (kind != PopupKind.Items || ReopensLeft == 0) return;

            ReopensLeft--;
            Open(kind, _frame);
        }

        private bool IsUp(PopupKind kind) =>
            _upFrom.TryGetValue(kind, out var upFrom) && _frame >= upFrom && !(_downFrom.TryGetValue(kind, out var downFrom) && _frame >= downFrom);
    }
}
