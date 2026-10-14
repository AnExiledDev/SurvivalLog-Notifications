using Notifications.Core;
using Xunit;

namespace Notifications.Tests;

public class PendingClosesTests
{
    private const int OpenFrame = 100;
    private const double OpenTime = 10;
    private const double FrameSeconds = 0.02;
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

        var step = Assert.Single(_closes.Due(OpenFrame + 1, At(OpenFrame + 1), Up(PopupKind.Wish)));

        Assert.Equal(new CloseStep.Close(PopupKind.Wish, 1), step);
    }

    [Fact]
    public void APopupIsNotClosedAgainWhileItsCloseSettles()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);
        _closes.Due(OpenFrame + 1, At(OpenFrame + 1), Up(PopupKind.Wish));

        for (var frame = OpenFrame + 2; frame < OpenFrame + 1 + Settled; frame++) Assert.Empty(_closes.Due(frame, At(frame), Up(PopupKind.Wish)));
    }

    [Fact]
    public void APopupGoneOnceItsCloseSettlesIsDoneWith()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);
        _closes.Due(OpenFrame + 1, At(OpenFrame + 1), Up(PopupKind.Wish));

        Assert.Equal(new CloseStep.Settled(PopupKind.Wish, AfterClose.Gone), Assert.Single(_closes.Due(OpenFrame + 1 + Settled, At(OpenFrame + 1 + Settled), Up())));
        Assert.Empty(_closes.Due(OpenFrame + 2 + Settled, At(OpenFrame + 2 + Settled), Up(PopupKind.Wish)));
    }

    [Fact]
    public void TheSettleLastsAtLeastItsTimeAtHighFrameRates()
    {
        const double fastFrame = 1.0 / 240;
        var sentAt = OpenTime + fastFrame;

        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);
        _closes.Due(OpenFrame + 1, sentAt, Up(PopupKind.Wish));

        Assert.Empty(_closes.Due(OpenFrame + 1 + Settled, sentAt + Settled * fastFrame, Up()));

        var step = Assert.Single(_closes.Due(OpenFrame + 20, sentAt + PendingCloses.SettleSeconds + 0.001, Up()));

        Assert.Equal(new CloseStep.Settled(PopupKind.Wish, AfterClose.Gone), step);
    }

    [Fact]
    public void APopupNeverSeenGoneAfterItsCloseIsClosedAgain()
    {
        _closes.Expect(PopupKind.Wave, OpenFrame, OpenTime);
        _closes.Due(OpenFrame + 1, At(OpenFrame + 1), Up(PopupKind.Wave));

        for (var frame = OpenFrame + 2; frame < OpenFrame + 1 + Settled; frame++) _closes.Due(frame, At(frame), Up(PopupKind.Wave));

        var step = Assert.Single(_closes.Due(OpenFrame + 1 + Settled, At(OpenFrame + 1 + Settled), Up(PopupKind.Wave)));

        Assert.Equal(new CloseStep.CloseAgain(PopupKind.Wave, 2), step);
    }

    [Fact]
    public void APopupSeenGoneOnceIsNotClosedAgainWhenItsWindowComesBackWithoutAToast()
    {
        _closes.Expect(PopupKind.Items, OpenFrame, OpenTime);
        _closes.Due(OpenFrame + 1, At(OpenFrame + 1), Up(PopupKind.Items));
        _closes.Due(OpenFrame + 2, At(OpenFrame + 2), Up());

        for (var frame = OpenFrame + 3; frame < OpenFrame + 1 + Settled; frame++) _closes.Due(frame, At(frame), Up(PopupKind.Items));

        var step = Assert.Single(_closes.Due(OpenFrame + 1 + Settled, At(OpenFrame + 1 + Settled), Up(PopupKind.Items)));

        Assert.Equal(new CloseStep.Settled(PopupKind.Items, AfterClose.LeftToTheGame), step);
    }

    [Fact]
    public void APopupThatWillNotCloseIsLeftVisibleForThePlayer()
    {
        var steps = new List<CloseStep>();

        _closes.Expect(PopupKind.Wave, OpenFrame, OpenTime);

        for (var frame = OpenFrame + 1; frame < OpenFrame + 200; frame++) steps.AddRange(_closes.Due(frame, At(frame), Up(PopupKind.Wave)));

        Assert.Equal(PendingCloses.MostCloses, steps.Count(step => step is CloseStep.Close or CloseStep.CloseAgain));
        Assert.Contains(new CloseStep.LeftVisible(PopupKind.Wave), steps);
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
    public void AWindowShowingSomethingTheModLeavesToTheGameIsNotClosed()
    {
        _closes.Expect(PopupKind.Items, OpenFrame, OpenTime);

        Assert.Empty(_closes.Due(OpenFrame + 1, At(OpenFrame + 1), _ => PopupSight.Other));
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
        var late = OpenTime + PendingCloses.GiveUpSeconds + 1;

        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);
        _closes.Expect(PopupKind.Wave, OpenFrame, OpenTime);
        _closes.Due(OpenFrame + 1, OpenTime, Up(PopupKind.Wish, PopupKind.Wave));

        Assert.Equal(new CloseStep.Settled(PopupKind.Wish, AfterClose.Gone), Assert.Single(_closes.Due(OpenFrame + 1 + Settled, late, Up(PopupKind.Wave))));
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
        Assert.Empty(_closes.Due(OpenFrame + 1, At(OpenFrame + 1), Up(PopupKind.Wish)));
    }

    [Fact]
    public void APopupThePlayerOpensAgainWhileItsCloseSettlesIsNotClosedAgain()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);
        _closes.Due(OpenFrame + 1, At(OpenFrame + 1), Up(PopupKind.Wish));

        Assert.False(_closes.Cancel(PopupKind.Wish));

        for (var frame = OpenFrame + 2; frame < OpenFrame + 100; frame++) Assert.Empty(_closes.Due(frame, At(frame), Up(PopupKind.Wish)));
    }

    [Fact]
    public void CancellingWithNoClosePendingSaysSo() => Assert.False(_closes.Cancel(PopupKind.Items));

    [Fact]
    public void EachKindOfPopupIsClosedOnItsOwn()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);
        _closes.Expect(PopupKind.Items, OpenFrame, OpenTime);
        _closes.Cancel(PopupKind.Wish);

        var step = Assert.Single(_closes.Due(OpenFrame + 1, At(OpenFrame + 1), Up(PopupKind.Wish, PopupKind.Items)));

        Assert.Equal(PopupKind.Items, step.Kind);
    }

    [Fact]
    public void OnlyThePopupThatIsUpIsClosedWhileTheOtherWaits()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);
        _closes.Expect(PopupKind.Skill, OpenFrame, OpenTime);

        Assert.Equal(new CloseStep.Close(PopupKind.Skill, 1), Assert.Single(_closes.Due(OpenFrame + 1, At(OpenFrame + 1), Up(PopupKind.Skill))));
        Assert.Equal(new CloseStep.Settled(PopupKind.Skill, AfterClose.Gone), Assert.Single(_closes.Due(OpenFrame + 1 + Settled, At(OpenFrame + 1 + Settled), Up(PopupKind.Wish))));
        Assert.Equal(new CloseStep.Close(PopupKind.Wish, 2 + Settled), Assert.Single(_closes.Due(OpenFrame + 2 + Settled, At(OpenFrame + 2 + Settled), Up(PopupKind.Wish))));
    }

    [Fact]
    public void TwoOpensOfOnePopupInARowGiveOneCloseAfterTheLaterOpen()
    {
        _closes.Expect(PopupKind.Wish, OpenFrame, OpenTime);
        _closes.Expect(PopupKind.Wish, OpenFrame + 1, OpenTime);

        Assert.Empty(_closes.Due(OpenFrame + 1, At(OpenFrame + 1), Up(PopupKind.Wish)));
        Assert.Single(_closes.Due(OpenFrame + 2, At(OpenFrame + 2), Up(PopupKind.Wish)));
    }

    [Fact]
    public void ALaterOpenGivesThePopupItsFullTimeAgain()
    {
        _closes.Expect(PopupKind.Wave, OpenFrame, OpenTime);
        _closes.Expect(PopupKind.Wave, OpenFrame + 1, OpenTime + 4);

        Assert.Empty(_closes.Due(OpenFrame + 2, OpenTime + PendingCloses.GiveUpSeconds, Up()));
    }

    [Fact]
    public void ALastLookComesOnceNothingHasBeenLeftToCloseForAWhile()
    {
        var settledAt = At(OpenFrame + 1 + Settled);

        _closes.Expect(PopupKind.Skill, OpenFrame, OpenTime);
        _closes.Due(OpenFrame + 1, At(OpenFrame + 1), Up(PopupKind.Skill));
        _closes.Due(OpenFrame + 1 + Settled, settledAt, Up());

        Assert.Empty(_closes.Due(OpenFrame + 10, settledAt + PendingCloses.QuietSeconds - 0.1, Up()));
        Assert.Equal(new CloseStep.Quiet(PopupKind.Skill), Assert.Single(_closes.Due(OpenFrame + 11, settledAt + PendingCloses.QuietSeconds + 0.1, Up())));
        Assert.Empty(_closes.Due(OpenFrame + 12, settledAt + 3 * PendingCloses.QuietSeconds, Up()));
    }

    [Fact]
    public void ABurstOfPopupsIsClosedOnePopupAtATimeAndLeavesNothingUp()
    {
        var game = new FakeGame(_closes);

        game.Open(PopupKind.Items);
        game.Open(PopupKind.Wave);
        game.Open(PopupKind.Items);
        game.Open(PopupKind.Skill);
        game.Open(PopupKind.Wish);
        game.RunFrames(100);

        Assert.Empty(game.FullKinds);
        Assert.Equal(new[] { PopupKind.Wish, PopupKind.Items, PopupKind.Wave, PopupKind.Skill }, game.ClosesSent);
        Assert.All(game.CloseFrames.Zip(game.CloseFrames.Skip(1)), pair => Assert.True(pair.Second - pair.First > Settled));
        Assert.DoesNotContain(game.Steps, step => step is CloseStep.CloseAgain or CloseStep.LeftVisible);
        Assert.Equal(new CloseStep.Quiet(PopupKind.Skill), game.Steps.Last());
        Assert.Single(game.Steps.OfType<CloseStep.Quiet>());
    }

    /// <summary>
    /// The burst the log showed. Closing the received-items popup lets the game show the small pickup pop it held back,
    /// at least 100 ms of game time later, with no toast standing in for it; a frame hitch can bring it as soon as the
    /// next frame. It is the game's to show and fade, even when its window cannot be told from the full popup.
    /// </summary>
    [Theory]
    [InlineData(6, PopupSight.Other)]
    [InlineData(2, PopupSight.Other)]
    [InlineData(1, PopupSight.Other)]
    [InlineData(2, PopupSight.Full)]
    public void ThePickupPopTheGameHeldBackIsNeverClosed(int framesAfterTheClose, PopupSight shows)
    {
        var game = new FakeGame(_closes) { HeldBackPop = (framesAfterTheClose, shows) };

        game.Open(PopupKind.Items);
        game.Open(PopupKind.Wave);
        game.RunFrames(100);

        Assert.Equal(new[] { PopupKind.Items, PopupKind.Wave }, game.ClosesSent);
        Assert.Equal(shows, game.SightOf(PopupKind.Items));
        Assert.DoesNotContain(game.Steps, step => step is CloseStep.CloseAgain or CloseStep.LeftVisible);
    }

    [Fact]
    public void ReceivedItemsOpenedAgainWithAToastAfterTheirCloseAreClosedInTheirTurn()
    {
        var game = new FakeGame(_closes);

        game.Open(PopupKind.Items);
        game.Open(PopupKind.Wave);
        game.RunFrames(1);
        game.Open(PopupKind.Items);
        game.RunFrames(100);

        Assert.Empty(game.FullKinds);
        Assert.Equal(new[] { PopupKind.Items, PopupKind.Wave, PopupKind.Items }, game.ClosesSent);
        Assert.Contains(new CloseStep.Settled(PopupKind.Items, AfterClose.OpenedAgainToClose), game.Steps);
        Assert.DoesNotContain(game.Steps, step => step is CloseStep.CloseAgain or CloseStep.LeftVisible);
    }

    [Fact]
    public void APopupWhoseCloseIsLostIsClosedAgainAndTheOthersStillClose()
    {
        var game = new FakeGame(_closes, losesCloses: 1);

        game.Open(PopupKind.Wave);
        game.Open(PopupKind.Items);
        game.RunFrames(100);

        Assert.Empty(game.FullKinds);
        Assert.Contains(new CloseStep.CloseAgain(PopupKind.Items, 2), game.Steps);
        Assert.Equal(new[] { PopupKind.Items, PopupKind.Items, PopupKind.Wave }, game.ClosesSent);
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
        Assert.StartsWith("zombie wave report: never seen gone after its close, so the game's close action was sent again", new CloseStep.CloseAgain(PopupKind.Wave, 2).Line);
        Assert.Equal("skill level-up popup: gone after its close", new CloseStep.Settled(PopupKind.Skill, AfterClose.Gone).Line);
        Assert.StartsWith("received-items popup: the game opened it again with a toast", new CloseStep.Settled(PopupKind.Items, AfterClose.OpenedAgainToClose).Line);
        Assert.EndsWith("its window is up again with something the mod leaves to the game", new CloseStep.Settled(PopupKind.Items, AfterClose.LeftToTheGame).Line);
        Assert.Equal("zombie wave report: still up after 2 closes; left visible for the player to close", new CloseStep.LeftVisible(PopupKind.Wave).Line);
        Assert.StartsWith("received-items popup: toast shown, but the popup was not seen open", new CloseStep.GiveUp(PopupKind.Items).Line);
        Assert.Equal("750 ms after the last close (wish popup), nothing is left to close", new CloseStep.Quiet(PopupKind.Wish).Line);
    }

    private static double At(int frame) => OpenTime + (frame - OpenFrame) * FrameSeconds;

    private static Func<PopupKind, PopupSight> Up(params PopupKind[] upKinds) => kind => upKinds.Contains(kind) ? PopupSight.Full : PopupSight.Down;

    /// <summary>
    /// The game's side, one frame of <see cref="FrameSeconds"/> at a time. A popup it opens is up from the next frame,
    /// and a close takes it down from the next frame. After the received-items close it can show a held-back pickup pop
    /// in that window, with no toast, and it can lose a close.
    /// </summary>
    private sealed class FakeGame
    {
        private readonly PendingCloses _closes;
        private readonly Dictionary<PopupKind, Window> _windows = new();
        private int _frame = OpenFrame;
        private int _closesToLose;
        private Window? _heldBackPop;

        public FakeGame(PendingCloses closes, int losesCloses = 0)
        {
            _closes = closes;
            _closesToLose = losesCloses;
        }

        /// <summary>How many frames after the received-items close the game shows its held-back pickup pop, and how it reads.</summary>
        public (int Frames, PopupSight Shows)? HeldBackPop { get; init; }

        public List<CloseStep> Steps { get; } = new();

        public List<PopupKind> ClosesSent { get; } = new();

        public List<int> CloseFrames { get; } = new();

        public IEnumerable<PopupKind> FullKinds => PopupKinds.All.Where(kind => SightOf(kind) == PopupSight.Full);

        private double Now => At(_frame);

        /// <summary>The game opens a popup a toast stands in for.</summary>
        public void Open(PopupKind kind)
        {
            _windows[kind] = new Window(_frame + 1, null, PopupSight.Full);
            _closes.Expect(kind, _frame, Now);
        }

        public void RunFrames(int count)
        {
            for (var i = 0; i < count; i++) Tick();
        }

        public PopupSight SightOf(PopupKind kind)
        {
            if (kind == PopupKind.Items && _heldBackPop is { } pop && _frame >= pop.UpFrom) return pop.Shows;

            return _windows.TryGetValue(kind, out var window) ? window.SightAt(_frame) : PopupSight.Down;
        }

        private void Tick()
        {
            _frame++;

            foreach (var step in _closes.Due(_frame, Now, SightOf))
            {
                Steps.Add(step);

                if (step is CloseStep.Close or CloseStep.CloseAgain) Close(step.Kind);
            }
        }

        private void Close(PopupKind kind)
        {
            ClosesSent.Add(kind);
            CloseFrames.Add(_frame);

            if (_closesToLose > 0)
            {
                _closesToLose--;

                return;
            }

            _windows[kind] = _windows[kind] with { DownFrom = _frame + 1 };

            if (kind == PopupKind.Items && HeldBackPop is { } pop && _heldBackPop == null) _heldBackPop = new Window(_frame + pop.Frames, null, pop.Shows);
        }

        private sealed record Window(int UpFrom, int? DownFrom, PopupSight Shows)
        {
            public PopupSight SightAt(int frame) => frame < UpFrom || frame >= DownFrom ? PopupSight.Down : Shows;
        }
    }
}
